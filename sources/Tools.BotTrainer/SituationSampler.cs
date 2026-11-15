using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Zufällige Spielsituationen über den ganzen Bereich: Rest 2–501 (Straight-Out ab 1), 1–3 Darts in der Aufnahme,
    /// Gegnerstand 2–501, alle Fähigkeiten 0–1, In-/Out-Modus zufällig. Reproduzierbar über den Seed.
    /// </summary>
    public sealed class SituationSampler
    {
        /// <summary>Anteil Situationen im Finish-Bereich (≤ 170): dort wird am meisten entschieden.</summary>
        private const double FinishAreaShare = 0.6;

        private readonly Random _random;

        public SituationSampler(int seed)
        {
            _random = new Random(seed);
        }

        public BotSituation Next()
        {
            var outMode = _random.NextDouble() < 0.8 ? X01OutModeEnum.Double : X01OutModeEnum.Straight;
            var inMode = _random.NextDouble() < 0.8 ? X01InModeEnum.Straight : X01InModeEnum.Double;
            var min = X01Rules.MinRemaining(outMode);

            var remaining = _random.NextDouble() < FinishAreaShare
                ? _random.Next(min, 171)
                : _random.Next(171, 502);

            // Double-In: nicht eröffnet nur, solange noch nichts gezählt hat (Rest = Startwert 301 oder 501).
            var isOpened = true;
            if (inMode == X01InModeEnum.Double && _random.NextDouble() < 0.2)
            {
                remaining = _random.NextDouble() < 0.5 ? 301 : 501;
                isOpened = false;
            }

            var profile = new BotSkillProfile(Skill(), Skill(), Skill(), Skill(), Skill());

            return new BotSituation(
                remaining,
                _random.Next(1, 4),
                _random.Next(2, 502),
                profile,
                inMode,
                outMode,
                isOpened);
        }

        private float Skill() => (float)Math.Round(_random.NextDouble(), 3);
    }
}
