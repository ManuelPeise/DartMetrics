using Logic.Bot.Learning;
using Logic.Shared.Rules;
using Shared.Enums.X01;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;
using Shared.Models.Options;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Zufällige Spielsituationen, reproduzierbar über den Seed (Version 2):
    /// - Rest gewichtet nach Bereichen (dort, wo die Zielwahl entscheidet, mehr Situationen), siehe <see cref="RemainingBands"/>.
    /// - Fähigkeiten wie bei echten Bots: Level 3–20 über die Level-Kurve plus kleine Streuung je Wert.
    /// - Gegnerstand halb unabhängig, halb in der Nähe des eigenen Rests (realistische Duelle und Drucksituationen).
    /// - In-/Out-Modus zufällig (80 % Straight-In, 80 % Double-Out).
    /// </summary>
    public sealed class SituationSampler
    {
        public const int Version = 2;

        /// <summary>Anteil je Rest-Bereich (von, bis, Anteil). Die Summe ist 1.</summary>
        public static readonly (int From, int To, double Share)[] RemainingBands =
        [
            (2, 60, 0.30),
            (61, 110, 0.25),
            (111, 170, 0.20),
            (171, 501, 0.25)
        ];

        /// <summary>Unter Level 3 enden die meisten Rollouts an der Obergrenze und liefern kaum Unterschiede.</summary>
        public const int MinLevel = 3;

        private const float SkillNoise = 0.05f;

        private readonly Random _random;
        private readonly DartBotOptions _options = new();

        public SituationSampler(int seed)
        {
            _random = new Random(seed);
        }

        /// <summary>Für GeneratorParametersJson: womit der Datensatz erzeugt wurde.</summary>
        public static object Describe() => new
        {
            Version,
            RemainingBands = RemainingBands.Select(b => new { b.From, b.To, b.Share }),
            Levels = $"{MinLevel}-20 (DartBotLevelCurve)",
            SkillNoise
        };

        public BotSituation Next()
        {
            var outMode = _random.NextDouble() < 0.8 ? X01OutModeEnum.Double : X01OutModeEnum.Straight;
            var inMode = _random.NextDouble() < 0.8 ? X01InModeEnum.Straight : X01InModeEnum.Double;

            var remaining = Math.Max(X01Rules.MinRemaining(outMode), NextRemaining());

            // Double-In: nicht eröffnet nur, solange noch nichts gezählt hat (Rest = Startwert 301 oder 501).
            var isOpened = true;
            if (inMode == X01InModeEnum.Double && _random.NextDouble() < 0.2)
            {
                remaining = _random.NextDouble() < 0.5 ? 301 : 501;
                isOpened = false;
            }

            var opponent = _random.NextDouble() < 0.5
                ? _random.Next(2, 502)
                : Math.Clamp(remaining + _random.Next(-100, 101), 2, 501);

            return new BotSituation(
                remaining,
                _random.Next(1, 4),
                opponent,
                NextProfile(),
                inMode,
                outMode,
                isOpened);
        }

        private int NextRemaining()
        {
            var roll = _random.NextDouble();

            foreach (var (from, to, share) in RemainingBands)
            {
                if (roll < share)
                {
                    return _random.Next(from, to + 1);
                }

                roll -= share;
            }

            var last = RemainingBands[^1];
            return _random.Next(last.From, last.To + 1);
        }

        private BotSkillProfile NextProfile()
        {
            var level = _random.Next(MinLevel, _options.MaxLevel + 1);
            var start = _options.StartProfile;

            return new BotSkillProfile(
                Skill(start.ThrowPrecision, level),
                Skill(start.ThrowConsistency, level),
                Skill(start.TacticalIntelligence, level),
                Skill(start.PressureResistance, level),
                (float)Math.Round(0.2 + _random.NextDouble() * 0.6, 3));
        }

        private float Skill(float start, int level)
        {
            var value = DartBotLevelCurve.SkillForLevel(_options, start, level) + (float)((_random.NextDouble() * 2 - 1) * SkillNoise);
            return (float)Math.Round(Math.Clamp(value, 0f, 1f), 3);
        }
    }
}
