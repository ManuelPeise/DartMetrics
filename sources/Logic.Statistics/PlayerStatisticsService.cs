using Data.DartContext.Entities.Bot;
using Data.DartContext.Entities.Tournament;
using Shared.Enums.Tournament;
using Data.DartContext.Entities.Training;
using Logic.Shared;
using Logic.Shared.Rules;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.Statistics;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Statistics;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Statistics;

namespace Logic.Statistics
{
    /// <summary>
    /// Nur lesende Auswertung der eigenen Trainings und Bot-Spiele des angemeldeten Spielers.
    /// - Zeitraum nach Ende (FinishedAt) der Session bzw. des Spiels.
    /// - Trainings zählen nur, wenn sie beendet sind und mindestens einen Dart haben.
    /// - Ist ein 30/90-Tage-Zeitraum leer, wird alles ausgewertet (IsFallbackToAll).
    /// </summary>
    public class PlayerStatisticsService : ALogicBase, IPlayerStatisticsService
    {
        private const int TopTargets = 5;

        /// <summary>Ab so vielen Punkten wird der Verlauf (nur bei All) zu Wochenwerten zusammengefasst.</summary>
        private const int MaxTrendPoints = 100;

        private readonly IClaimsHelper _claimsHelper;
        private readonly AroundTheClockRules _aroundTheClockRules = new();

