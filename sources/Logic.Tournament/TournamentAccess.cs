using Data.DartContext.Entities.Tournament;
using Shared.Enums.Tournament;
using Shared.Enums.User;
using Shared.Models.Authentication;

namespace Logic.Tournament
{
    /// <summary>
    /// Rechte (immer im Server): offizielle Turniere verwaltet jeder GameMaster, private nur ihr Besitzer (Player).
    /// Ansehen: offizielle alle angemeldeten Benutzer, private nur der Besitzer.
    /// </summary>
    internal static class TournamentAccess
    {
        public static void EnsureCanCreate(CurrentUser? user, TournamentKindEnum kind)
        {
            EnsureSignedIn(user);

            var allowed = kind switch
            {
                TournamentKindEnum.Official => user!.Roles.Contains(UserRoleEnum.GameMaster),
                TournamentKindEnum.Private => user!.Roles.Contains(UserRoleEnum.Player),
                _ => false
            };

            if (!allowed)
            {
                throw new UnauthorizedAccessException(kind == TournamentKindEnum.Official
                    ? "Only game masters can create official tournaments."
                    : "Only players can create private tournaments.");
            }
        }

        public static void EnsureCanManage(CurrentUser? user, TournamentEntity tournament)
        {
            if (!CanManage(user, tournament))
            {
                throw new UnauthorizedAccessException("You may not manage this tournament.");
            }
        }

        public static void EnsureCanView(CurrentUser? user, TournamentEntity tournament)
        {
            EnsureSignedIn(user);

            if (tournament.Kind == TournamentKindEnum.Private && tournament.OwnerUserId != user!.UserId)
            {
                throw new UnauthorizedAccessException("You may not view this tournament.");
            }
        }

        public static bool CanManage(CurrentUser? user, TournamentEntity tournament)
        {
            if (user is null || user.UserId == 0 || user.Roles is null)
            {
                return false;
            }

            return tournament.Kind == TournamentKindEnum.Official
                ? user.Roles.Contains(UserRoleEnum.GameMaster)
                : user.Roles.Contains(UserRoleEnum.Player) && tournament.OwnerUserId == user.UserId;
        }

        private static void EnsureSignedIn(CurrentUser? user)
        {
            if (user is null || user.UserId == 0 || user.Roles is null)
            {
                throw new UnauthorizedAccessException("Current user not found.");
            }
        }
    }
}
