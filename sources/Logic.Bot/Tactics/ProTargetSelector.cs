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
                var routes = CheckoutBook.RoutesFor(context.Remaining, context.DartsAvailable);

                if (routes.Count > 0)
                {
                    var knowledgeGap = (1.0 - Math.Clamp(context.Profile.TacticalIntelligence, 0f, 1f)) * MaxKnowledgeGap;

                    return _random.NextDouble() < knowledgeGap ? Instinct(context.Remaining) : ChooseRoute(routes, context.Profile.RiskTaking, context.Profile.FavoriteDouble)[0];
                }
            }

            return _table.SelectTarget(context);
        }

        /// <summary>Ab diesem Risiko-Wert spielt der Bot aggressiv, bis zu RiskDefensive defensiv (Stil: 0,8 / 0,5 / 0,2).</summary>
        public const float RiskAggressive = 0.7f;
        public const float RiskDefensive = 0.3f;

        /// <summary>
        /// Stil wählt nur zwischen Buch-Varianten:
        /// aggressiv = wenigste Darts, Bull-Wege gern; defensiv = keine Bull-Wege, Doppel mit viel Rest-Spielraum (D16, D20, D8);
        /// normal = Hauptweg.
        /// </summary>
        public static DartTarget[] ChooseRoute(IReadOnlyList<DartTarget[]> routes, float riskTaking, int? favoriteDouble = null)
        {
            if (routes.Count == 1)
            {
                return routes[0];
            }

            // Lieblingsdoppel: unter gleich langen Buch-Varianten die, die darauf endet (nicht bei aggressiv: dort zählt Tempo).
            if (favoriteDouble is { } favorite && riskTaking < RiskAggressive)
            {
                var toFavorite = routes.FirstOrDefault(r => r.Length == routes[0].Length && r[^1].Segment == favorite && r[^1].IsDouble);

                if (toFavorite is not null)
                {
                    return toFavorite;
                }
            }

            if (riskTaking is > RiskDefensive and < RiskAggressive)
            {
                return routes[0];
            }

            return riskTaking >= RiskAggressive
                ? routes.OrderBy(r => r.Length).ThenBy(r => r.Any(IsBull) ? 0 : 1).First()
                : routes.OrderBy(r => r.Any(IsBull) ? 1 : 0).ThenBy(r => DefensiveDoubleRank(r[^1])).First();
        }

        private static bool IsBull(DartTarget target) => target.Segment == 25;

        /// <summary>Doppel, bei denen ein Fehlwurf nach innen wieder ein Doppel lässt (32 → 16 → 8 …), zuerst.</summary>
        private static int DefensiveDoubleRank(DartTarget finish) => finish.Segment switch
        {
            16 => 0,
            20 => 1,
            8 => 2,
            12 => 3,
            18 => 4,
            4 => 5,
            10 => 6,
            _ => 10
        };

        /// <summary>Was ein Anfänger ohne Wegewissen wirft.</summary>
        private static DartTarget Instinct(int remaining) =>
            remaining <= 40 && remaining % 2 == 0 ? DartTarget.Double(remaining / 2)
            : remaining == 50 ? DartTarget.Double(25)
            : remaining > 60 ? DefaultScoringTarget
            : DartTarget.Single(Math.Min(20, remaining - 32 > 0 ? remaining - 32 : remaining / 2));
    }
}
