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

        /// <summary>
        /// Wie <see cref="Throw(BoardPoint, BotSkillProfile, double, bool)"/>, aber mit den Darts, die in dieser Aufnahme
        /// schon im Board stecken (können den Wurf ablenken, z. B. bei verdeckter T20).
        /// </summary>
        BoardPoint Throw(BoardPoint aimPoint, BotSkillProfile profile, double visitForm, bool isPressureSituation, IReadOnlyList<BoardPoint> dartsInBoard) =>
            Throw(aimPoint, profile, visitForm, isPressureSituation);
    }
}
