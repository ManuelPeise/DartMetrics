namespace Web.App.Client.Components.Player
{
    /// <summary>
    /// Ein eingegebener Dart. Kodierung wie im Backend (DartRulesBase):
    /// Miss = 0 × 0, Segmente 1–20 × 1–3, 25 × 1 (25) und 25 × 2 (Bull, 50).
    /// </summary>
    public readonly record struct DartThrowInput(int Segment, int Multiplier)
    {
        public int Score => Segment * Multiplier;

        public bool IsMiss => Segment == 0;

        public static DartThrowInput Miss => new(0, 0);

        /// <summary>Kurzform für die Anzeige: T20, D16, S5, 25, Bull, Miss.</summary>
        public string Label => (Segment, Multiplier) switch
        {
            (0, _) => "Miss",
            (25, 2) => "Bull",
            (25, _) => "25",
            (_, 3) => $"T{Segment}",
            (_, 2) => $"D{Segment}",
            _ => $"S{Segment}"
        };
    }
}
