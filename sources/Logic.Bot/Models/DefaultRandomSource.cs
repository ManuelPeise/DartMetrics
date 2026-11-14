using Shared.Interfaces.Bot;

namespace Logic.Bot.Models
{
    public sealed class DefaultRandomSource : IRandomSource
    {
        private readonly Random _random;

        public DefaultRandomSource() : this(Random.Shared)
        {
        }

        public DefaultRandomSource(Random random)
        {
            _random = random;
        }

        public double NextDouble() => _random.NextDouble();
    }
}
