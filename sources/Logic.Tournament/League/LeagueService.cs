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
    public class LeagueService : ALogicBase, ILeagueService
    {
        private readonly IClaimsHelper _claimsHelper;

        public LeagueService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
        }

        private ITournamentUnitOfWork Tournaments => UnitOfWork.TournamentUnitOfWork;

        public async Task<int> CreateLeagueAsync(CreateLeagueModel model)
        {
            try
            {
                var currentUser = _claimsHelper.GetCurrentUserFromClaims();
                TournamentAccess.EnsureCanCreate(currentUser, model.Kind);

                var name = model.Name?.Trim();

                if (string.IsNullOrWhiteSpace(name) || name.Length > TournamentConfiguration.NameMaxLength)
                {
                    throw new ArgumentException($"Name is required and may have at most {TournamentConfiguration.NameMaxLength} characters.");
                }

                if (model.StartingScore is not (301 or 501))
                {
                    throw new ArgumentException("Starting score must be 301 or 501.");
                }

                var tournament = new TournamentEntity
                {
                    Name = name,
                    Type = TournamentTypeEnum.League,
                    Kind = model.Kind,
                    Status = TournamentStatusEnum.Draft,
                    OwnerUserId = currentUser.UserId,
                    StartingScore = model.StartingScore,
                    InMode = model.InMode,
                    OutMode = model.OutMode
                };

                await Tournaments.TournamentRepository.AddAsync(tournament);
                await UnitOfWork.SaveChangesAsync();

                return tournament.Id;
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
                var tournament = await LoadManagedLeagueAsync(tournamentId, tracking: true);
                EnsureNotStarted(tournament);

                if ((model.UserId is null) == (model.DartBotId is null))
                {
                    throw new ArgumentException("Exactly one of UserId and DartBotId is required.");
                }

                var active = tournament.Participants.Count(p => p.Status != TournamentParticipantStatusEnum.Withdrawn);

                if (active >= LeagueScheduler.MaxParticipants)
                {
                    throw new ArgumentException($"A league has at most {LeagueScheduler.MaxParticipants} participants.");
                }

                var participant = model.UserId is int userId
                    ? await NewUserParticipantAsync(tournament, userId)
                    : await NewBotParticipantAsync(tournament, model.DartBotId!.Value);

                // Zurückgezogen und erneut hinzugefügt: Eintrag wiederverwenden (Unique Index je Turnier).
                var existing = tournament.Participants.FirstOrDefault(p =>
                    (participant.UserId is not null && p.UserId == participant.UserId) ||
                    (participant.DartBotId is not null && p.DartBotId == participant.DartBotId));

                if (existing is not null)
                {
                    if (existing.Status != TournamentParticipantStatusEnum.Withdrawn)
                    {
                        throw new ArgumentException("This participant is already registered.");
                    }

                    existing.Status = TournamentParticipantStatusEnum.Confirmed;
                    existing.DisplayName = participant.DisplayName;
                    tournament.ConcurrencyStamp = Guid.NewGuid();
                    await UnitOfWork.SaveChangesAsync();
                    return existing.Id;
                }

                tournament.Participants.Add(participant);
                // Teilnehmerliste gehört zum Turnier: Stempel ändern, damit gleichzeitige Änderungen auffallen.
                tournament.ConcurrencyStamp = Guid.NewGuid();
                await UnitOfWork.SaveChangesAsync();

                return participant.Id;
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
                var tournament = await LoadManagedLeagueAsync(tournamentId, tracking: true);
                EnsureNotStarted(tournament);

                var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId)
                    ?? throw new ArgumentException($"Participant {participantId} not found.");

                participant.Status = TournamentParticipantStatusEnum.Withdrawn;
                tournament.ConcurrencyStamp = Guid.NewGuid();
                await UnitOfWork.SaveChangesAsync();
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
                var tournament = await LoadManagedLeagueAsync(tournamentId, tracking: true);
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
                var tournament = await LoadManagedLeagueAsync(tournamentId, tracking: false);

                if (tournament.Status != TournamentStatusEnum.Running)
                {
                    throw new InvalidOperationException("Only running leagues can be scheduled.");
                }

                var round = await Tournaments.TournamentRoundRepository.GetFirstOrDefaultAsync(
                    r => r.Id == roundId && r.TournamentId == tournamentId,
                    include: q => q.Include(r => r.Matches))
                    ?? throw new ArgumentException($"Round {roundId} not found in league {tournamentId}.");

                if (round.Matches.Any(m => m.Status != TournamentMatchStatusEnum.Scheduled))
                {
                    throw new InvalidOperationException("A round with started matches cannot be rescheduled.");
                }

                round.ScheduledAt = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error scheduling round {roundId} of league {tournamentId}.", nameof(LeagueService), exception);
                throw;
            }
        }

        public async Task<LeagueOverview> GetLeagueAsync(int tournamentId)
        {
            try
            {
                var tournament = await LoadVisibleLeagueAsync(tournamentId);

                return new LeagueOverview
                {
                    Id = tournament.Id,
                    Name = tournament.Name,
                    Kind = tournament.Kind,
                    Status = tournament.Status,
                    StartingScore = tournament.StartingScore,
                    InMode = tournament.InMode,
                    OutMode = tournament.OutMode,
                    StartedAt = tournament.StartedAt,
                    FinishedAt = tournament.FinishedAt,
                    WinnerParticipantId = tournament.WinnerParticipantId,
                    Participants = tournament.Participants
                        .Where(p => p.Status != TournamentParticipantStatusEnum.Withdrawn)
                        .OrderBy(p => p.DisplayName)
                        .Select(ToItem)
                        .ToList(),
                    Rounds = tournament.Rounds
                        .OrderBy(r => r.RoundNumber)
                        .Select(r => new TournamentRoundItem
                        {
                            Id = r.Id,
                            RoundNumber = r.RoundNumber,
                            Name = r.Name,
                            BestOfLegs = r.BestOfLegs,
                            ScheduledAt = r.ScheduledAt,
                            Matches = r.Matches.OrderBy(m => m.MatchNumber).Select(ToItem).ToList()
                        })
                        .ToList()
                };
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
                var tournament = await LoadVisibleLeagueAsync(tournamentId);
                return await CalculateTableAsync(tournament);
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

        private async Task<List<LeagueTableRow>> CalculateTableAsync(TournamentEntity tournament)
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

        private async Task<TournamentParticipantEntity> NewUserParticipantAsync(TournamentEntity tournament, int userId)
        {
            var user = await UnitOfWork.UserUnitOfWork.UserRepository.GetFirstOrDefaultAsync(
                u => u.Id == userId,
                asNoTracking: true,
                include: q => q.Include(u => u.UserRoles).ThenInclude(r => r.Role))
                ?? throw new ArgumentException($"User {userId} not found.");

            if (!user.IsActive || user.DeletedAt is not null || !user.UserRoles.Any(r => r.Role.UserRole == UserRoleEnum.Player))
            {
                throw new ArgumentException($"User {userId} is not an active player.");
            }

            // Privat: nur der Besitzer selbst als Mensch, sonst Bots.
            if (tournament.Kind == TournamentKindEnum.Private && userId != tournament.OwnerUserId)
            {
                throw new UnauthorizedAccessException("Private tournaments may only contain their owner and bots.");
            }

            return new TournamentParticipantEntity
            {
                ParticipantType = TournamentParticipantTypeEnum.User,
                UserId = user.Id,
                DisplayName = Truncate(user.Alias),
                Status = TournamentParticipantStatusEnum.Confirmed
            };
        }

        private async Task<TournamentParticipantEntity> NewBotParticipantAsync(TournamentEntity tournament, int botId)
        {
            var bot = await UnitOfWork.DartBotUnitOfWork.DartBotRepository.GetFirstOrDefaultAsync(b => b.Id == botId, asNoTracking: true)
                ?? throw new ArgumentException($"Dart bot {botId} not found.");

            if (!bot.IsActive)
            {
                throw new ArgumentException($"Dart bot {botId} is not active.");
            }

            return new TournamentParticipantEntity
            {
                ParticipantType = TournamentParticipantTypeEnum.Bot,
                DartBotId = bot.Id,
                DisplayName = Truncate(bot.Name),
                Status = TournamentParticipantStatusEnum.Confirmed
            };
        }

        private async Task<TournamentEntity> LoadManagedLeagueAsync(int tournamentId, bool tracking)
        {
            var tournament = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                t => t.Id == tournamentId && t.Type == TournamentTypeEnum.League,
                asNoTracking: !tracking,
                include: q => q.Include(t => t.Participants))
                ?? throw new ArgumentException($"League {tournamentId} not found.");

            TournamentAccess.EnsureCanManage(_claimsHelper.GetCurrentUserFromClaims(), tournament);
            return tournament;
        }

        private async Task<TournamentEntity> LoadVisibleLeagueAsync(int tournamentId)
        {
            var tournament = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                t => t.Id == tournamentId && t.Type == TournamentTypeEnum.League,
                asNoTracking: true,
                include: q => q.Include(t => t.Participants).Include(t => t.Rounds).ThenInclude(r => r.Matches))
                ?? throw new ArgumentException($"League {tournamentId} not found.");

            TournamentAccess.EnsureCanView(_claimsHelper.GetCurrentUserFromClaims(), tournament);
            return tournament;
        }

        private static void EnsureNotStarted(TournamentEntity tournament)
        {
            if (tournament.Status is not (TournamentStatusEnum.Draft or TournamentStatusEnum.Registration))
            {
                throw new InvalidOperationException("The league has already started.");
            }
        }

        private static string Truncate(string value) => value.Length <= 100 ? value : value[..100];

        private static TournamentParticipantItem ToItem(TournamentParticipantEntity p) => new()
        {
            Id = p.Id,
            ParticipantType = p.ParticipantType,
            UserId = p.UserId,
            DartBotId = p.DartBotId,
            DisplayName = p.DisplayName,
            Status = p.Status,
            Seed = p.Seed
        };

        private static TournamentMatchItem ToItem(TournamentMatchEntity m) => new()
        {
            Id = m.Id,
            MatchNumber = m.MatchNumber,
            Status = m.Status,
            HomeParticipantId = m.HomeParticipantId,
            AwayParticipantId = m.AwayParticipantId,
            HomeLegs = m.HomeLegs,
            AwayLegs = m.AwayLegs,
            WinnerParticipantId = m.WinnerParticipantId
        };
    }
}
