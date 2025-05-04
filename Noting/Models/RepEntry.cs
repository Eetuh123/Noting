namespace Noting.Models
{
    public class RepEntry
    {
        public int? Reps { get; set; }

        public bool IsFailure => !Reps.HasValue;

        public RepEntry(int count)
        {
            Reps = count;
        }
        public RepEntry()
        {
            Reps = null;
        }
        public override string ToString() =>
            IsFailure ? "failure" : Reps.Value.ToString();
    }
}
