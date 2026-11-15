using Shared.Interfaces.Bot;

namespace Logic.Integration.Tests.Infrastructure
{
    /// <summary>Immer 0.25: Box-Muller ergibt 0, der Bot trifft jeden Zielpunkt (deterministische Spiele).</summary>
    public sealed class FixedRandomSource : IRandomSource
    {
        public double NextDouble() => 0.25;
    }
}
