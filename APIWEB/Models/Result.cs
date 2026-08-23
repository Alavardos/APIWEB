namespace APIWEB.Models
{
    public class Result
    {
        public long Id { get; set; }
        public string FileName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public double TimeDelta { get; set; }
        public double AverageExecutionTime { get; set; }
        public double AverageValue { get; set; }
        public double MedianValue { get; set; }
        public double MaxValue { get; set; }
        public double MinValue { get; set; }
        public ICollection<Value> Values { get; set; } = new List<Value>();
        }
    }
