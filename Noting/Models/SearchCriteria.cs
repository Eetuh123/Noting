namespace Noting.Models
{
    public class SearchCriteria
    {
        public List<string> Tags { get; set; } = new();
        public string Name { get; set; }
        public List<DateTime> FullDates { get; set; } = new();
        public List<(int Day, int Month)> PartialDates { get; set; } = new();
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public DateTime? Date { get; set; }
    }
}