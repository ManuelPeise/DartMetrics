using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Shared.Enums.Bot;
using Shared.Interfaces.Tournament;
using Shared.Interfaces.UnitsOfWork;
using Shared.Interfaces.User;

namespace Logic.Shared.Users
{
    /// <summary>Gemeinsame Anonymisierung für die Löschung durch den Admin und durch den Benutzer selbst.</summary>
    public class UserAnonymizer : IUserAnonymizer
    {
        public const string DeletedFirstName = "Gelöschter";
        public const string DeletedLastName = "Benutzer";
        public const string DeletedDisplayName = "Gelöschter Spieler";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ITournamentWithdrawal _tournamentWithdrawal;

        public UserAnonymizer(IUnitOfWork unitOfWork, ITournamentWithdrawal tournamentWithdrawal)
        {
            _unitOfWork = unitOfWork;
            _tournamentWithdrawal = tournamentWithdrawal;
        }

        public async Task AnonymizeAsync(UserEntity user)
        {
            var now = DateTime.UtcNow;

            // Persönliche Daten ersetzen; die E-Mail muss eindeutig bleiben (Unique Index).
            user.FirstName = DeletedFirstName;
            user.LastName = DeletedLastName;
            user.DisplayName = null;
            user.EmailAddress = $"deleted-{user.Id}@deleted.invalid";
            user.IsActive = false;
            user.EmailVerifiedAt = null;
            user.DeletedAt = now;

            // Login-Daten und Refresh-Token entfernen -> keine Anmeldung und keine Token-Erneuerung mehr.
            if (user.UserCredentials is not null)
            {
                _unitOfWork.UserUnitOfWork.UserCredentialsRepository.Delete(user.UserCredentials);
            }

            // Profilbild ist personenbezogen -> löschen.
            var avatar = await _unitOfWork.UserUnitOfWork.UserAvatarRepository.GetFirstOrDefaultAsync(a => a.UserId == user.Id);

            if (avatar is not null)
            {
                _unitOfWork.UserUnitOfWork.UserAvatarRepository.Delete(avatar);
            }

            user.AvatarVersion = null;

            // Bot-Spiele bleiben erhalten (Statistik, ML), nur der Name wird anonymisiert; laufende Spiele enden.
            var botPlayers = await _unitOfWork.DartBotUnitOfWork.DartBotGamePlayerRepository.GetWhereAsync(
                p => p.UserId == user.Id,
                include: q => q.Include(p => p.Game).ThenInclude(g => g.Legs));

            foreach (var player in botPlayers)
            {
                player.DisplayName = DeletedDisplayName;

                if (player.Game.Status == DartBotGameStatusEnum.Running)
                {
                    player.Game.Status = DartBotGameStatusEnum.Aborted;
                    player.Game.FinishedAt = now;

                    foreach (var leg in player.Game.Legs.Where(l => l.FinishedAt == null))
                    {
                        leg.FinishedAt = now;
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Turniere: abmelden bzw. anonymisieren, offene Matches kampflos für den Gegner (eigene Speicherung).
            await _tournamentWithdrawal.WithdrawDeletedUserAsync(user.Id, DeletedDisplayName);
        }
    }
}
