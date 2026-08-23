namespace APIWEB.Services
{
    public class CsvValidationException : Exception
    {
        public CsvValidationException(string message) : base(message) { }
    }
}
