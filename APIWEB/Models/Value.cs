using System.Text.Json.Serialization;

namespace APIWEB.Models
{
    public class Value
    {
        public long Id { get; set; }
        public long ResultId { get; set; }
        public DateTime Date { get; set; }
        public double ExecutionTime { get; set; }
        public double ValueNumber { get; set; }
        [JsonIgnore]
        public Result Result { get; set; } = null!;
    }
}
