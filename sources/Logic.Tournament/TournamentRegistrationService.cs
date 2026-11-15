using Data.DartContext.Entities.Tournament;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Shared;
using Shared.Models.Tournament;
using Logic.Tournament.Knockout;
using Logic.Tournament.League;

namespace Logic.Tournament
{
    public class TournamentRegistrationService : TournamentServiceBase, ITournamentRegistrationService
    {
        public TournamentRegistrationService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task OpenRegistrationAsync(int tournamentId, DateTime? deadlineUtc)
        {
            try
            {
                var tournament = await LoadAsync(tournamentId);
                TournamentAccess.EnsureCanManage(ClaimsHelper.GetCurrentUserFromClaims(), tournament);

                if (tournament.Kind != TournamentKindEnum.Official)
                {
                    throw new BusinessRuleException("Only official tournaments have a registration.");
                }

                EnsureNotStarted(tournament);

                if (deadlineUtc is DateTime deadline && deadline <= DateTime.UtcNow)
                {
                    throw new ArgumentException("The registration deadline must be in the future.");
                }

                tournament.Status = TournamentStatusEnum.Registration;
                tournament.RegistrationDeadline = deadlineUtc is DateTime d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;
                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error opening registration of tournament {tournamentId}.", nameof(TournamentRegistrationService), exception);
                throw;
            }
        }

        public async Task<int> RegisterAsync(int tournamentId)
        {
            try
            {
                var user = ClaimsHelper.GetCurrentUserFromClaims();

                if (user?.Roles?.Contains(UserRoleEnum.Player) != true)
                {
                    throw new UnauthorizedAccessException("Only players can register for tournaments.");
                }

                var tournament = await LoadAsync(tournamentId);

                if (tournament.Kind != TournamentKindEnum.Official || tournament.Status != TournamentStatusEnum.Registration)
                {
                    throw new BusinessRuleException("Registration for this tournament is not open.");
                }

                if (tournament.RegistrationDeadline is DateTime deadline && deadline < DateTime.UtcNow)
                {
                    throw new BusinessRuleException("The registration deadline has passed.");
                }

                var max = tournament.Type == TournamentTypeEnum.League ? LeagueScheduler.MaxParticipants : KnockoutBracketBuilder.MaxParticipants;
                var participantId = await AddParticipantCoreAsync(tournament, new AddTournamentParticipantModel { UserId = user.UserId }, max);

                // Selbst angemeldet: wartet auf die Bestätigung durch den GameMaster.
                var participant = tournament.Participants.Single(p => p.Id == participantId);
                participant.Status = TournamentParticipantStatusEnum.Registered;
                await UnitOfWork.SaveChangesAsync();

                return participantId;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error registering for tournament {tournamentId}.", nameof(TournamentRegistrationService), exception);
                throw;
            }
        }

        public async Task WithdrawAsync(int tournamentId)
        {
            try
            {
                var user = ClaimsHelper.GetCurrentUserFromClaims();
                var tournament = await LoadAsync(tournamentId);

                var own = tournament.Participants.FirstOrDefault(p => p.UserId == user.UserId && p.Status != TournamentParticipantStatusEnum.Withdrawn)
                    ?? throw new BusinessRuleException("You are not registered for this tournament.");

                await RemoveParticipantCoreAsync(tournament, own.Id);
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error withdrawing from tournament {tournamentId}.", nameof(TournamentRegistrationService), exception);
                throw;
            }
        }

        public async Task ConfirmParticipantAsync(int tournamentId, int participantId)
        {
            try
            {
                var tournament = await LoadAsync(tournamentId);
                TournamentAccess.EnsureCanManage(ClaimsHelper.GetCurrentUserFromClaims(), tournament);
                EnsureNotStarted(tournament);

                var participant = tournament.Participants.FirstOrDefault(p => p.Id == participantId)
                    ?? throw new ArgumentException($"Participant {participantId} not found.");

                if (participant.Status != TournamentParticipantStatusEnum.Registered)
                {
                    throw new BusinessRuleException("Only registered participants can be confirmed.");
                }

                participant.Status = TournamentParticipantStatusEnum.Confirmed;
                tournament.ConcurrencyStamp = Guid.NewGuid();
                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error confirming participant {participantId} of tournament {tournamentId}.", nameof(TournamentRegistrationService), exception);
                throw;
            }
        }

        private async Task<TournamentEntity> LoadAsync(int tournamentId) =>
            await Tournaments.TournamentRepository.GetFirstOrDefaultAsync(
                t => t.Id == tournamentId,
                include: q => q.Include(t => t.Participants))
            ?? throw new ArgumentException($"Tournament {tournamentId} not found.");
    }
}
