namespace Shared.Models.Bot
{
    /// <summary>Ein simulierter Bot-Dart: Ziel und Treffer (Koordinaten in mm, Ursprung Bull-Mitte).</summary>
    public class BotThrowResult
    {
        public int ThrowNumber { get; set; }

        public int TargetSegment { get; set; }

        public int TargetMultiplier { get; set; }

        public float TargetX { get; set; }

        public float TargetY { get; set; }

        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public float HitX { get; set; }

        public float HitY { get; set; }

        public int ScoreBeforeThrow { get; set; }

        public int DartsLeftInVisit { get; set; }
    }
}
