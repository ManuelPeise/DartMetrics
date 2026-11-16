using Data.DartContext.Entities.Bot;
using Shared.Models.Bot;

namespace Logic.Shared.Bot
{
    /// <summary>Gespeicherte Profilversion → Fähigkeiten für Wurf und Zielwahl (inklusive Wurf-Steckbrief V2).</summary>
    public static class ProfileMapping
    {
        /// <param name="scatterFactor">Tagesform und Nerven: Faktor auf die Streuung (1 = normal).</param>
        public static BotSkillProfile ToSkillProfile(this DartBotProfileVersionEntity profile, float scatterFactor = 1f) => new(
            profile.ThrowPrecision,
            profile.ThrowConsistency,
            profile.TacticalIntelligence,
            profile.PressureResistance,
            profile.RiskTaking)
        {
            // V1-Profile ohne SigmaMm: Faktor wirkt über die Präzision nicht, daher nur bei V2.
            SigmaMm = profile.SigmaMm * scatterFactor,
            VerticalRatio = profile.VerticalRatio,
            BiasXMm = profile.BiasXMm,
            BiasYMm = profile.BiasYMm,
            BadDartRate = profile.BadDartRate
        };
    }
}
