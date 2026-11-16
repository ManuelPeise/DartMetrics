using Data.DartContext.Entities.Bot;
using Shared.Models.Bot;

namespace Logic.Bot.Games
{
    /// <summary>Kennzahlen des Steckbriefs aus den gespeicherten Würfen der beendeten Spiele.</summary>
    public static class BotProfileCardBuilder
    {
        /// <summary>Unterschied (Ø) zwischen den letzten und den vorherigen Spielen, ab dem eine Tendenz angezeigt wird.</summary>
        public const double TrendThreshold = 2.0;

        private const int TrendGames = 3;

        public static DartBotProfileCard Build(DartBotEntity bot, DartBotProfileVersionEntity profile, IReadOnlyCollection<DartBotGameEntity> games, int recentGames)
        {
            var botGames = games
                .Select(g => (Game: g, Player: g.Players.First(p => p.DartBotId == bot.Id)))
                .OrderBy(x => x.Game.FinishedAt ?? x.Game.StartedAt)
                .ToList();

            var visits = botGames
                .SelectMany(x => x.Game.Legs.SelectMany(l => l.Visits.Where(v => v.DartBotGamePlayerId == x.Player.Id)))
                .ToList();

            var darts = visits.Sum(v => v.DartsUsed);
            var points = visits.Sum(v => v.ScoreBefore - v.ScoreAfter);

            // First-9: die ersten drei eigenen Aufnahmen je Leg.
            var first9 = botGames
                .SelectMany(x => x.Game.Legs.Select(l => l.Visits.Where(v => v.DartBotGamePlayerId == x.Player.Id).OrderBy(v => v.TurnNumber).Take(3).ToList()))
                .SelectMany(v => v)
                .ToList();
            var first9Darts = first9.Sum(v => v.DartsUsed);

            var doubleAttempts = visits.SelectMany(v => v.Throws).Where(IsFinishingDoubleAttempt).ToList();

            var card = new DartBotProfileCard
            {
                Id = bot.Id,
                Name = bot.Name,
                BaseAverage = profile.BaseAverage,
                PotentialAverage = bot.PotentialAverage,
                StageName = profile.BaseAverage is { } average ? DartBotStages.NameOf(DartBotStages.StageFor(average)) : null,
                Rating = bot.Rating,
                PlayingStyle = profile.PlayingStyle,
                Form = profile.ThrowConsistency,
                Pressure = profile.PressureResistance,
                Nerves = 1f - profile.Nervousness,
                RouteKnowledge = profile.TacticalIntelligence,
                Risk = profile.RiskTaking,
                GamesPlayed = botGames.Count,
                GamesWon = botGames.Count(x => x.Game.WinnerPlayerId == x.Player.Id),
                Average = darts == 0 ? null : Math.Round(points * 3.0 / darts, 2),
                First9Average = first9Darts == 0 ? null : Math.Round(first9.Sum(v => v.ScoreBefore - v.ScoreAfter) * 3.0 / first9Darts, 2),
                CheckoutRate = doubleAttempts.Count == 0 ? null : Math.Round(doubleAttempts.Count(t => t.Segment == t.TargetSegment && t.Multiplier == 2) / (double)doubleAttempts.Count, 4),
                HighestFinish = visits.Where(v => v.IsCheckout).Select(v => (int?)v.ScoreBefore).Max(),
                OneEighties = visits.Count(v => v.ScoreBefore - v.ScoreAfter == 180),
                RecentGames = botGames.TakeLast(recentGames).Select(x => new DartBotProfileCardGame
                {
                    GameId = x.Game.Id,
                    PlayedAt = x.Game.FinishedAt ?? x.Game.StartedAt,
                    OpponentName = x.Game.Players.FirstOrDefault(p => p.Id != x.Player.Id)?.DisplayName ?? "",
                    IsWin = x.Game.WinnerPlayerId == x.Player.Id,
                    LegsWon = x.Game.Legs.Count(l => l.WinnerPlayerId == x.Player.Id),
                    LegsLost = x.Game.Legs.Count(l => l.WinnerPlayerId != null && l.WinnerPlayerId != x.Player.Id),
                    Average = x.Player.AverageInGame,
                    DayForm = x.Player.DayForm
                }).ToList()
            };

            card.FormTrend = Trend(card.RecentGames.Select(g => g.Average).OfType<float>().ToList());
            return card;
        }

        /// <summary>Letzte drei Spiele gegen die drei davor: steigend, stabil oder fallend.</summary>
        public static int Trend(IReadOnlyList<float> averages)
        {
            if (averages.Count < 2 * TrendGames)
            {
                return 0;
            }

            var recent = averages.TakeLast(TrendGames).Average();
            var before = averages.SkipLast(TrendGames).TakeLast(TrendGames).Average();

            return recent - before >= TrendThreshold ? 1 : before - recent >= TrendThreshold ? -1 : 0;
        }

        private static bool IsFinishingDoubleAttempt(DartBotGameThrowEntity dart) =>
            dart.TargetMultiplier == 2 && dart.TargetSegment is { } segment && segment * 2 == dart.ScoreBeforeThrow;
    }
}
