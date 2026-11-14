using Shared.Enums.User;

namespace Shared.Models.Administration.User
{
    public class ChangeUserRoleModel
    {
        public int UserId { get; set; }

        /// <summary>Nur Player oder GameMaster; die Admin-Rolle kann nicht vergeben werden.</summary>
        public UserRoleEnum Role { get; set; }
    }
}
