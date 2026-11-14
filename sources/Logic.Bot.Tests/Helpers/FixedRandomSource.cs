using Shared.Interfaces.Bot;

namespace Logic.Bot.Tests.Helpers
{
    /// <summary>
    /// Liefert immer denselben Wert. 0.25 ergibt in Box-Muller cos(π/2) ≈ 0, also Streuung 0:
    /// jeder Dart landet exakt auf dem Zielpunkt.
    /// </summary>
    internal sealed class FixedRandomSource : IRandomSource
    {
        public const double NoScatter = 0.25;

        private readonly double _value;

        public FixedRandomSource(double value = NoScatter)
        {
            _value = value;
        }

        public double NextDouble() => _value;
    }
}
