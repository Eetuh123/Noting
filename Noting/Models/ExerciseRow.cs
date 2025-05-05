namespace Noting.Models
{
    public class ExerciseRow
    {
        public string NameTag { get; set; } = "";
        public int Weight { get; set; }
        public int Reps { get; set; }

        public int Volume
            => Weight * Reps;
        public double OneRepMaxEstimate
            => Weight * (1 + Reps / 30.0);
    }
}