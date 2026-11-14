namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Eine Aufnahme eines Spielers in einem Leg.
    /// DartBotGameId ist denormalisiert, damit Leg und Spieler über zusammengesetzte Fremdschlüssel
    /// garantiert zum selben Spiel gehören.
    /// </summary>
    public class DartBotGameVisitEntity : AEntityBase
    {
        public int DartBotGameId { get; set; }

        public int DartBotGameLegId { get; set; }

        public DartBotGameLegEntity Leg { get; set; } = null!;

        public int DartBotGamePlayerId { get; set; }

        public DartBotGamePlayerEntity Player { get; set; } = null!;

        /// <summary>Chronologische Reihenfolge im Leg.</summary>
        public int TurnNumber { get; set; }

        /// <summary>Laufende Nummer je Spieler im Leg.</summary>
        public int VisitNumber { get; set; }

        public int ScoreBefore { get; set; }

        public int ScoreAfter { get; set; }

        /// <summary>Gegnerstand zu Beginn der Aufnahme (gilt für alle Würfe darin).</summary>
        public int OpponentScoreBefore { get; set; }

        public bool IsBust { get; set; }

        public bool IsCheckout { get; set; }

        /// <summary>Pflicht; sind Würfe gespeichert, muss die Anzahl übereinstimmen (Anwendungsregel).</summary>
        public int DartsUsed { get; set; }

        public ICollection<DartBotGameThrowEntity> Throws { get; set; } = [];
    }
}
