using System.Collections.Concurrent;
using Logic.Bot.Models;
using Logic.Bot.Simulation;
using Logic.Shared.Rules;
using Shared.Interfaces.Bot;
using Shared.Models.Bot;
using Shared.Models.Shared;

namespace Logic.Bot.Tactics
{
    /// <summary>
    /// Regelbasierte Zielwahl (Startlösung, solange kein ML-Modell aktiv ist):
    /// - Checkout möglich: bester Weg aus X01Rules.GetCheckoutSuggestions.
    /// - Weit weg: T20.
    /// - Sonst: Setup, das einen guten Rest hinterlässt und nicht bustet.
    /// TacticalIntelligence: wie oft der beste Weg gewählt wird. RiskTaking: Bereitschaft zu Triples/Bull auf dem Weg.
    /// </summary>
    public sealed class RuleBasedTargetSelector : ITargetSelector
    {
        private const int MaxCheckout = 170;
        private const int TopRoutesKept = 5;
        private const int RiskBuckets = 5;

        /// <summary>Höchstwahrscheinlichkeit, bei TacticalIntelligence 0 einen schlechteren Weg zu wählen.</summary>
        private const double MaxTacticalMistakeChance = 0.3;

        private static readonly DartTarget DefaultScoringTarget = DartTarget.Triple(20);

        /// <summary>Beliebtheit der Finish-Doubles (kleiner = besser). D20, D16, D8 ...</summary>
        private static readonly Dictionary<int, int> DoublePreference = new()
        {
            [20] = 0, [16] = 1, [8] = 2, [18] = 3, [12] = 4, [10] = 5, [4] = 6, [2] = 7,
            [14] = 8, [6] = 9, [25] = 10, [1] = 15
        };

        private const int OtherDoublePreference = 11;

        /// <summary>
        /// Beste Wege je (Rest, verfügbare Darts), sortiert pro Risiko-Stufe.
        /// GetCheckoutSuggestions ist teuer (bis ~240.000 Kombinationen bei drei Darts), daher Cache.
        /// </summary>
        private static readonly ConcurrentDictionary<(int Remaining, int Darts), IReadOnlyList<CheckoutSuggestion>[]> RouteCache = new();

        private readonly X01Rules _rules = new();
        private readonly IRandomSource _random;

        public RuleBasedTargetSelector(IRandomSource random)
        {
            _random = random;
        }

        public DartTarget SelectTarget(BotThrowDecisionContext context)
        {
            var routes = GetBestRoutes(context.Remaining, context.DartsAvailable, context.Profile.RiskTaking);

            if (routes.Count > 0)
            {
                return ToTarget(PickRoute(routes, context.Profile).Darts[0]);
            }

            if (context.Remaining - DefaultScoringTarget.Score > MaxCheckout)
            {
                return DefaultScoringTarget;
            }

            return SelectSetupTarget(context);
        }

        private CheckoutSuggestion PickRoute(IReadOnlyList<CheckoutSuggestion> routes, BotSkillProfile profile)
        {
            var mistakeChance = (1.0 - Math.Clamp(profile.TacticalIntelligence, 0f, 1f)) * MaxTacticalMistakeChance;

            if (routes.Count > 1 && _random.NextDouble() < mistakeChance)
            {
                var index = 1 + (int)(_random.NextDouble() * (routes.Count - 1));
                return routes[Math.Min(index, routes.Count - 1)];
            }

            return routes[0];
        }

        /// <summary>
        /// Kein Checkout in dieser Aufnahme möglich: Ziel so wählen, dass der Rest (bei Treffer) gut zu checken ist.
        /// Ziele, die bei Treffer busten würden (Rest kleiner 2), sind ausgeschlossen.
        /// </summary>
        private DartTarget SelectSetupTarget(BotThrowDecisionContext context)
        {
            DartTarget? best = null;
            var bestCost = double.MaxValue;

            foreach (var candidate in SetupCandidates())
            {
                var leftover = context.Remaining - candidate.Score;

                if (leftover < 2)
                {
                    continue;
                }

                var cost = LeftoverCost(leftover, context.Profile.RiskTaking) + TargetDifficulty(candidate, context.Profile.RiskTaking);

                if (cost < bestCost)
                {
                    bestCost = cost;
                    best = candidate;
                }
            }

            // Rest 2 oder 3 ohne Checkout-Darts gibt es nicht; Fallback nur zur Sicherheit.
            return best ?? DartTarget.Single(1);
        }

