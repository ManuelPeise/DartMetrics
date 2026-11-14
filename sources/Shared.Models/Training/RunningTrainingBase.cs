namespace Shared.Models.Training
{
    /// <summary>
    /// Stand eines offenen Trainings zum Fortsetzen nach einem Reload.
    /// Next… ist die Position des nächsten Darts (Aufnahme bzw. Versuch, Dart 1–3).
    /// </summary>
    public abstract class RunningTrainingBase
    {
        public int TrainingId { get; set; }

        public DateTime StartedAt { get; set; }

        public int NextVisitNumber { get; set; } = 1;

        public int NextThrowNumber { get; set; } = 1;

        public int DartsThrown { get; set; }

        /// <summary>Bereits geworfene Darts der angefangenen Aufnahme (leer, wenn die nächste Aufnahme neu beginnt).</summary>
        public List<TrainingDart> CurrentVisitDarts { get; set; } = [];
    }

    public class TrainingDart
    {
        public int Segment { get; set; }

        public int Multiplier { get; set; }
    }
}
