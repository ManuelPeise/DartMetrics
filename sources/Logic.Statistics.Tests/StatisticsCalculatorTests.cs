using Logic.Statistics.Performance;
using Shared.Enums.Statistics;
using Shared.Enums.X01;
using static Logic.Statistics.Tests.LegBuilder;

namespace Logic.Statistics.Tests
{
    /// <summary>Rechenkern der Statistik mit Beispielen, deren Ergebnis von Hand bekannt ist.</summary>
    public class StatisticsCalculatorTests
    {
        private static readonly StatLeg NineDarter =
            Leg(501, V("T20", "T20", "T20"), V("T20", "T20", "T20"), V("T20", "T19", "D12"));

        [Fact]
        public void NineDarter_HasAverage167_AndPerfectCheckout()
        {
            var legs = new[] { NineDarter };

            Assert.Equal(167m, StatisticsCalculator.ThreeDartAverage(legs));
            Assert.Equal(167m, StatisticsCalculator.FirstNineAverage(legs));

            var scoring = StatisticsCalculator.Scoring(legs);
            Assert.Equal(180, scoring.HighestScore);
            Assert.Equal(2, scoring.Count180);
            Assert.Equal(3, scoring.Count140Plus); // 180, 180, 141 – kumuliert
            Assert.Equal(200m, scoring.Per100Legs180);

            var checkout = StatisticsCalculator.Checkout(legs);
            Assert.Equal(1, checkout.Attempts); // nur der Dart auf Rest 24; Rest 81 und 141 sind kein Ein-Dart-Finish
            Assert.Equal(1, checkout.Successful);
            Assert.Equal(100m, checkout.Rate);
            Assert.Equal(141, checkout.HighestCheckout);

            var legStats = StatisticsCalculator.LegsAndMatches(legs, []);
            Assert.Equal(9, legStats.BestLegDarts);
            Assert.Equal(9m, legStats.AverageDartsPerLeg);
        }

        [Fact]
        public void MissedDoubles_BeforeTheCheckout_AreCountedAsAttempts()
        {
            // Rest 40: S20 (statt D20) → 20, S10 (statt D10) → 10, Miss → 10; nächste Aufnahme D5.
            var leg = Leg(40, V("S20", "S10", "0"), V("D5"));

            var checkout = StatisticsCalculator.Checkout([leg]);

            Assert.Equal(4, checkout.Attempts);
            Assert.Equal(1, checkout.Successful);
            Assert.Equal(3, checkout.MissedDoubles);
            Assert.Equal(25m, checkout.Rate);
        }

        [Fact]
        public void Bust_ScoresZero_ButItsDartsCount()
        {
            // Rest 32, T20 → Bust. Danach D16.
            var leg = Leg(32, V("T20"), V("D16"));

            Assert.Equal(1, StatisticsCalculator.Checkout([leg]).Busts);
            // 32 Punkte mit 2 Darts → 48
            Assert.Equal(48m, StatisticsCalculator.ThreeDartAverage([leg]));
        }

        [Fact]
        public void StraightOut_HasNoDoubleAttempts()
        {
            var leg = Leg(40, X01OutModeEnum.Straight, true, V("S20", "S20"));

            var checkout = StatisticsCalculator.Checkout([leg]);

            Assert.Equal(0, checkout.Attempts);
            Assert.Null(checkout.Rate);
            Assert.Equal(40, checkout.HighestCheckout);
        }

        [Fact]
        public void FirstNine_UsesOnlyTheFirstThreeVisits()
        {
            // 3 × 60, dann 3 × 26 Punkte (S20 S5 S1) → First 9 = 60, gesamt = (180 + 78) / 18 × 3 = 43
            var leg = Leg(501, X01OutModeEnum.Double, false,
                V("S20", "S20", "S20"), V("S20", "S20", "S20"), V("S20", "S20", "S20"),
                V("S20", "S5", "S1"), V("S20", "S5", "S1"), V("S20", "S5", "S1"));

            Assert.Equal(60m, StatisticsCalculator.FirstNineAverage([leg]));
            Assert.Equal(43m, StatisticsCalculator.ThreeDartAverage([leg]));
        }

        [Fact]
        public void UnfinishedLeg_CountsForAverage_NotAsLeg()
        {
            var unfinished = Leg(501, X01OutModeEnum.Double, false, V("T20", "T20", "T20"));

            var legs = StatisticsCalculator.LegsAndMatches([NineDarter, unfinished], []);

            Assert.Equal(1, legs.LegsPlayed);
            Assert.Equal(1, legs.LegsWon);
            Assert.Equal(100m, legs.LegsWonRate);
            Assert.Equal(StatisticsCalculator.ThreeDartAverage([NineDarter, unfinished]), (501m + 180m) / 12 * 3);
        }

