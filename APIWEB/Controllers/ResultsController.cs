using APIWEB.Data;
using APIWEB.Models;
using APIWEB.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace APIWEB.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly CsvImportService _csvImportService;

        public ResultsController(AppDbContext context, CsvImportService csvImportService)
        {
            _context = context;
            _csvImportService = csvImportService;
        }

        [HttpPost("upload")]
        public async Task<ActionResult<Result>> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _csvImportService.ImportAsync(
                    file,
                    cancellationToken);
                return Ok(result);
            }
            catch (CsvValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Result>>> GetResult(
            [FromQuery] string? fileName,
            [FromQuery] DateTime? startDateFrom,
            [FromQuery] DateTime? startDateTo,
            [FromQuery] double? averageValueFrom,
            [FromQuery] double? averageValueTo,
            [FromQuery] double? averageExecutionTimeFrom,
            [FromQuery] double? averageExecutionTimeTo)
        {
            var query = _context.Results
                 .AsNoTracking()
                 .AsQueryable();
            
            if(!string.IsNullOrWhiteSpace(fileName))
            {
                query = query.Where(x => x.FileName == fileName);
            }

            if(startDateFrom.HasValue)
            {
                query = query.Where(r=> r.StartDate >= startDateFrom.Value);
            }

            if(startDateTo.HasValue)
            {
                query = query.Where(r=>r.StartDate <= startDateTo.Value);
            }

            if(averageValueFrom.HasValue)
            {
                query = query.Where(x=>x.AverageValue >= averageValueFrom.Value);
            }

            if (averageValueTo.HasValue)
            {
                query = query.Where(x=>x.AverageValue <= averageValueTo.Value);
            }

            if(averageExecutionTimeFrom.HasValue)
            {
                query = query.Where(x => x.AverageExecutionTime >= averageExecutionTimeFrom.Value); 
            }

            if(averageExecutionTimeTo.HasValue)
            {
                query = query.Where(x=>x.AverageExecutionTime <= averageExecutionTimeTo.Value);
            }

            var results = await query
                .OrderBy(r=>r.StartDate)
                .ToListAsync();

            return Ok(results);
        }
        [HttpGet("{filename}/values/latest")]
        public async Task<ActionResult<IEnumerable<Value>>> GetLatestValues(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return BadRequest(new { error = "Имя файла не может быть пустым" });
            }

            var resultExists = await _context.Results
                .AsNoTracking()
                .AnyAsync(r => r.FileName == filename);

            if (!resultExists)
            {
                return NotFound(new { error = $"Файл '{filename}' не найден" });
            }

            var values = await _context.Values
                .AsNoTracking()
                .Where(v => v.Result.FileName == filename)
                .OrderByDescending(v => v.Date)
                .Take(10)
                .ToListAsync();
            return Ok(values);
        }
    }
}
