using Shared.Enums.User;

namespace Shared.Models.Authentication
{
    public class CurrentUser
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public List<UserRoleEnum> Roles { get; set; } = null!;
    }
}
