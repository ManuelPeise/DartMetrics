using Logic.Bot.Extensions;
using Logic.Bot.Models;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Simulation
{
    /// <summary>
    /// Treffer = Zielpunkt + systematische Abweichung + 2D-Normalverteilung (senkrecht ggf. stärker als waagerecht),
    /// gelegentlich ein „schlechter Dart“ mit dreifacher Streuung; steckende Darts nahe dem Zielpunkt können ablenken.
    /// V1-Profile (ohne SigmaMm): runde Streuung aus ThrowPrecision, keine schlechten Darts, keine Abweichung.
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

        /// <summary>Steckt ein Dart näher als das am Zielpunkt, kann er den nächsten ablenken (verdeckte T20).</summary>
        public const double BlockRadius = 7.0;

        /// <summary>Wahrscheinlichkeit je steckendem Dart im Block-Radius, dass er ablenkt.</summary>
        public const double BlockChance = 0.35;

        /// <summary>Ablenkung (Standardabweichung je Achse, mm).</summary>
        public const double BlockDeflection = 7.0;

        /// <summary>Streuung eines schlechten Darts relativ zur normalen.</summary>
        public const double BadDartFactor = 3.0;

        private static readonly IReadOnlyList<BoardPoint> NoDarts = [];

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

        /// <summary>Waagerechte Grundstreuung (mm): V2 direkt, V1 aus ThrowPrecision.</summary>
        public static double HorizontalSigma(BotSkillProfile profile) => profile.SigmaMm ?? BaseSigma(profile.ThrowPrecision);

        public double DrawVisitForm(BotSkillProfile profile)
        {
            var consistency = Math.Clamp(profile.ThrowConsistency, 0f, 1f);
            var deviation = (1.0 - consistency) * MaxFormDeviation;

            return Math.Exp(_random.NextGaussian() * deviation);
        }

        public BoardPoint Throw(BoardPoint aimPoint, BotSkillProfile profile, double visitForm, bool isPressureSituation) =>
            Throw(aimPoint, profile, visitForm, isPressureSituation, NoDarts);

        public BoardPoint Throw(BoardPoint aimPoint, BotSkillProfile profile, double visitForm, bool isPressureSituation, IReadOnlyList<BoardPoint> dartsInBoard)
        {
            var sigma = HorizontalSigma(profile) * visitForm;

            if (isPressureSituation)
            {
                var resistance = Math.Clamp(profile.PressureResistance, 0f, 1f);
                sigma *= 1.0 + MaxPressurePenalty * (1.0 - resistance);
            }

            // Nur bei V2-Profilen wird gezogen: V1-Zufallsfolgen bleiben unverändert.
            if (profile.BadDartRate > 0 && _random.NextDouble() < profile.BadDartRate)
            {
                sigma *= BadDartFactor;
            }

            var x = aimPoint.X + profile.BiasXMm + _random.NextGaussian() * sigma;
            var y = aimPoint.Y + profile.BiasYMm + _random.NextGaussian() * sigma * Math.Max(0.1, profile.VerticalRatio);

            foreach (var dart in dartsInBoard)
            {
                var dx = dart.X - aimPoint.X;
                var dy = dart.Y - aimPoint.Y;

                if (dx * dx + dy * dy <= BlockRadius * BlockRadius && _random.NextDouble() < BlockChance)
                {
                    x += _random.NextGaussian() * BlockDeflection;
                    y += _random.NextGaussian() * BlockDeflection;
                }
            }

            return new BoardPoint(x, y);
        }
    }
}
