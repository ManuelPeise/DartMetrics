using Web.Shared.Components.Admin;

namespace Web.App.Components.Layout.Admin
{
    public sealed record AdminNavItem(string Title, string Href, AdminIconName Icon, bool ShowInBottomNav = false, bool MatchExact = false);

    public sealed record AdminNavGroup(string Title, IReadOnlyList<AdminNavItem> Items);

    /// <summary>Einzige Quelle für Sidebar, Bottom Navigation, Seitentitel und Breadcrumb.</summary>
    public static class AdminNavigation
    {
        public const string Root = "/admin";

        public static readonly IReadOnlyList<AdminNavGroup> Groups =
        [
            new("Übersicht",
            [
                new("Dashboard", Root, AdminIconName.Dashboard, ShowInBottomNav: true, MatchExact: true)
            ]),
            new("Verwaltung",
            [
                new("Benutzerverwaltung", Root + "/users", AdminIconName.Users, ShowInBottomNav: true),
                new("DartBot-Verwaltung", Root + "/bots", AdminIconName.Bot, ShowInBottomNav: true),
                new("ML-Modellverwaltung", Root + "/models", AdminIconName.Model)
                // V1: "Statistiken" (/admin/statistics) ist noch ein Platzhalter und deshalb ausgeblendet.
            ]),
            new("System",
            [
                new("System-Logs", Root + "/logs", AdminIconName.Logs, ShowInBottomNav: true),
                // V1: "Systemeinstellungen" (/admin/settings) ist noch ein Platzhalter und deshalb ausgeblendet.
                new("Passwort ändern", Root + "/password", AdminIconName.Lock)
            ])
        ];

        public static IEnumerable<AdminNavItem> AllItems => Groups.SelectMany(g => g.Items);

        /// <summary>Navigationspunkt und Gruppe zur aktuellen Adresse (längster passender Pfad).</summary>
        public static (AdminNavGroup Group, AdminNavItem Item)? Find(string absolutePath)
        {
            var path = "/" + absolutePath.Split('?', '#')[0].Trim('/');

            return Groups
                .SelectMany(g => g.Items.Select(i => (Group: g, Item: i)))
                .Where(x => x.Item.MatchExact
                    ? path.Equals(x.Item.Href, StringComparison.OrdinalIgnoreCase)
                    : path.Equals(x.Item.Href, StringComparison.OrdinalIgnoreCase) ||
                      path.StartsWith(x.Item.Href + "/", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Item.Href.Length)
                .Select(x => ((AdminNavGroup, AdminNavItem)?)x)
                .FirstOrDefault();
        }
    }
}
