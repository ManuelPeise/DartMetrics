using Data.DartContext.Entities.Bot;
using Data.DartContext.Entities.Tournament;
using Data.DartContext.Entities.Training;
using Logic.Shared;
using Logic.Shared.Rules;
using Logic.Statistics.Performance;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.Statistics;
using Shared.Enums.Tournament;
using Shared.Enums.X01;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Statistics;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Statistics;

namespace Logic.Statistics
{
    /// <summary>
    /// Nur lesende Leistungs-Statistik des angemeldeten Spielers.
    /// 1. Lädt alle eigenen X01-Legs aus X01-Training, Bot-Spielen und Turnier-Matches und bringt sie auf eine Form (<see cref="StatLeg"/>).
    /// 2. Filtert nach Kategorie und Zeitraum (nach Ende des Matches bzw. der Session) und rechnet mit <see cref="StatisticsCalculator"/>.
    /// Kategorien: Training = X01-Training + alle Bot-Spiele (+ Übungen), Official = Turnier-Matches, Overall = beides.
    /// Nur beendete Sessions bzw. Matches zählen; abgebrochene Bot-Spiele nicht.
    /// </summary>
    public class PlayerStatisticsService : ALogicBase, IPlayerStatisticsService
    {
        private const int TopTargets = 5;

        private readonly IClaimsHelper _claimsHelper;
        private readonly AroundTheClockRules _aroundTheClockRules = new();

        public PlayerStatisticsService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper)
            : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
        }

        public async Task<PlayerStatistics> GetStatisticsAsync(StatisticsCategoryEnum category, DateTime? fromUtc, DateTime? toUtc)
        {
            if (!Enum.IsDefined(category))
            {
                throw new ArgumentException("Invalid statistics category.", nameof(category));
            }

            if (fromUtc is { } f && toUtc is { } t && f >= t)
            {
                throw new ArgumentException("The start of the period must be before its end.", nameof(fromUtc));
            }

            try
            {
                var userId = GetCurrentUserId();
                var to = toUtc ?? DateTime.UtcNow;

                // Alle Legs der Kategorie über alle Zeit: Bestwerte und "letzte 10 Matches" brauchen den ganzen Verlauf.
                var (allLegs, allMatches, ratings) = await new PlayerHistoryLoader(UnitOfWork).LoadLegsAsync(userId, category);

                var legs = InRange(allLegs, fromUtc, to);
                var matches = InRange(allMatches, fromUtc, to);

                DateTime? previousFrom = null;
                DateTime? previousTo = null;
                List<StatLeg>? previousLegs = null;
                List<StatMatch>? previousMatches = null;

                if (fromUtc is { } from)
                {
                    // Gleich langer Zeitraum direkt davor.
                    previousTo = from;
                    previousFrom = from - (to - from);
                    previousLegs = InRange(allLegs, previousFrom, previousTo.Value);
                    previousMatches = InRange(allMatches, previousFrom, previousTo.Value);
                }

                var exercises = category == StatisticsCategoryEnum.Official
                    ? new ExerciseStatistics()
                    : await LoadExercisesAsync(userId, fromUtc, toUtc);

                var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetByIdAsync(userId, asNoTracking: true);
                var kpis = StatisticsCalculator.Kpis(legs, matches, previousLegs, previousMatches);
                var personalBests = StatisticsCalculator.PersonalBests(allLegs);

                var statistics = new PlayerStatistics
                {
                    Category = category,
                    From = fromUtc,
                    To = toUtc,
                    PreviousFrom = previousFrom,
                    PreviousTo = previousTo,
                    CurrentRating = user.Rating,
                    Kpis = kpis,
                    Scoring = StatisticsCalculator.Scoring(legs),
                    Checkout = StatisticsCalculator.Checkout(legs),
                    LegsAndMatches = StatisticsCalculator.LegsAndMatches(legs, matches),
                    Development = new DevelopmentStatistics
                    {
                        LastTenMatches = StatisticsCalculator.LastMatches(allLegs, allMatches),
                        PersonalBests = personalBests
                    },
                    Insights = PerformanceInsights.Build(kpis, legs.Count(l => l.IsFinished), previousLegs?.Count(l => l.IsFinished) ?? 0),
                    AverageTrend = StatisticsCalculator.AverageTrend(legs),
                    CheckoutTrend = StatisticsCalculator.CheckoutTrend(legs),
                    RatingTrend = ratings
                        .Where(r => (fromUtc == null || r.Date >= fromUtc) && r.Date < to)
                        .OrderBy(r => r.Date)
                        .Select(r => new StatisticsTrendPoint { Date = r.Date, Value = r.Value })
                        .ToList(),
                    Exercises = exercises,
                    TotalDarts = legs.Sum(l => l.Visits.Sum(v => v.Darts.Count)) + ExerciseDarts(exercises)
                };

                statistics.HasData = legs.Count > 0 || exercises.HasData;

                return statistics;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading player statistics.", nameof(PlayerStatisticsService), exception);
                throw;
            }
        }

        private static List<StatLeg> InRange(List<StatLeg> legs, DateTime? from, DateTime to) =>
            legs.Where(l => (from == null || l.Date >= from) && l.Date < to).ToList();

        private static List<StatMatch> InRange(List<StatMatch> matches, DateTime? from, DateTime to) =>
            matches.Where(m => (from == null || m.Date >= from) && m.Date < to).ToList();

        // ---------- Übungen (Checkout-, Segment-, Around-the-Clock-Training) ----------

        private async Task<ExerciseStatistics> LoadExercisesAsync(int userId, DateTime? from, DateTime? to) => new()
        {
            Checkout = BuildCheckout(await LoadCheckoutAsync(userId, from, to)),
            Segment = BuildSegment(await LoadSegmentAsync(userId, from, to)),
            AroundTheClock = BuildAroundTheClock(await LoadAroundTheClockAsync(userId, from, to))
        };

        private static int ExerciseDarts(ExerciseStatistics e) => e.Checkout.Darts + e.Segment.Darts + e.AroundTheClock.Darts;

        private async Task<List<CheckoutTrainingEntity>> LoadCheckoutAsync(int userId, DateTime? from, DateTime? to)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.CheckoutTrainingRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from) && (to == null || t.TrainingSession.FinishedAt < to),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Attempts).ThenInclude(a => a.Throws));

            return trainings.Where(t => t.Attempts.Any(a => a.Throws.Count > 0)).ToList();
        }

        private async Task<List<SegmentTrainingEntity>> LoadSegmentAsync(int userId, DateTime? from, DateTime? to)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.SegmentTrainingRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from) && (to == null || t.TrainingSession.FinishedAt < to),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Throws));

            return trainings.Where(t => t.Throws.Count > 0).ToList();
        }

        private async Task<List<AroundTheClockTrainingEntity>> LoadAroundTheClockAsync(int userId, DateTime? from, DateTime? to)
        {
            var trainings = await UnitOfWork.DartTrainingUnitOfWork.AroundTheClockTrainingRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null &&
                     (from == null || t.TrainingSession.FinishedAt >= from) && (to == null || t.TrainingSession.FinishedAt < to),
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Visits).ThenInclude(v => v.Throws));

            return trainings.Where(t => t.Visits.Any(v => v.Throws.Count > 0)).ToList();
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
                Darts = attempts.Sum(a => a.Attempt.Throws.Count),
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
                Darts = totalDarts,
                HitRate = Percent(totalHits, totalDarts),
                BestDarts = completedDarts.Count == 0 ? null : completedDarts.Min(),
                AverageDarts = completedDarts.Count == 0 ? null : Round((decimal)completedDarts.Average())
            };
        }

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
