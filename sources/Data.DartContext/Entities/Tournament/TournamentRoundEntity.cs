namespace Data.DartContext.Entities.Tournament
{
    /// <summary>Liga: ein Spieltag. K.-o.: eine Runde (1. Runde … Finale). Termin legt der GameMaster fest.</summary>
    public class TournamentRoundEntity : AEntityBase
    {
        public int TournamentId { get; set; }

        public TournamentEntity Tournament { get; set; } = null!;

        public int RoundNumber { get; set; }

        /// <summary>Anzeigename, z. B. "Spieltag 3" oder "Halbfinale".</summary>
        public string Name { get; set; } = null!;

        /// <summary>Satzlänge aller Matches dieser Runde (X01SetRules.AllowedBestOfLegs).</summary>
        public int BestOfLegs { get; set; }

        /// <summary>Geplanter Termin; Bot-gegen-Bot-Matches starten nicht vorher.</summary>
        public DateTime? ScheduledAt { get; set; }

        public ICollection<TournamentMatchEntity> Matches { get; set; } = [];
    }
}
