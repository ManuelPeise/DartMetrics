namespace Web.App.Client.Pages.Player.Tournaments
{
    /// <summary>Adressen der Turnierseiten an einer Stelle.</summary>
    public static class TournamentLinks
    {
        public const string List = "/player/tournaments";

        public static string Detail(int tournamentId, bool isKnockout) =>
            $"{List}/{(isKnockout ? "knockout" : "league")}/{tournamentId}";

        public static string Match(int matchId) => $"{List}/match/{matchId}";
    }
}
