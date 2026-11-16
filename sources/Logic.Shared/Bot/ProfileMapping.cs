using Data.DartContext.Entities.Bot;
using Shared.Models.Bot;

namespace Logic.Shared.Bot
{
    /// <summary>Gespeicherte Profilversion → Fähigkeiten für Wurf und Zielwahl (inklusive Wurf-Steckbrief V2).</summary>
    public static class ProfileMapping
    {
        public static BotSkillProfile ToSkillProfile(this DartBotProfileVersionEntity profile) => new(
            profile.ThrowPrecision,
            profile.ThrowConsistency,
            profile.TacticalIntelligence,
            profile.PressureResistance,
            profile.RiskTaking)
        {
            SigmaMm = profile.SigmaMm,
            VerticalRatio = profile.VerticalRatio,
            BiasXMm = profile.BiasXMm,
            BiasYMm = profile.BiasYMm,
            BadDartRate = profile.BadDartRate
        };
    }
}
