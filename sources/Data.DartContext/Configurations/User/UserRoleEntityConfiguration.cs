using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.User
{
    public class UserRoleEntityConfiguration : AEntityBaseConfiguration<UserRoleEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<UserRoleEntity> builder)
        {
            builder.ToTable("UserRoles");

            // Ein User kann dieselbe Rolle nicht doppelt haben.
            builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        }
    }
}
