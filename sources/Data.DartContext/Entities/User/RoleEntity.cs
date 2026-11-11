using Shared.Enums.User;

namespace Data.DartContext.Entities.User
{
    public class RoleEntity : AEntityBase
    {
        public UserRoleEnum UserRole { get; set; }

        public string Description { get; set; } = null!;

        public ICollection<UserRoleEntity> UserRoles { get; set; } = [];
    }
}
