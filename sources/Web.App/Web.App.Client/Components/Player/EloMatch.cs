namespace Web.App.Client.Components.Player
{
    /// <summary>Passender Gegner: Elo-Abstand höchstens MatchRange (etwa 36–64 % Siegchance).</summary>
    public static class EloMatch
    {
        public const int MatchRange = 100;

        public static bool IsGoodMatch(int? userRating, int botRating) =>
            userRating is { } rating && Math.Abs(rating - botRating) <= MatchRange;
    }
}
