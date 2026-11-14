namespace Web.App.Client.Components.Player
{
    /// <summary>Eine Kennzahl für PlayerStatGrid.</summary>
    public sealed record PlayerStat(string Label, string Value, string? Hint = null, bool Highlight = false);
}
