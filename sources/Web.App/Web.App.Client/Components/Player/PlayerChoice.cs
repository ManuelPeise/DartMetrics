namespace Web.App.Client.Components.Player
{
    public sealed record PlayerChoice<TValue>(TValue Value, string Text, string? Hint = null);
}