        public PlayerStatisticsService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper)
            : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
        }

        public async Task<PlayerStatistics> GetStatisticsAsync(StatisticsPeriodEnum period)
        {
            try
            {
                if (!Enum.IsDefined(period))
                {
                    throw new ArgumentException("Invalid statistics period.", nameof(period));
                }

                var userId = GetCurrentUserId();
                var statistics = await BuildAsync(userId, period);

                if (!statistics.HasData && period != StatisticsPeriodEnum.All)
                {
                    statistics = await BuildAsync(userId, StatisticsPeriodEnum.All);
                    statistics.IsFallbackToAll = statistics.HasData;
                }

                return statistics;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading player statistics.", nameof(PlayerStatisticsService), exception);
                throw;
            }
        }

        private async Task<PlayerStatistics> BuildAsync(int userId, StatisticsPeriodEnum period)
        {
            var from = period switch
            {
                StatisticsPeriodEnum.Last30Days => DateTime.UtcNow.AddDays(-30),
                StatisticsPeriodEnum.Last90Days => DateTime.UtcNow.AddDays(-90),
                _ => (DateTime?)null
            };

            // Nacheinander: ein DbContext erlaubt keine parallelen Abfragen.
            var x01 = await LoadX01Async(userId, from);
            var checkout = await LoadCheckoutAsync(userId, from);
            var segment = await LoadSegmentAsync(userId, from);
            var aroundTheClock = await LoadAroundTheClockAsync(userId, from);
            var botPlayers = await LoadBotGamesAsync(userId, from);
            var tournamentMatches = await LoadTournamentMatchesAsync(userId, from);
            var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(userId, asNoTracking: true);

            var statistics = new PlayerStatistics
            {
                Period = period,
                From = from,
                X01 = BuildX01(x01),
                Checkout = BuildCheckout(checkout),
                Segment = BuildSegment(segment),
                AroundTheClock = BuildAroundTheClock(aroundTheClock),
                BotGames = BuildBotGames(botPlayers),
                Tournaments = BuildTournaments(userId, tournamentMatches),
                X01AverageTrend = Compress(period, x01
                    .Select(t => (Date: t.TrainingSession.FinishedAt!.Value, Value: AveragePerVisit(t)))
                    .ToList(), useLastValue: false),
                // Elo aus Bot-Spielen und offiziellen Turnier-Matches, zeitlich gemischt.
                RatingTrend = Compress(period, botPlayers
                    .Where(p => p.RatingAfter != null)
                    .Select(p => (Date: p.Game.FinishedAt!.Value, Value: (decimal)p.RatingAfter!.Value))
                    .Concat(tournamentMatches
                        .Where(m => OwnRatingAfter(userId, m) != null)
                        .Select(m => (Date: m.FinishedAt!.Value, Value: (decimal)OwnRatingAfter(userId, m)!.Value)))
                    .OrderBy(p => p.Date)
                    .ToList(), useLastValue: true)
            };

            statistics.Summary = new SummaryStatistics
            {
                X01Sessions = x01.Count,
                CheckoutSessions = checkout.Count,
                SegmentSessions = segment.Count,
                AroundTheClockSessions = aroundTheClock.Count,
                TrainingSessions = x01.Count + checkout.Count + segment.Count + aroundTheClock.Count,
                TotalDarts = statistics.X01.Darts
                             + checkout.Sum(c => c.Attempts.Sum(a => a.Throws.Count))
                             + statistics.Segment.Darts
                             + aroundTheClock.Sum(a => a.Visits.Sum(v => v.Throws.Count)),
                CurrentRating = user.Rating
            };

            statistics.HasData = statistics.Summary.TrainingSessions > 0 || statistics.BotGames.Games > 0 || statistics.Tournaments.Matches > 0;

            return statistics;
        }

        // ---------- Laden (nur eigene, beendete Sessions im Zeitraum) ----------

        private async Task<List<TrainingGameEntity>> LoadX01Async(int userId, DateTime? from)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.TrainingGameRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Visits).ThenInclude(v => v.Throws));

            return trainings
                .Where(t => t.Visits.Any(v => v.Throws.Count > 0))
                .OrderBy(t => t.TrainingSession.FinishedAt)
                .ToList();
        }

        private async Task<List<CheckoutTrainingEntity>> LoadCheckoutAsync(int userId, DateTime? from)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.CheckoutTrainingRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Attempts).ThenInclude(a => a.Throws));

            return trainings.Where(t => t.Attempts.Any(a => a.Throws.Count > 0)).ToList();
        }

        private async Task<List<SegmentTrainingEntity>> LoadSegmentAsync(int userId, DateTime? from)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.SegmentTrainingRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Throws));

            return trainings.Where(t => t.Throws.Count > 0).ToList();
        }

        private async Task<List<AroundTheClockTrainingEntity>> LoadAroundTheClockAsync(int userId, DateTime? from)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.AroundTheClockTrainingRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Visits).ThenInclude(v => v.Throws));

            return trainings.Where(t => t.Visits.Any(v => v.Throws.Count > 0)).ToList();
        }

        private async Task<List<DartBotGamePlayerEntity>> LoadBotGamesAsync(int userId, DateTime? from)
        {
            // Nur Vorwärts-Includes (Player -> Game, Player -> Visits): No-Tracking erlaubt keine Zyklen.
            var players = await UnitOfWork.DartBotUnitOfWork.DartBotGamePlayerRepository.GetWhereAsync(
                p => p.UserId == userId && p.Game.Status == DartBotGameStatusEnum.Finished && p.Game.FinishedAt != null &&
                     (from == null || p.Game.FinishedAt >= from),
                asNoTracking: true,
                include: q => q.Include(p => p.Game).Include(p => p.Visits));

            return players.OrderBy(p => p.Game.FinishedAt).ToList();
        }

        private async Task<List<TournamentMatchEntity>> LoadTournamentMatchesAsync(int userId, DateTime? from)
        {
            // Nur Vorwärts-Includes (Match -> Teilnehmer, Match -> Legs -> Aufnahmen).
            var matches = await UnitOfWork.TournamentUnitOfWork.TournamentMatchRepository.GetWhereAsync(
                m => m.Status == TournamentMatchStatusEnum.Finished && m.FinishedAt != null &&
                     (m.HomeParticipant!.UserId == userId || m.AwayParticipant!.UserId == userId) &&
                     (from == null || m.FinishedAt >= from),
                asNoTracking: true,
                include: q => q.Include(m => m.HomeParticipant).Include(m => m.AwayParticipant).Include(m => m.Legs).ThenInclude(l => l.Visits));

            return matches.OrderBy(m => m.FinishedAt).ToList();
        }

        // ---------- Auswerten ----------

        private static X01Statistics BuildX01(List<TrainingGameEntity> trainings)
        {
            var visits = trainings.SelectMany(t => t.Visits).Where(v => v.Throws.Count > 0).ToList();
            var darts = visits.Sum(v => v.Throws.Count);
            var points = visits.Sum(VisitPoints);
            // Ein Training ist ein Satz aus mehreren Legs: gezählt wird jedes gecheckte Leg einzeln.
            var finishedLegs = trainings
                .SelectMany(t => t.Visits.GroupBy(v => v.LegNumber))
                .Where(leg => leg.Any(v => v.ScoreAfter == 0 && !v.IsBust))
                .ToList();

            return new X01Statistics
            {
                Sessions = trainings.Count,
                LegsFinished = finishedLegs.Count,
                Darts = darts,
                Visits = visits.Count,
                AveragePerDart = Ratio(points, darts),
                AveragePerVisit = Ratio(points, visits.Count),
                BestVisit = visits.Count == 0 ? 0 : visits.Max(VisitPoints),
                Busts = visits.Count(v => v.IsBust),
                AverageDartsPerLeg = finishedLegs.Count == 0
                    ? null
                    : Round((decimal)finishedLegs.Sum(leg => leg.Sum(v => v.Throws.Count)) / finishedLegs.Count)
            };
        }

        private static CheckoutStatistics BuildCheckout(List<CheckoutTrainingEntity> trainings)
        {
            var attempts = trainings
                .SelectMany(t => t.Attempts.Where(a => a.Throws.Count > 0).Select(a => (t.TargetScore, Attempt: a)))
                .ToList();
            var successful = attempts.Where(a => a.Attempt.IsCheckout).ToList();

            return new CheckoutStatistics
            {
                Sessions = trainings.Count,
                Attempts = attempts.Count,
                Successful = successful.Count,
                Rate = Percent(successful.Count, attempts.Count),
                AverageDartsPerCheckout = successful.Count == 0
                    ? null
                    : Round((decimal)successful.Sum(a => a.Attempt.Throws.Count) / successful.Count),
                ByTarget = attempts
                    .GroupBy(a => a.TargetScore)
                    .Select(g => new CheckoutTargetStatistics
                    {
                        TargetScore = g.Key,
                        Attempts = g.Count(),
                        Successful = g.Count(a => a.Attempt.IsCheckout),
                        Rate = Percent(g.Count(a => a.Attempt.IsCheckout), g.Count())
                    })
                    .OrderByDescending(t => t.Attempts)
                    .ThenBy(t => t.TargetScore)
                    .Take(TopTargets)
                    .ToList()
            };
        }

        private static SegmentStatistics BuildSegment(List<SegmentTrainingEntity> trainings)
        {
            var darts = trainings.Sum(t => t.Throws.Count);
            var hits = trainings.Sum(t => t.Throws.Count(x => x.IsHit));

            return new SegmentStatistics
            {
                Sessions = trainings.Count,
                Darts = darts,
                Hits = hits,
                HitRate = Percent(hits, darts),
                ByTarget = trainings
                    .GroupBy(t => (t.TargetSegment, t.TargetMultiplier))
                    .Select(g =>
                    {
                        var targetDarts = g.Sum(t => t.Throws.Count);
                        var targetHits = g.Sum(t => t.Throws.Count(x => x.IsHit));

                        return new SegmentTargetStatistics
                        {
                            Segment = g.Key.TargetSegment,
                            Multiplier = g.Key.TargetMultiplier,
                            Darts = targetDarts,
                            Hits = targetHits,
                            HitRate = Percent(targetHits, targetDarts)
                        };
                    })
                    .OrderByDescending(t => t.Darts)
                    .Take(TopTargets)
                    .ToList()
            };
        }

        private AroundTheClockStatistics BuildAroundTheClock(List<AroundTheClockTrainingEntity> trainings)
        {
            var totalDarts = 0;
            var totalHits = 0;
            var completedDarts = new List<int>();

            foreach (var training in trainings)
            {
                var target = AroundTheClockRules.FirstTarget;
                var darts = 0;

                foreach (var dart in training.Visits
                             .OrderBy(v => v.VisitNumber)
                             .SelectMany(v => v.Throws.OrderBy(t => t.ThrowNumber)))
                {
                    darts++;

                    if (_aroundTheClockRules.IsTargetHit(target, dart.Segment, dart.Multiplier))
                    {
                        totalHits++;
                        target = _aroundTheClockRules.NextTarget(target);

                        if (target == AroundTheClockRules.FinishedTarget)
                        {
                            completedDarts.Add(darts);
                            break;
                        }
                    }
                }

                totalDarts += darts;
            }

            return new AroundTheClockStatistics
            {
                Sessions = trainings.Count,
                Completed = completedDarts.Count,
                HitRate = Percent(totalHits, totalDarts),
                BestDarts = completedDarts.Count == 0 ? null : completedDarts.Min(),
                AverageDarts = completedDarts.Count == 0 ? null : Round((decimal)completedDarts.Average())
            };
        }

        private static BotGameStatistics BuildBotGames(List<DartBotGamePlayerEntity> players)
        {
            var wins = players.Count(p => p.Game.WinnerPlayerId == p.Id);
            var visits = players.SelectMany(p => p.Visits).ToList();
            var rated = players.Where(p => p.RatingBefore != null && p.RatingAfter != null).ToList();

            return new BotGameStatistics
            {
                Games = players.Count,
                Wins = wins,
                WinRate = Percent(wins, players.Count),
                AveragePerVisit = Ratio(visits.Sum(v => v.IsBust ? 0 : v.ScoreBefore - v.ScoreAfter), visits.Count),
                RatingChange = rated.Count == 0 ? null : rated[^1].RatingAfter!.Value - rated[0].RatingBefore!.Value
            };
        }

        private static TournamentMatchStatistics BuildTournaments(int userId, List<TournamentMatchEntity> matches)
        {
            var wins = matches.Count(m => m.WinnerParticipantId == OwnParticipant(userId, m)?.Id);
            var visits = matches.SelectMany(m => m.Legs.SelectMany(l => l.Visits).Where(v => v.PlayerNumber == OwnNumber(userId, m))).ToList();
            var rated = matches.Where(m => OwnRatingAfter(userId, m) != null).ToList();

            return new TournamentMatchStatistics
            {
                Matches = matches.Count,
                Wins = wins,
                WinRate = Percent(wins, matches.Count),
                AveragePerVisit = Ratio(visits.Sum(v => v.ScoreBefore - v.ScoreAfter), visits.Count),
                RatingChange = rated.Count == 0 ? null : OwnRatingAfter(userId, rated[^1])!.Value - OwnRatingBefore(userId, rated[0])!.Value
            };
        }

        private static int OwnNumber(int userId, TournamentMatchEntity m) => m.HomeParticipant?.UserId == userId ? 1 : 2;

        private static TournamentParticipantEntity? OwnParticipant(int userId, TournamentMatchEntity m) =>
            OwnNumber(userId, m) == 1 ? m.HomeParticipant : m.AwayParticipant;

        private static int? OwnRatingAfter(int userId, TournamentMatchEntity m) => OwnNumber(userId, m) == 1 ? m.HomeRatingAfter : m.AwayRatingAfter;

        private static int? OwnRatingBefore(int userId, TournamentMatchEntity m) => OwnNumber(userId, m) == 1 ? m.HomeRatingBefore : m.AwayRatingBefore;

        // ---------- Hilfen ----------

        /// <summary>Gewertete Punkte einer Aufnahme (Bust und Darts vor der Eröffnung zählen 0).</summary>
        private static int VisitPoints(TrainingGameVisitEntity visit) => visit.ScoreBefore - visit.ScoreAfter;

        private static decimal AveragePerVisit(TrainingGameEntity training)
        {
            var visits = training.Visits.Where(v => v.Throws.Count > 0).ToList();

            return Ratio(visits.Sum(VisitPoints), visits.Count);
        }

        /// <summary>Bei All und vielen Punkten: ein Wert pro Woche (Montag, UTC); Elo = letzter Wert, Average = Mittel.</summary>
        private static List<StatisticsTrendPoint> Compress(
            StatisticsPeriodEnum period, List<(DateTime Date, decimal Value)> points, bool useLastValue)
        {
            var ordered = points.OrderBy(p => p.Date).ToList();

            if (period != StatisticsPeriodEnum.All || ordered.Count <= MaxTrendPoints)
            {
                return ordered.Select(p => new StatisticsTrendPoint { Date = p.Date, Value = p.Value }).ToList();
            }

            return ordered
                .GroupBy(p => WeekStart(p.Date))
                .Select(g => new StatisticsTrendPoint
                {
                    Date = g.Key,
                    Value = useLastValue ? g.Last().Value : Round(g.Average(p => p.Value))
                })
                .ToList();
        }

        private static DateTime WeekStart(DateTime date)
        {
            var offset = ((int)date.DayOfWeek + 6) % 7;
            return DateTime.SpecifyKind(date.Date.AddDays(-offset), DateTimeKind.Utc);
        }

        private static decimal Ratio(int value, int count) => count == 0 ? 0 : Round((decimal)value / count);

        private static decimal Percent(int value, int count) => count == 0 ? 0 : Round(value * 100m / count);

        private static decimal Round(decimal value) => Math.Round(value, 2);

        private int GetCurrentUserId()
        {
            var currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new UnauthorizedAccessException("Current user is not authenticated.");
            }

            return currentUser.UserId;
        }
    }
}
