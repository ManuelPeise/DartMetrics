using Data.DartContext.Configurations.Tournament;
using Data.DartContext.Entities.Tournament;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Enums.X01;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Tournament;

namespace Logic.Tournament
{
    /// <summary>Gemeinsames für Liga und K.-o.: Anlegen, Teilnehmer, Laden mit Rechteprüfung, Übersicht.</summary>
    public abstract class TournamentServiceBase : ALogicBase
    {
        protected readonly IClaimsHelper ClaimsHelper;

        protected TournamentServiceBase(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog)
        {
            ClaimsHelper = claimsHelper;
        }

        protected ITournamentUnitOfWork Tournaments => UnitOfWork.TournamentUnitOfWork;

        protected async Task<int> CreateTournamentAsync(TournamentTypeEnum type, TournamentKindEnum kind, string? name, int startingScore,
            X01InModeEnum inMode, X01OutModeEnum outMode, Action<TournamentEntity>? configure = null)
        {
            var currentUser = ClaimsHelper.GetCurrentUserFromClaims();
            TournamentAccess.EnsureCanCreate(currentUser, kind);

            name = name?.Trim();

            if (string.IsNullOrWhiteSpace(name) || name.Length > TournamentConfiguration.NameMaxLength)
            {
                throw new ArgumentException($"Name is required and may have at most {TournamentConfiguration.NameMaxLength} characters.");
            }

            if (startingScore is not (301 or 501))
            {
                throw new ArgumentException("Starting score must be 301 or 501.");
            }

            var tournament = new TournamentEntity
            {
                Name = name,
                Type = type,
                Kind = kind,
                Status = TournamentStatusEnum.Draft,
                OwnerUserId = currentUser.UserId,
                StartingScore = startingScore,
                InMode = inMode,
                OutMode = outMode
            };

            configure?.Invoke(tournament);

            await Tournaments.TournamentRepository.AddAsync(tournament);
            await UnitOfWork.SaveChangesAsync();

            return tournament.Id;
        }

        protected async Task<int> AddParticipantCoreAsync(TournamentEntity tournament, AddTournamentParticipantModel model, int maxParticipants)
        {
            EnsureNotStarted(tournament);

            if ((model.UserId is null) == (model.DartBotId is null))
            {
                throw new ArgumentException("Exactly one of UserId and DartBotId is required.");
            }

            var active = tournament.Participants.Count(p => p.Status != TournamentParticipantStatusEnum.Withdrawn);

            if (active >= maxParticipants)
            {
                throw new ArgumentException($"This tournament has at most {maxParticipants} participants.");
            }

            var participant = model.UserId is int userId
                ? await NewUserParticipantAsync(tournament, userId)
                : await NewBotParticipantAsync(model.DartBotId!.Value);

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

        protected async Task RemoveParticipantCoreAsync(TournamentEntity tournament, int participantId)
        {
            EnsureNotStarted(tournament);

            var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId)
                ?? throw new ArgumentException($"Participant {participantId} not found.");

            participant.Status = TournamentParticipantStatusEnum.Withdrawn;
            participant.Seed = null;
            tournament.ConcurrencyStamp = Guid.NewGuid();
            await UnitOfWork.SaveChangesAsync();
        }

        protected async Task<TournamentParticipantEntity> NewUserParticipantAsync(TournamentEntity tournament, int userId)
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

        protected async Task<TournamentParticipantEntity> NewBotParticipantAsync(int botId)
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

        protected async Task<TournamentEntity> LoadManagedAsync(int tournamentId, TournamentTypeEnum type, bool tracking)
        {
            var tournament = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                t => t.Id == tournamentId && t.Type == type,
                asNoTracking: !tracking,
                include: q => q.Include(t => t.Participants))
                ?? throw new ArgumentException($"{type} tournament {tournamentId} not found.");

            TournamentAccess.EnsureCanManage(ClaimsHelper.GetCurrentUserFromClaims(), tournament);
            return tournament;
        }

