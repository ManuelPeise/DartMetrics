namespace Data.DartContext.Entities.User
{
    public class UserRoleEntity : AEntityBase
    {
        public int UserId { get; set; }

        public UserEntity User { get; set; } = null!;

        public int RoleId { get; set; }

        public RoleEntity Role { get; set; } = null!;
    }
}
