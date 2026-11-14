using Logic.Bot.Extensions;
using Logic.Bot.Models;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Simulation
{
    /// <summary>
    /// Treffer = Zielpunkt + 2D-Normalverteilung.
    /// Die Streuung sinkt nicht linear mit ThrowPrecision: die letzten Prozentpunkte machen den größten Unterschied.
    /// </summary>
    public sealed class ThrowSimulator : IThrowSimulator
    {
        /// <summary>Streuung (Standardabweichung je Achse, mm) bei Precision 1.0 bzw. 0.0.</summary>
        public const double SigmaAtBest = 8.0;
        public const double SigmaAtWorst = 70.0;

        /// <summary>Krümmung der Kurve Precision -> Streuung.</summary>
        private const double PrecisionExponent = 2.0;

        /// <summary>Formschwankung bei Consistency 0 (Standardabweichung des log-Formfaktors).</summary>
        private const double MaxFormDeviation = 0.35;

        /// <summary>Zusätzliche Streuung unter Druck bei PressureResistance 0.</summary>
        private const double MaxPressurePenalty = 0.4;

        private readonly IRandomSource _random;

        public ThrowSimulator(IRandomSource random)
        {
            _random = random;
        }

        public static double BaseSigma(float throwPrecision)
        {
            var precision = Math.Clamp(throwPrecision, 0f, 1f);

            return SigmaAtBest + (SigmaAtWorst - SigmaAtBest) * Math.Pow(1.0 - precision, PrecisionExponent);
        }

        public double DrawVisitForm(BotSkillProfile profile)
        {
            var consistency = Math.Clamp(profile.ThrowConsistency, 0f, 1f);
            var deviation = (1.0 - consistency) * MaxFormDeviation;

            return Math.Exp(_random.NextGaussian() * deviation);
        }

        public BoardPoint Throw(BoardPoint aimPoint, BotSkillProfile profile, double visitForm, bool isPressureSituation)
        {
            var sigma = BaseSigma(profile.ThrowPrecision) * visitForm;

            if (isPressureSituation)
            {
                var resistance = Math.Clamp(profile.PressureResistance, 0f, 1f);
                sigma *= 1.0 + MaxPressurePenalty * (1.0 - resistance);
            }

            return new BoardPoint(
                aimPoint.X + _random.NextGaussian() * sigma,
                aimPoint.Y + _random.NextGaussian() * sigma);
        }
    }
}