        protected async Task<TournamentEntity> LoadVisibleAsync(int tournamentId, TournamentTypeEnum type)
        {
            var tournament = await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                t => t.Id == tournamentId && t.Type == type,
                asNoTracking: true,
                include: q => q.Include(t => t.Participants).Include(t => t.Rounds).ThenInclude(r => r.Matches))
                ?? throw new ArgumentException($"{type} tournament {tournamentId} not found.");

            TournamentAccess.EnsureCanView(ClaimsHelper.GetCurrentUserFromClaims(), tournament);
            return tournament;
        }

        protected static void EnsureNotStarted(TournamentEntity tournament)
        {
            if (tournament.Status is not (TournamentStatusEnum.Draft or TournamentStatusEnum.Registration))
            {
                throw new InvalidOperationException("The tournament has already started.");
            }
        }

        protected static TournamentOverview ToOverview(TournamentEntity tournament) => new()
        {
            Id = tournament.Id,
            Name = tournament.Name,
            Type = tournament.Type,
            Kind = tournament.Kind,
            Status = tournament.Status,
            StartingScore = tournament.StartingScore,
            InMode = tournament.InMode,
            OutMode = tournament.OutMode,
            StartedAt = tournament.StartedAt,
            FinishedAt = tournament.FinishedAt,
            WinnerParticipantId = tournament.WinnerParticipantId,
            SeedSourceTournamentId = tournament.SeedSourceTournamentId,
            SeedCount = tournament.SeedCount,
            Participants = tournament.Participants
                .Where(p => p.Status != TournamentParticipantStatusEnum.Withdrawn)
                .OrderBy(p => p.Seed ?? int.MaxValue)
                .ThenBy(p => p.DisplayName)
                .Select(p => new TournamentParticipantItem
                {
                    Id = p.Id,
                    ParticipantType = p.ParticipantType,
                    UserId = p.UserId,
                    DartBotId = p.DartBotId,
                    DisplayName = p.DisplayName,
                    Status = p.Status,
                    Seed = p.Seed
                })
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
                    Matches = r.Matches
                        .OrderBy(m => m.MatchNumber)
                        .Select(m => new TournamentMatchItem
                        {
                            Id = m.Id,
                            MatchNumber = m.MatchNumber,
                            Status = m.Status,
                            HomeParticipantId = m.HomeParticipantId,
                            AwayParticipantId = m.AwayParticipantId,
                            HomeLegs = m.HomeLegs,
                            AwayLegs = m.AwayLegs,
                            WinnerParticipantId = m.WinnerParticipantId,
                            NextMatchId = m.NextMatchId,
                            NextMatchSlot = m.NextMatchSlot
                        })
                        .ToList()
                })
                .ToList()
        };

        /// <summary>Termin einer Runde setzen, solange keines ihrer Matches begonnen hat.</summary>
        protected async Task ScheduleRoundCoreAsync(TournamentEntity tournament, int roundId, DateTime scheduledAtUtc)
        {
            if (tournament.Status != TournamentStatusEnum.Running)
            {
                throw new InvalidOperationException("Only running tournaments can be scheduled.");
            }

            var round = await Tournaments.TournamentRoundRepository.GetFirstOrDefaultAsync(
                r => r.Id == roundId && r.TournamentId == tournament.Id,
                include: q => q.Include(r => r.Matches))
                ?? throw new ArgumentException($"Round {roundId} not found in tournament {tournament.Id}.");

            if (round.Matches.Any(m => m.Status is TournamentMatchStatusEnum.Running or TournamentMatchStatusEnum.Finished))
            {
                throw new InvalidOperationException("A round with started matches cannot be rescheduled.");
            }

            round.ScheduledAt = DateTime.SpecifyKind(scheduledAtUtc, DateTimeKind.Utc);
            await UnitOfWork.SaveChangesAsync();
        }

        private static string Truncate(string value) => value.Length <= 100 ? value : value[..100];
    }
}
