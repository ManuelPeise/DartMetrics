namespace Shared.Interfaces.Tournament
{
    /// <summary>
    /// Turnierseite der Kontolöschung (ohne Rechteprüfung, nur für den Lösch-Ablauf):
    /// nicht gestartete Turniere → abgemeldet; laufende/beendete → Name „Gelöschter Spieler“,
    /// offene Matches gewinnt der Gegner kampflos, danach Liga abschließen bzw. K.o. weiterrücken.
    /// </summary>
    public interface ITournamentWithdrawal
    {
        Task WithdrawDeletedUserAsync(int userId, string anonymizedName);
    }
}
