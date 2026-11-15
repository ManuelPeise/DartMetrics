using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Options
{
    /// <summary>Abschnitt "Tournament": Hintergrunddienst für Bot-gegen-Bot-Matches.</summary>
    public class TournamentOptions
    {
        /// <summary>Aus: Bot-gegen-Bot-Matches starten nicht automatisch.</summary>
        public bool BotMatchesEnabled { get; set; } = true;

        /// <summary>Abstand zwischen zwei Bot-Aufnahmen eines Matches (live mitzuverfolgen).</summary>
        [Range(1, 300)]
        public int BotVisitIntervalSeconds { get; set; } = 5;
    }
}