        [Fact]
        public void WinRate_AndLongestStreak_FollowTheMatchOrder()
        {
            var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var results = new[] { true, true, false, true, true, true };
            var matches = results.Select((won, i) => new StatMatch($"m{i}", start.AddDays(i), StatisticsCategoryEnum.Training, won)).ToList();

            var stats = StatisticsCalculator.LegsAndMatches([], matches);

            Assert.Equal(6, stats.MatchesPlayed);
            Assert.Equal(5, stats.MatchesWon);
            Assert.Equal(83.33m, stats.WinRate);
            Assert.Equal(3, stats.LongestWinningStreak);
        }

        [Fact]
        public void NoData_GivesNullInsteadOfZero()
        {
            Assert.Null(StatisticsCalculator.ThreeDartAverage([]));
            Assert.Null(StatisticsCalculator.CheckoutRate([]));
            Assert.Null(StatisticsCalculator.WinRate([]));
            Assert.Null(StatisticsCalculator.Scoring([]).HighestScore);
            Assert.Null(StatisticsCalculator.LegsAndMatches([], []).AverageDartsPerLeg);
        }

        [Fact]
        public void Kpis_CompareWithThePreviousPeriod()
        {
            var current = new[] { NineDarter };
            var previous = new[] { Leg(501, X01OutModeEnum.Double, false, V("S20", "S20", "S20")) };

            var kpis = StatisticsCalculator.Kpis(current, [], previous, []);

            Assert.Equal(167m, kpis.ThreeDartAverage.Current);
            Assert.Equal(60m, kpis.ThreeDartAverage.Previous);
            Assert.Equal(107m, kpis.ThreeDartAverage.Change);

            var allTime = StatisticsCalculator.Kpis(current, [], null, null);
            Assert.Null(allTime.ThreeDartAverage.Change);
        }

        [Fact]
        public void LastTenMatches_NeedTwentyMatches()
        {
            var start = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            var legs = new List<StatLeg>();
            var matches = new List<StatMatch>();

            for (var i = 0; i < 20; i++)
            {
                var key = $"match{i}";
                // Ältere 10 Matches: 60er-Aufnahmen; neuere 10: 180er.
                var visit = i < 10 ? V("S20", "S20", "S20") : V("T20", "T20", "T20");
                legs.Add(Leg(key, start.AddDays(i), StatisticsCategoryEnum.Training, 501, X01OutModeEnum.Double, false, visit));
                matches.Add(new StatMatch(key, start.AddDays(i), StatisticsCategoryEnum.Training, i >= 10));
            }

            Assert.Null(StatisticsCalculator.LastMatches(legs, matches.Take(19).ToList()).ThreeDartAverage.Current);

            var comparison = StatisticsCalculator.LastMatches(legs, matches);
            Assert.Equal(180m, comparison.ThreeDartAverage.Current);
            Assert.Equal(60m, comparison.ThreeDartAverage.Previous);
            Assert.Equal(100m, comparison.WinRate.Current);
            Assert.Equal(0m, comparison.WinRate.Previous);
        }

        [Fact]
        public void PersonalBest_IgnoresUnitsWithTooFewDarts()
        {
            // 3 Darts mit 180 zählen nicht (unter 30 Darts); ein Match mit 30 Darts à 20 Punkten schon.
            var shortUnit = Leg("short", DateTime.UtcNow, StatisticsCategoryEnum.Training, 501, X01OutModeEnum.Double, false, V("T20", "T20", "T20"));
            var visits = Enumerable.Range(0, 10).Select(_ => V("S20", "0", "0")).ToArray();
            var longUnit = Leg("long", DateTime.UtcNow, StatisticsCategoryEnum.Training, 501, X01OutModeEnum.Double, false, visits);

            var best = StatisticsCalculator.PersonalBests([shortUnit, longUnit]);

            Assert.Equal(20m, best.ThreeDartAverage);
        }

        [Fact]
        public void ScoreDistribution_PutsEachVisitInExactlyOneBand()
        {
            var scoring = StatisticsCalculator.Scoring([NineDarter]);

            Assert.Equal(3, scoring.Distribution.Sum(b => b.Count));
            Assert.Equal(2, scoring.Distribution.Single(b => b.Label == "180").Count);
            Assert.Equal(1, scoring.Distribution.Single(b => b.Min == 140).Count);
        }

        [Fact]
        public void Insights_NeedEnoughLegs_AndNameTheWeakness()
        {
            var kpis = StatisticsCalculator.Kpis([NineDarter], [], [], []);
            kpis.ThreeDartAverage.Current = 60; kpis.ThreeDartAverage.Previous = 55;
            kpis.CheckoutRate.Current = 30; kpis.CheckoutRate.Previous = 40;
            kpis.FirstNineAverage.Current = 70; kpis.FirstNineAverage.Previous = 65;

            Assert.Empty(PerformanceInsights.Build(kpis, legs: 9, previousLegs: 20));

            var insights = PerformanceInsights.Build(kpis, legs: 12, previousLegs: 15);
            Assert.Contains(insights, i => i.Contains("Checkout-Quote ist aber gesunken"));
        }
    }
}
