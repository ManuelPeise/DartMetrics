using Shared.Models.Tournament;

namespace Shared.Interfaces.Tournament
{
    /// <summary>
    /// Ein Turnier-Match spielen. Menschen: der Spielleiter (GameMaster bzw. Besitzer bei privaten Turnieren) gibt jeden Dart ein;
    /// ist danach ein Bot am Wurf, wirft er sofort. Bot gegen Bot: nur der Hintergrunddienst, erst zum Termin der Runde.
    /// Am Matchende: Liga ggf. abschließen, im K.-o. Sieger weiterrücken.
    /// </summary>
    public interface ITournamentMatchService
    {
        /// <summary>Startet ein Match mit mindestens einem Menschen; speichert Profilversion und Zielwahl-Modell der Bots.</summary>
        Task<TournamentMatchState> StartMatchAsync(int matchId);

        Task<TournamentMatchState> AddDartAsync(int matchId, AddTournamentDartModel model);

        /// <summary>Stand für Spielleiter und Zuschauer (offizielle Turniere: alle angemeldeten Benutzer).</summary>
        Task<TournamentMatchState> GetMatchStateAsync(int matchId);

        /// <summary>
        /// Hintergrunddienst (ohne Benutzer): startet fällige Bot-gegen-Bot-Matches und spielt in jedem laufenden eine Aufnahme.
        /// Gibt die Anzahl gespielter Aufnahmen zurück.
        /// </summary>
        Task<int> PlayDueBotMatchesAsync(DateTime nowUtc);
    }
}
