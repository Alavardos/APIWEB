using System.Globalization;
using APIWEB.Data;
using APIWEB.Models;
using Microsoft.EntityFrameworkCore;

namespace APIWEB.Services;

public class CsvImportService
{
    private readonly AppDbContext _context;

    public CsvImportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result> ImportAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // проверка файла
        if (file == null || file.Length == 0){
            throw new CsvValidationException("Файл пуст");
        }

        if (string.IsNullOrWhiteSpace(file.FileName)){
            throw new CsvValidationException("Отсутствие имени файла");
        }

        var rows = new List<CsvRow>();

        // читаем csv
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);

        // первая строка = заголовок 
        var header = await reader.ReadLineAsync(cancellationToken);

        if (header == null){
            throw new CsvValidationException("CSV-файл пуст");
        }

        var normalizedHeader = header.Trim();

        if (!string.Equals(
                normalizedHeader,
                "Date;ExecutionTime;Value",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new CsvValidationException(
                "Некорректный заголовок CSV. Ожидается: Date;ExecutionTime;Value");
        }

        string? line;
        var lineNumber = 1;

        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineNumber++;

            // пустую строку воспримим как ошибку
            if (string.IsNullOrWhiteSpace(line)){
                throw new CsvValidationException(
                    $"строка {lineNumber} пустая");
            }

            var parts = line.Split(';');

            if (parts.Length != 3)
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: должно быть 3 значения");
            }

            // проверим есть ли везде значения
            if (string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]) ||
                string.IsNullOrWhiteSpace(parts[2]))
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: все значения обязательны");
            }

            // работаем с датой
            if (!DateTimeOffset.TryParse(
                    parts[0].Trim(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var date))
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: некорректная дата");
            }

            var utcDate = date.UtcDateTime;

            // дата не раньше 01.01.2000
            var minDate = new DateTime(
                2000,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

            //проверка чтоб дата была не раньше чем 01.01.2000
            if (utcDate < minDate)
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: дата не может быть раньше 01.01.2000");
            }

            // дата не может быть позже текущего момента
            if (utcDate > DateTime.UtcNow)
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: дата не может быть позже текущего времени");
            }

            // exe time
            if (!double.TryParse(
                    parts[1].Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var executionTime))
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: некорректное время выполнения");
            }

            if (double.IsNaN(executionTime) ||
                double.IsInfinity(executionTime))
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: некорректное время выполнения");
            }

            if (executionTime < 0)
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: время выполнения не может быть меньше 0");
            }

            // value
            if (!double.TryParse(
                    parts[2].Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: некорректное значение показателя");
            }

            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: некорректное значение показателя");
            }

            if (value < 0)
            {
                throw new CsvValidationException(
                    $"строка {lineNumber}: значение показателя не может быть меньше 0");
            }

            rows.Add(new CsvRow
            {
                Date = utcDate,
                ExecutionTime = executionTime,
                Value = value
            });

            // максимум 10 000 строк
            if (rows.Count > 10_000)
            {
                throw new CsvValidationException(
                    "превышено количество строк ( не больше 10 000 )");
            }
        }

        // минимум 1 строка
        if (rows.Count < 1)
        {
            throw new CsvValidationException(
                "CSV-файл должен содержать минимум одну запись");
        }

        // проверили все аспекты -> попробуем парсировать
        await using var transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // ищем предыдущий результат с таким же именем
            var oldResult = await _context.Results
                .Include(r => r.Values)
                .FirstOrDefaultAsync(
                    r => r.FileName == file.FileName,
                    cancellationToken);

            if (oldResult != null)
            {
                _context.Results.Remove(oldResult);

                await _context.SaveChangesAsync(cancellationToken);
            }

            // cчитаем агрегаты
            var minDate = rows.Min(x => x.Date);
            var maxDate = rows.Max(x => x.Date);

            var timeDelta =
                (maxDate - minDate).TotalSeconds;

            var averageExecutionTime =
                rows.Average(x => x.ExecutionTime);

            var averageValue =
                rows.Average(x => x.Value);

            var sortedValues =
                rows.Select(x => x.Value)
                    .OrderBy(x => x)
                    .ToList();

            double medianValue;

            if (sortedValues.Count % 2 == 1)
            {
                medianValue =
                    sortedValues[sortedValues.Count / 2];
            }
            else
            {
                var middle = sortedValues.Count / 2;

                medianValue =
                    (sortedValues[middle - 1] +
                     sortedValues[middle]) / 2.0;
            }

            var maxValue = rows.Max(x => x.Value);
            var minValue = rows.Min(x => x.Value);

            // cоздаём Result
            var result = new Result
            {
                FileName = file.FileName,
                StartDate = minDate,
                TimeDelta = timeDelta,
                AverageExecutionTime = averageExecutionTime,
                AverageValue = averageValue,
                MedianValue = medianValue,
                MaxValue = maxValue,
                MinValue = minValue
            };

            // cоздаём Values
            foreach (var row in rows)
            {
                result.Values.Add(new Value
                {
                    Date = row.Date,
                    ExecutionTime = row.ExecutionTime,
                    ValueNumber = row.Value
                });
            }

            _context.Results.Add(result);

            await _context.SaveChangesAsync(cancellationToken);

            // подтверждаем транзакцию
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private class CsvRow
    {
        public DateTime Date { get; set; }

        public double ExecutionTime { get; set; }

        public double Value { get; set; }
    }
}