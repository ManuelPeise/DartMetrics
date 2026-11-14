namespace Shared.Models.Training
{
    /// <summary>Ergebnis eines Darts im Segment-Training.</summary>
    public class SegmentTrainingThrowResult
    {
        /// <summary>Zielfeld genau getroffen (Segment und Multiplikator)?</summary>
        public bool IsHit { get; set; }

        public int TotalHits { get; set; }

        public int TotalThrows { get; set; }

        /// <summary>Aufnahme mit diesem Dart abgeschlossen (3. Dart)?</summary>
        public bool IsVisitFinished { get; set; }

        /// <summary>Training beendet (letzter Dart der letzten Aufnahme)?</summary>
        public bool IsFinished { get; set; }
    }
}
