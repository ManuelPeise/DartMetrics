namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Ein einzelner Dart: anvisiertes Ziel und tatsächlicher Treffer.
    /// Kodierung wie DartThrowConstraints: 0x0 = Miss, 1-20 x 1-3, 25 x 1-2.
    /// Koordinaten in mm, Ursprung Bull-Mitte, x nach rechts, y nach oben.
    /// IsDoubleAttempt / IsCheckoutAttempt werden abgeleitet, nicht gespeichert.
    /// </summary>
    public class DartBotGameThrowEntity : AEntityBase
    {
        public int DartBotGameVisitId { get; set; }

        public DartBotGameVisitEntity Visit { get; set; } = null!;

        public int ThrowNumber { get; set; }

        // Ziel (bei Bots Pflicht, bei Menschen optional)
        public int? TargetSegment { get; set; }

        public int? TargetMultiplier { get; set; }

        public float? TargetX { get; set; }

        public float? TargetY { get; set; }

        // Treffer
        public int Segment { get; set; }

        public int Multiplier { get; set; }

        public int Score { get; set; }

        public float? HitX { get; set; }

        public float? HitY { get; set; }

        // Spielsituation
        public int ScoreBeforeThrow { get; set; }

        public int DartsLeftInVisit { get; set; }
    }
}
