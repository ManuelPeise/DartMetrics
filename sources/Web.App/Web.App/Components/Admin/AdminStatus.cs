namespace Web.App.Components.Admin
{
    /// <summary>Zustände für Badges und Karten. Farben kommen aus den bestehenden Status-Farben.</summary>
    public enum AdminStatus
    {
        Neutral,
        Success,
        Warning,
        Danger,

        /// <summary>Noch keine Datenquelle vorhanden (gestrichelt dargestellt).</summary>
        Placeholder
    }
}
