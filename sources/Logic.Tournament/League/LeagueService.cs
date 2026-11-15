using Data.DartContext.Configurations.Tournament;
using Data.DartContext.Entities.Tournament;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Shared;
using Shared.Models.Tournament;

namespace Logic.Tournament.League
{
    public class LeagueService : TournamentServiceBase, ILeagueService
    {
        public LeagueService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task<int> CreateLeagueAsync(CreateLeagueModel model)
        {
            try
            {
                return await CreateTournamentAsync(TournamentTypeEnum.League, model.Kind, model.Name, model.StartingScore, model.InMode, model.OutMode);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error creating league.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task<int> AddParticipantAsync(int tournamentId, AddTournamentParticipantModel model)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.League, tracking: true);
                return await AddParticipantCoreAsync(tournament, model, LeagueScheduler.MaxParticipants);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error adding participant to league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task RemoveParticipantAsync(int tournamentId, int participantId)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.League, tracking: true);
                await RemoveParticipantCoreAsync(tournament, participantId);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error removing participant {participantId} from league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task StartLeagueAsync(int tournamentId, StartLeagueModel model)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.League, tracking: true);
                EnsureNotStarted(tournament);

                if (!X01SetRules.AllowedBestOfLegs.Contains(model.BestOfLegs))
                {
                    throw new ArgumentException($"Best of {model.BestOfLegs} legs is not allowed.");
                }

                var confirmed = tournament.Participants
                    .Where(p => p.Status == TournamentParticipantStatusEnum.Confirmed)
                    .OrderBy(p => p.Id)
                    .ToList();

                if (confirmed.Count is < LeagueScheduler.MinParticipants or > LeagueScheduler.MaxParticipants)
                {
                    throw new ArgumentException($"A league needs {LeagueScheduler.MinParticipants} to {LeagueScheduler.MaxParticipants} confirmed participants (has {confirmed.Count}).");
                }

                // Reihenfolge im Kreis zufällig, damit der Spielplan nicht von der Anmeldereihenfolge abhängt.
                var order = confirmed.Select(p => p.Id).OrderBy(_ => Random.Shared.Next()).ToList();
                var days = LeagueScheduler.CreateDoubleRoundRobin(order);

                var roundNumber = 0;
                foreach (var day in days)
                {
                    roundNumber++;
                    var round = new TournamentRoundEntity
                    {
                        RoundNumber = roundNumber,
                        Name = $"Spieltag {roundNumber}",
                        BestOfLegs = model.BestOfLegs
                    };

                    var matchNumber = 0;
                    foreach (var pairing in day.Where(p => p.Away is not null))
                    {
                        matchNumber++;
                        round.Matches.Add(new TournamentMatchEntity
                        {
                            MatchNumber = matchNumber,
                            Status = TournamentMatchStatusEnum.Scheduled,
                            HomeParticipantId = pairing.Home,
                            AwayParticipantId = pairing.Away
                        });
                    }

                    tournament.Rounds.Add(round);
                }

                tournament.Status = TournamentStatusEnum.Running;
                tournament.StartedAt = DateTime.UtcNow;

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error starting league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task ScheduleRoundAsync(int tournamentId, int roundId, DateTime scheduledAtUtc)
        {
            try
            {
                var tournament = await LoadManagedAsync(tournamentId, TournamentTypeEnum.League, tracking: false);
                await ScheduleRoundCoreAsync(tournament, roundId, scheduledAtUtc);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error scheduling round {roundId} of league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task<TournamentOverview> GetLeagueAsync(int tournamentId)
        {
            try
            {
                return ToOverview(await LoadVisibleAsync(tournamentId, TournamentTypeEnum.League));
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error loading league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task<List<LeagueTableRow>> GetTableAsync(int tournamentId)
        {
            try
            {
                return await CalculateTableAsync(await LoadVisibleAsync(tournamentId, TournamentTypeEnum.League));
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error loading table of league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task<bool> FinishLeagueIfCompleteAsync(int tournamentId)
        {
            try
            {
                var tournament = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                    t => t.Id == tournamentId && t.Type == TournamentTypeEnum.League,
                    include: q => q.Include(t => t.Participants).Include(t => t.Rounds).ThenInclude(r => r.Matches))
                    ?? throw new ArgumentException($"League {tournamentId} not found.");

                if (tournament.Status != TournamentStatusEnum.Running)
                {
                    return tournament.Status == TournamentStatusEnum.Finished;
                }

                if (tournament.Rounds.SelectMany(r => r.Matches).Any(m => m.Status != TournamentMatchStatusEnum.Finished))
                {
                    return false;
                }

                var table = await CalculateTableAsync(tournament);

                tournament.Status = TournamentStatusEnum.Finished;
                tournament.FinishedAt = DateTime.UtcNow;
                tournament.WinnerParticipantId = table.First().ParticipantId;

                await UnitOfWork.SaveChangesAsync();
                return true;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error finishing league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        /// <summary>Tabelle aus den beendeten Matches; auch für die Setzliste des K.-o.-Systems.</summary>
        internal async Task<List<LeagueTableRow>> CalculateTableAsync(TournamentEntity tournament)
        {
            var participants = tournament.Participants
                .Where(p => p.Status == TournamentParticipantStatusEnum.Confirmed)
                .ToList();

            var finished = tournament.Rounds
                .SelectMany(r => r.Matches)
                .Where(m => m.Status == TournamentMatchStatusEnum.Finished)
                .ToList();

            var finishedIds = finished.Select(m => m.Id).ToList();

            // Punkte und Darts je Match und Seite für den Average (Bust und Darts vor Double-In zählen 0).
            var visits = finishedIds.Count == 0
                ? new List<TournamentMatchVisitEntity>()
                : await Tournaments.TournamentMatchVisitRepository.GetWhereAsync(
                    v => finishedIds.Contains(v.Leg.TournamentMatchId),
                    asNoTracking: true,
                    include: q => q.Include(v => v.Leg));

            var sums = visits
                .GroupBy(v => (v.Leg.TournamentMatchId, v.PlayerNumber))
                .ToDictionary(g => g.Key, g => (Scored: g.Sum(v => v.ScoreBefore - v.ScoreAfter), Darts: g.Sum(v => v.DartsUsed)));

            var results = finished.Select(m =>
            {
                var home = sums.GetValueOrDefault((m.Id, 1));
                var away = sums.GetValueOrDefault((m.Id, 2));
                return new LeagueMatchResult(m.HomeParticipantId!.Value, m.AwayParticipantId!.Value, m.HomeLegs, m.AwayLegs,
                    home.Scored, home.Darts, away.Scored, away.Darts);
            });

            var types = participants.ToDictionary(p => p.Id, p => p.ParticipantType);

            return LeagueTableCalculator
                .Calculate(participants.ToDictionary(p => p.Id, p => p.DisplayName), results)
                .Select(e => new LeagueTableRow
                {
                    Position = e.Position,
                    ParticipantId = e.ParticipantId,
                    DisplayName = e.DisplayName,
                    ParticipantType = types[e.ParticipantId],
                    Played = e.Played,
                    Won = e.Won,
                    Lost = e.Lost,
                    Points = e.Points,
                    LegsFor = e.LegsFor,
                    LegsAgainst = e.LegsAgainst,
                    LegDifference = e.LegDifference,
                    Average = e.Average
                })
                .ToList();
        }
    }
}
