namespace Noting.Models
{
    public class RepEntry
    {
        public int? Count { get; set; }

        public bool IsFailure => !Count.HasValue;

        public RepEntry(int count)
        {
            Count = count;
        }
        public RepEntry()
        {
            Count = null;
        }
        public override string ToString() =>
            IsFailure ? "failure" : Count.Value.ToString();
    }
}
