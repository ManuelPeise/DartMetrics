using Web.App.Client.Components.Player;

namespace Web.App.Client.Layout.Player
{
    public sealed record PlayerNavItem(string Title, string ShortTitle, string Href, PlayerIconName Icon, bool MatchExact = false);

    /// <summary>Einzige Quelle für Top-Leiste und Bottom Navigation des Spieler-Bereichs.</summary>
    public static class PlayerNavigation
    {
        public const string Root = "/player";

        /// <summary>Rollen mit Zugriff auf den Spieler-Bereich (Admins nutzen /admin).</summary>
        public const string Roles = "Player,GameMaster";

        public static readonly IReadOnlyList<PlayerNavItem> Items =
        [
            new("Start", "Start", Root, PlayerIconName.Home, MatchExact: true),
            new("Training", "Training", Root + "/training", PlayerIconName.Training),
            new("Gegen Bot spielen", "Bot", Root + "/bots", PlayerIconName.Bot),
            new("Turniere", "Turniere", Root + "/tournaments", PlayerIconName.Trophy),
            new("Statistiken", "Statistik", Root + "/statistics", PlayerIconName.Chart),
            new("Profil", "Profil", Root + "/profile", PlayerIconName.User)
        ];
    }
}
