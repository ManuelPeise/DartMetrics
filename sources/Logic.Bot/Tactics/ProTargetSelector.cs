using Logic.Bot.Models;
using Logic.Bot.Policy;
using Shared.Enums.X01;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Tactics
{
    /// <summary>
    /// Zielwahl „wie ein Profi“:
    /// - Im Checkout-Bereich (Double-Out) gilt konsequent das Wegebuch; nach jedem Dart wird für den neuen Rest neu nachgeschlagen.
    /// - Ohne Weg (Rest über 170, Bogey-Zahlen, Setup mit dem letzten Dart, Straight-Out): exakte Tabelle für das eigene Wurfprofil.
    /// - Checkout-Wissen hängt an TacticalIntelligence: schwache Bots kennen den Weg manchmal nicht und werfen „aus dem Bauch“
    ///   (T20 bzw. das Doppel bei geradem Rest bis 40).
    /// - Double-In vor der Eröffnung: D20.
    /// </summary>
    public sealed class ProTargetSelector : ITargetSelector
    {
        /// <summary>Höchste Wahrscheinlichkeit, den Buch-Weg nicht zu kennen (TacticalIntelligence 0).</summary>
        public const double MaxKnowledgeGap = 0.4;

        private static readonly DartTarget OpeningTarget = DartTarget.Double(20);
        private static readonly DartTarget DefaultScoringTarget = DartTarget.Triple(20);

        private readonly PolicyTableCache _tables;
        private readonly IRandomSource _random;
        private readonly ITargetSelector _table;

        public ProTargetSelector(PolicyTableCache tables, IRandomSource random)
        {
            _tables = tables;
            _random = random;
            _table = new PolicyTableTargetSelector(tables, new RuleBasedTargetSelector(random), random);
        }

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            if (!context.IsOpened && context.InMode == X01InModeEnum.Double)
            {
                return OpeningTarget;
            }

            if (context.OutMode == X01OutModeEnum.Double)
            {
                var book = CheckoutBook.NextTarget(context.Remaining, context.DartsAvailable);

                if (book is not null)
                {
                    var knowledgeGap = (1.0 - Math.Clamp(context.Profile.TacticalIntelligence, 0f, 1f)) * MaxKnowledgeGap;

                    return _random.NextDouble() < knowledgeGap ? Instinct(context.Remaining) : book.Value;
                }
            }

            return _table.SelectTarget(context);
        }

        /// <summary>Was ein Anfänger ohne Wegewissen wirft.</summary>
        private static DartTarget Instinct(int remaining) =>
            remaining <= 40 && remaining % 2 == 0 ? DartTarget.Double(remaining / 2)
            : remaining == 50 ? DartTarget.Double(25)
            : remaining > 60 ? DefaultScoringTarget
            : DartTarget.Single(Math.Min(20, remaining - 32 > 0 ? remaining - 32 : remaining / 2));
    }
}
