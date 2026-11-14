namespace Shared.Models.Bot
{
    public readonly record struct DartTarget(int Segment, int Multiplier)
    {
        public int Score => Segment * Multiplier;

        public bool IsDouble => Multiplier == 2;

        public bool IsMiss => Segment == 0;

        public static DartTarget Miss => new(0, 0);

        public static DartTarget Triple(int segment) => new(segment, 3);

        public static DartTarget Double(int segment) => new(segment, 2);

        public static DartTarget Single(int segment) => new(segment, 1);

        public override string ToString() => Segment switch
        {
            0 => "Miss",
            25 => Multiplier == 2 ? "Bull" : "25",
            _ => $"{(Multiplier switch { 3 => "T", 2 => "D", _ => "S" })}{Segment}"
        };
    }
}
