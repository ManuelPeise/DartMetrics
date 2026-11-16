using Data.DartContext.Entities.Bot;
using Data.DartContext.Entities.Tournament;
using Data.DartContext.Entities.Training;
using Logic.Shared.Rules;
using Logic.Statistics.Performance;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Enums.Statistics;
using Shared.Enums.Tournament;
using Shared.Enums.X01;
using Shared.Interfaces.UnitsOfWork;

namespace Logic.Statistics
{
    /// <summary>
    /// Lädt alle beendeten X01-Legs eines Spielers (X01-Training, Bot-Spiele, Turnier-Matches) und bringt sie auf eine Form.
    /// Gemeinsam genutzt von Statistik und Fortschritt (Level, Erfolge, Rekorde).
    /// </summary>
    public sealed class PlayerHistoryLoader
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly X01Rules _x01Rules = new();

        public PlayerHistoryLoader(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // ---------- Legs laden und vereinheitlichen ----------

        public async Task<(List<StatLeg> Legs, List<StatMatch> Matches, List<(DateTime Date, decimal Value)> Ratings)> LoadLegsAsync(
            int userId, StatisticsCategoryEnum category)
        {
            var legs = new List<StatLeg>();
            var matches = new List<StatMatch>();
            var ratings = new List<(DateTime, decimal)>();

            // Nacheinander: ein DbContext erlaubt keine parallelen Abfragen.
            if (category != StatisticsCategoryEnum.Official)
            {
                legs.AddRange(await LoadX01TrainingLegsAsync(userId));

                var botGames = await LoadBotGamesAsync(userId);
                legs.AddRange(botGames.SelectMany(BotLegs));
                matches.AddRange(botGames.Select(p => new StatMatch(BotKey(p), p.Game.FinishedAt!.Value, StatisticsCategoryEnum.Training, p.Game.WinnerPlayerId == p.Id)));
                ratings.AddRange(botGames.Where(p => p.RatingAfter != null).Select(p => (p.Game.FinishedAt!.Value, (decimal)p.RatingAfter!.Value)));
            }

            if (category != StatisticsCategoryEnum.Training)
            {
                var tournamentMatches = await LoadTournamentMatchesAsync(userId);
                legs.AddRange(tournamentMatches.SelectMany(m => TournamentLegs(userId, m)));
                matches.AddRange(tournamentMatches.Select(m => new StatMatch(TournamentKey(m), m.FinishedAt!.Value, StatisticsCategoryEnum.Official,
                    m.WinnerParticipantId != null && m.WinnerParticipantId == OwnParticipant(userId, m)?.Id)));
                ratings.AddRange(tournamentMatches.Where(m => OwnRatingAfter(userId, m) != null).Select(m => (m.FinishedAt!.Value, (decimal)OwnRatingAfter(userId, m)!.Value)));
            }

            return (legs, matches, ratings);
        }

        /// <summary>X01-Training allein: Reststand je Dart wird aus der Aufnahme abgeleitet (Double-In: Darts vor dem Eröffnen zählen 0).</summary>
        private async Task<List<StatLeg>> LoadX01TrainingLegsAsync(int userId)
        {
            var trainings = await _unitOfWork.DartTraining_unitOfWork.TrainingGameRepository.GetWhereAsync(
                t => t.TrainingSession.UserId == userId && t.TrainingSession.FinishedAt != null,
                asNoTracking: true,
                include: q => q.Include(t => t.TrainingSession).Include(t => t.Visits).ThenInclude(v => v.Throws));

            var legs = new List<StatLeg>();

            foreach (var training in trainings)
            {
                var date = training.TrainingSession.FinishedAt!.Value;
                var key = $"x01-{training.TrainingSessionId}";

                foreach (var leg in training.Visits.Where(v => v.Throws.Count > 0).GroupBy(v => v.LegNumber).OrderBy(g => g.Key))
                {
                    var visits = leg.OrderBy(v => v.VisitNumber).Select(v => X01TrainingVisit(training, v)).ToList();
                    var finished = visits.Any(v => v.IsCheckout);

                    legs.Add(new StatLeg(key, date, StatisticsCategoryEnum.Training, training.OutMode, finished, finished, visits));
                }
            }

            return legs;
        }

        private StatVisit X01TrainingVisit(TrainingGameEntity training, TrainingGameVisitEntity visit)
        {
            var opened = training.InMode == X01InModeEnum.Straight || visit.ScoreBefore < training.StartingScore;
            var remaining = visit.ScoreBefore;
            var darts = new List<StatDart>();

            foreach (var dart in visit.Throws.OrderBy(t => t.ThrowNumber))
            {
                darts.Add(new StatDart(dart.Segment, dart.Multiplier, remaining));

                if (!opened && _x01Rules.IsOpeningDart(dart.Segment, dart.Multiplier, training.InMode))
                {
                    opened = true;
                }

                if (opened)
                {
                    remaining -= dart.Score;
                }
            }

            var isCheckout = visit.ScoreAfter == 0 && !visit.IsBust;

            return new StatVisit(visit.IsBust ? 0 : visit.ScoreBefore - visit.ScoreAfter, visit.IsBust, isCheckout, visit.ScoreBefore, darts);
        }

        public async Task<List<DartBotGamePlayerEntity>> LoadBotGamesAsync(int userId)
        {
            // Nur Vorwärts-Includes: No-Tracking erlaubt keine Zyklen.
            var players = await _unitOfWork.DartBot_unitOfWork.DartBotGamePlayerRepository.GetWhereAsync(
                p => p.UserId == userId && p.Game.Status == DartBotGameStatusEnum.Finished && p.Game.FinishedAt != null,
                asNoTracking: true,
                include: q => q.Include(p => p.Game).ThenInclude(g => g.Legs).Include(p => p.Visits).ThenInclude(v => v.Throws));

            return players.OrderBy(p => p.Game.FinishedAt).ToList();
        }

        private static IEnumerable<StatLeg> BotLegs(DartBotGamePlayerEntity player)
        {
            var legsById = player.Game.Legs.ToDictionary(l => l.Id);

            return player.Visits
                .GroupBy(v => v.DartBotGameLegId)
                .Where(g => legsById.ContainsKey(g.Key))
                .OrderBy(g => legsById[g.Key].LegNumber)
                .Select(g =>
                {
                    var leg = legsById[g.Key];
                    var visits = g.OrderBy(v => v.VisitNumber)
                        .Select(v => new StatVisit(
                            v.IsBust ? 0 : v.ScoreBefore - v.ScoreAfter, v.IsBust, v.IsCheckout, v.ScoreBefore,
                            v.Throws.OrderBy(t => t.ThrowNumber).Select(t => new StatDart(t.Segment, t.Multiplier, t.ScoreBeforeThrow)).ToList()))
                        .ToList();

                    return new StatLeg(BotKey(player), player.Game.FinishedAt!.Value, StatisticsCategoryEnum.Training, player.Game.OutMode,
                        leg.WinnerPlayerId != null, leg.WinnerPlayerId == player.Id, visits);
                });
        }

        public async Task<List<TournamentMatchEntity>> LoadTournamentMatchesAsync(int userId)
        {
            var matches = await _unitOfWork.Tournament_unitOfWork.TournamentMatchRepository.GetWhereAsync(
                m => m.Status == TournamentMatchStatusEnum.Finished && m.FinishedAt != null &&
                     (m.HomeParticipant!.UserId == userId || m.AwayParticipant!.UserId == userId),
                asNoTracking: true,
                include: q => q.Include(m => m.Round).ThenInclude(r => r.Tournament)
                    .Include(m => m.HomeParticipant).Include(m => m.AwayParticipant)
                    .Include(m => m.Legs).ThenInclude(l => l.Visits).ThenInclude(v => v.Throws));

            return matches.OrderBy(m => m.FinishedAt).ToList();
        }

        private static IEnumerable<StatLeg> TournamentLegs(int userId, TournamentMatchEntity match)
        {
            var own = OwnNumber(userId, match);

            return match.Legs
                .OrderBy(l => l.LegNumber)
                .Select(leg =>
                {
                    var visits = leg.Visits
                        .Where(v => v.PlayerNumber == own)
                        .OrderBy(v => v.VisitNumber)
                        .Select(v => new StatVisit(
                            v.IsBust ? 0 : v.ScoreBefore - v.ScoreAfter, v.IsBust, v.IsCheckout, v.ScoreBefore,
                            v.Throws.OrderBy(t => t.ThrowNumber).Select(t => new StatDart(t.Segment, t.Multiplier, t.ScoreBeforeThrow)).ToList()))
                        .ToList();

                    return new StatLeg(TournamentKey(match), match.FinishedAt!.Value, StatisticsCategoryEnum.Official, match.Round.Tournament.OutMode,
                        leg.WinnerPlayerNumber != null, leg.WinnerPlayerNumber == own, visits);
                })
                .Where(l => l.Visits.Count > 0);
        }

        public static string BotKey(DartBotGamePlayerEntity player) => $"bot-{player.DartBotGameId}";

        public static string TournamentKey(TournamentMatchEntity match) => $"tournament-{match.Id}";

        public static int OwnNumber(int userId, TournamentMatchEntity m) => m.HomeParticipant?.UserId == userId ? 1 : 2;

        public static TournamentParticipantEntity? OwnParticipant(int userId, TournamentMatchEntity m) =>
            OwnNumber(userId, m) == 1 ? m.HomeParticipant : m.AwayParticipant;

        private static int? OwnRatingAfter(int userId, TournamentMatchEntity m) => OwnNumber(userId, m) == 1 ? m.HomeRatingAfter : m.AwayRatingAfter;

    }
}
