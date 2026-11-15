namespace Shared.Interfaces.Tournament
{
    /// <summary>
    /// Anmeldung zu offiziellen Turnieren (Liga und K.o.): der GameMaster öffnet die Anmeldung, Spieler melden sich an
    /// oder ab, der GameMaster bestätigt (oder entfernt). Gestartet wird nur mit bestätigten Teilnehmern.
    /// </summary>
    public interface ITournamentRegistrationService
    {
        /// <summary>GameMaster: Anmeldung öffnen, optional mit Frist (UTC).</summary>
        Task OpenRegistrationAsync(int tournamentId, DateTime? deadlineUtc);

        /// <summary>Spieler: sich selbst anmelden (Status „angemeldet“, wartet auf Bestätigung).</summary>
        Task<int> RegisterAsync(int tournamentId);

        /// <summary>Spieler: eigene Anmeldung zurückziehen (vor dem Start).</summary>
        Task WithdrawAsync(int tournamentId);

        /// <summary>GameMaster: angemeldeten Teilnehmer bestätigen.</summary>
        Task ConfirmParticipantAsync(int tournamentId, int participantId);
    }
}