        private static IEnumerable<DartTarget> SetupCandidates()
        {
            for (var segment = 20; segment >= 1; segment--)
            {
                yield return DartTarget.Triple(segment);
                yield return DartTarget.Single(segment);
            }

            yield return DartTarget.Single(25);
        }

        /// <summary>Wie gut ist der Rest für die nächste Aufnahme? Weniger Darts und beliebte Doubles sind besser.</summary>
        private double LeftoverCost(int leftover, float riskTaking)
        {
            var routes = GetBestRoutes(leftover, 3, riskTaking);

            if (routes.Count > 0)
            {
                return routes[0].Darts.Count * 10 + RouteCost(routes[0], RiskBucket(riskTaking));
            }

            // Nicht in einer Aufnahme checkbar: je näher an 170, desto besser.
            return 40 + Math.Max(0, leftover - MaxCheckout) / 10.0;
        }

        /// <summary>
        /// Wege mit der kleinstmöglichen Dartanzahl (höchstens <paramref name="darts"/>), beste zuerst.
        /// Es wird mit 1 Dart begonnen und nur bei Bedarf tiefer gesucht.
        /// </summary>
        private IReadOnlyList<CheckoutSuggestion> GetBestRoutes(int remaining, int darts, float riskTaking)
        {
            if (remaining is < 2 or > MaxCheckout || darts < 1)
            {
                return [];
            }

            var perBucket = RouteCache.GetOrAdd((remaining, Math.Min(darts, 3)), key =>
            {
                List<CheckoutSuggestion> found = [];

                for (var d = 1; d <= key.Darts && found.Count == 0; d++)
                {
                    found = _rules.GetCheckoutSuggestions(key.Remaining, d);
                }

                var ordered = new IReadOnlyList<CheckoutSuggestion>[RiskBuckets];

                for (var bucket = 0; bucket < RiskBuckets; bucket++)
                {
                    var b = bucket;
                    ordered[bucket] = found
                        .OrderBy(route => RouteCost(route, b))
                        .Take(TopRoutesKept)
                        .ToList();
                }

                return ordered;
            });

            return perBucket[RiskBucket(riskTaking)];
        }

        private static int RiskBucket(float riskTaking) =>
            Math.Clamp((int)(Math.Clamp(riskTaking, 0f, 1f) * RiskBuckets), 0, RiskBuckets - 1);

        /// <summary>Kosten eines Weges: Schwierigkeit der Darts davor + Beliebtheit des Finish-Doubles.</summary>
        private static double RouteCost(CheckoutSuggestion route, int riskBucket)
        {
            var risk = riskBucket / (double)(RiskBuckets - 1);
            double cost = 0;

            for (var i = 0; i < route.Darts.Count - 1; i++)
            {
                cost += TargetDifficulty(ToTarget(route.Darts[i]), (float)risk);
            }

            var finish = route.Darts[^1];
            cost += DoublePreference.GetValueOrDefault(finish.Segment, OtherDoublePreference);

            return cost;
        }

        /// <summary>Wie schwer bzw. riskant ist ein Ziel? Risikofreude senkt die Kosten von Triples und Bull.</summary>
        private static double TargetDifficulty(DartTarget target, float riskTaking)
        {
            var riskDiscount = 1.0 - 0.5 * Math.Clamp(riskTaking, 0f, 1f);

            return (target.Segment, target.Multiplier) switch
            {
                (25, 2) => 5 * riskDiscount,
                (25, 1) => 4 * riskDiscount,
                (_, 3) => 3 * riskDiscount,
                (_, 2) => 2,
                _ => 0
            };
        }

        private static DartTarget ToTarget(CheckoutDart dart) => new(dart.Segment, dart.Multiplier);
    }
}
