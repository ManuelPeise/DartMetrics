using Shared.Interfaces.Bot;

namespace Logic.Bot.Extensions
{
    internal static class RandomSourceExtensions
    {
        /// <summary>Standardnormalverteilt (Box-Muller).</summary>
        public static double NextGaussian(this IRandomSource random)
        {
            var u1 = 1.0 - random.NextDouble(); // (0, 1], vermeidet log(0)
            var u2 = random.NextDouble();

            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
