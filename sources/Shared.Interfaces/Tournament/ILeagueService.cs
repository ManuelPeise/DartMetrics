using Shared.Models.Tournament;

namespace Shared.Interfaces.Tournament
{
    /// <summary>
    /// Liga anlegen, besetzen, starten (Spielplan), Spieltage terminieren, Tabelle.
    /// Offizielle Ligen verwaltet die Rolle GameMaster, private nur ihr Besitzer (Rolle Player).
    /// </summary>
    public interface ILeagueService
    {
        Task<int> CreateLeagueAsync(CreateLeagueModel model);

        /// <summary>Nur vor dem Start; höchstens 16 Teilnehmer.</summary>
        Task<int> AddParticipantAsync(int tournamentId, AddTournamentParticipantModel model);

        /// <summary>Nur vor dem Start.</summary>
        Task RemoveParticipantAsync(int tournamentId, int participantId);

        /// <summary>Erzeugt den Spielplan (Hin- und Rückrunde); braucht 8–16 bestätigte Teilnehmer.</summary>
        Task StartLeagueAsync(int tournamentId, StartLeagueModel model);

        /// <summary>Termin eines Spieltags setzen oder ändern, solange keines seiner Matches begonnen hat.</summary>
        Task ScheduleRoundAsync(int tournamentId, int roundId, DateTime scheduledAtUtc);

        Task<LeagueOverview> GetLeagueAsync(int tournamentId);

        Task<List<LeagueTableRow>> GetTableAsync(int tournamentId);

        /// <summary>Schließt die Liga ab, sobald alle Matches beendet sind (Sieger = Platz 1). Gibt zurück, ob abgeschlossen.</summary>
        Task<bool> FinishLeagueIfCompleteAsync(int tournamentId);
    }
}
