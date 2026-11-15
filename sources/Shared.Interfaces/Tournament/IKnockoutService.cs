using Shared.Models.Tournament;

namespace Shared.Interfaces.Tournament
{
    /// <summary>
    /// K.-o.-Turnier (WM-Format): anlegen (optional mit Setzliste aus einer abgeschlossenen Liga), besetzen,
    /// starten (Baum mit Freilosen, Satzlänge je Runde), Runden terminieren, Sieger weiterrücken lassen.
    /// Rechte wie bei der Liga: offiziell = GameMaster, privat = Besitzer.
    /// </summary>
    public interface IKnockoutService
    {
        Task<int> CreateKnockoutAsync(CreateKnockoutModel model);

        Task<int> AddParticipantAsync(int tournamentId, AddTournamentParticipantModel model);

        Task RemoveParticipantAsync(int tournamentId, int participantId);

        /// <summary>
        /// Setzt die besten N der Quell-Liga (fehlen sie, werden sie als Teilnehmer ergänzt), lost die übrigen zu,
        /// legt alle Runden an und wertet Freilose sofort.
        /// </summary>
        Task StartKnockoutAsync(int tournamentId, StartKnockoutModel model);

        Task ScheduleRoundAsync(int tournamentId, int roundId, DateTime scheduledAtUtc);

        Task<TournamentOverview> GetKnockoutAsync(int tournamentId);

        /// <summary>
        /// Trägt die Sieger beendeter Matches (und Freilose) in die nächste Runde ein und schließt das Turnier nach dem Finale ab.
        /// Idempotent; wird nach jedem Matchende aufgerufen.
        /// </summary>
        Task AdvanceAsync(int tournamentId);
    }
}
