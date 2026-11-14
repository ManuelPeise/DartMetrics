using Shared.Models.Bot;

namespace Shared.Interfaces.Bot
{
    public interface IThrowSimulator
    {
        /// <summary>
        /// Formfaktor für eine Aufnahme (1.0 = normale Form, größer = schlechter).
        /// Wird einmal pro Aufnahme gezogen und an jeden Wurf übergeben.
        /// </summary>
        double DrawVisitForm(BotSkillProfile profile);

        /// <summary>Simuliert, wo der Dart landet.</summary>
        BoardPoint Throw(BoardPoint aimPoint, BotSkillProfile profile, double visitForm, bool isPressureSituation);
    }
}
