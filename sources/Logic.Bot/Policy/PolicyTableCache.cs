using System.Collections.Concurrent;
using Shared.Enums.X01;
using Shared.Models.Bot;

namespace Logic.Bot.Policy
{
    /// <summary>
    /// Tabellen je gerundetem Wurfprofil (Streuung, Form, Druck, Abweichung, schlechte Darts), Out-Modus und Gegnerdruck.
    /// Eine Tabelle braucht einige Sekunden; danach ist jede Entscheidung ein Nachschlagen.
    /// </summary>
    public sealed class PolicyTableCache
    {
        private readonly ConcurrentDictionary<Key, Lazy<PolicyTable>> _tables = new();
        private readonly int _samplesPerTarget;

        public PolicyTableCache(int samplesPerTarget = 4000)
        {
            _samplesPerTarget = samplesPerTarget;
        }

        public int Count => _tables.Count;

        public PolicyTable Get(BotSkillProfile profile, X01OutModeEnum outMode, bool opponentPressure)
        {
            var key = Key.Of(profile, outMode, opponentPressure);

            return _tables.GetOrAdd(key, k => new Lazy<PolicyTable>(() =>
                PolicyTable.Solve(ThrowOutcomeModel.Build(k.Profile, _samplesPerTarget, seed: k.Seed), k.OutMode, k.OpponentPressure))).Value;
        }

        private readonly record struct Key(
            int SigmaTenths, int VerticalHundredths, int BiasXTenths, int BiasYTenths, int BadDartPermille,
            int Consistency, int Pressure, X01OutModeEnum OutMode, bool OpponentPressure)
        {
            /// <summary>Nur was die Würfe bestimmt; Taktik und Risiko wirken in der Zielwahl, nicht in der Tabelle.</summary>
            public BotSkillProfile Profile => new(1f, Consistency / 100f, 1f, Pressure / 100f, 0.5f)
            {
                SigmaMm = SigmaTenths / 10f,
                VerticalRatio = VerticalHundredths / 100f,
                BiasXMm = BiasXTenths / 10f,
                BiasYMm = BiasYTenths / 10f,
                BadDartRate = BadDartPermille / 1000f
            };

            /// <summary>Fester Seed je Schlüssel: gleiche Tabelle in jedem Prozess (HashCode.Combine wäre je Prozess anders).</summary>
            public int Seed => unchecked(((((SigmaTenths * 31 + VerticalHundredths) * 31 + BiasXTenths) * 31 + BiasYTenths) * 31 + BadDartPermille) * 31
                                         + Consistency * 101 + Pressure + 1);

            public static Key Of(BotSkillProfile profile, X01OutModeEnum outMode, bool opponentPressure) => new(
                (int)MathF.Round((float)Simulation.ThrowSimulator.HorizontalSigma(profile) * 10),
                (int)MathF.Round(profile.VerticalRatio * 100),
                (int)MathF.Round(profile.BiasXMm * 10),
                (int)MathF.Round(profile.BiasYMm * 10),
                (int)MathF.Round(profile.BadDartRate * 1000),
                (int)MathF.Round(profile.ThrowConsistency * 100),
                (int)MathF.Round(profile.PressureResistance * 100),
                outMode,
                opponentPressure);
        }
    }
}
