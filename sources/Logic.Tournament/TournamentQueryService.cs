using Microsoft.EntityFrameworkCore;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Tournament;

namespace Logic.Tournament
{
    public class TournamentQueryService : TournamentServiceBase, ITournamentQueryService
    {
        public TournamentQueryService(IUnitOfWork unitOfWork, IMessageLog messageLog, IClaimsHelper claimsHelper) : base(unitOfWork, messageLog, claimsHelper)
        {
        }

        public async Task<List<TournamentListItem>> GetTournamentsAsync()
        {
            try
            {
                var user = ClaimsHelper.GetCurrentUserFromClaims();

                if (user is null || user.UserId == 0)
                {
                    throw new UnauthorizedAccessException("Current user not found.");
                }

                var tournaments = await Tournaments.TournamentRepository.GetWhereAsync(
                    t => t.Kind == TournamentKindEnum.Official || t.OwnerUserId == user.UserId,
                    asNoTracking: true,
                    include: q => q.Include(t => t.Participants));

                return tournaments
                    .OrderBy(t => t.Status == TournamentStatusEnum.Finished || t.Status == TournamentStatusEnum.Cancelled)
                    .ThenByDescending(t => t.CreatedAt)
                    .Select(t => new TournamentListItem
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Type = t.Type,
                        Kind = t.Kind,
                        Status = t.Status,
                        ParticipantCount = t.Participants.Count(p => p.Status == TournamentParticipantStatusEnum.Confirmed),
                        CreatedAt = t.CreatedAt,
                        StartedAt = t.StartedAt,
                        FinishedAt = t.FinishedAt,
                        WinnerName = t.Participants.FirstOrDefault(p => p.Id == t.WinnerParticipantId)?.DisplayName,
                        CanManage = TournamentAccess.CanManage(user, t),
                        MyStatus = t.Participants.FirstOrDefault(p => p.UserId == user.UserId && p.Status != TournamentParticipantStatusEnum.Withdrawn)?.Status
                    })
                    .ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading tournaments.", nameof(TournamentQueryService), exception);
                throw;
            }
        }

        public async Task<TournamentCandidates> GetCandidatesAsync()
        {
            try
            {
                var user = ClaimsHelper.GetCurrentUserFromClaims();
                var isGameMaster = user?.Roles?.Contains(UserRoleEnum.GameMaster) == true;
                var isPlayer = user?.Roles?.Contains(UserRoleEnum.Player) == true;

                if (user is null || user.UserId == 0 || (!isGameMaster && !isPlayer))
                {
                    throw new UnauthorizedAccessException("Only game masters and players can choose tournament participants.");
                }

                var bots = await UnitOfWork.DartBotUnitOfWork.DartBotRepository.GetWhereAsync(b => b.IsActive, asNoTracking: true);

                var players = await UnitOfWork.UserUnitOfWork.UserRepository.GetWhereAsync(
                    u => u.IsActive && u.DeletedAt == null
                         && u.UserRoles.Any(r => r.Role.UserRole == UserRoleEnum.Player)
                         && (isGameMaster || u.Id == user.UserId),
                    asNoTracking: true);

                return new TournamentCandidates
                {
                    Players = players
                        .Select(u => new TournamentCandidate { Id = u.Id, DisplayName = u.Alias, Rating = u.Rating })
                        .OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                        .ToList(),
                    Bots = bots
                        .Select(b => new TournamentCandidate { Id = b.Id, DisplayName = b.Name, Rating = b.Rating, Level = b.DevelopmentLevel })
                        .OrderBy(c => c.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                        .ToList()
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading tournament candidates.", nameof(TournamentQueryService), exception);
                throw;
            }
        }
    }
}
