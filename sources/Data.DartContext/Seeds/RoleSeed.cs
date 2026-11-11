using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Enums.User;

namespace Data.DartContext.Seeds
{
    public class RoleSeed : IEntityTypeConfiguration<RoleEntity>
    {
        // Seed data must be constant - DateTime.UtcNow would create a model change on every migration.
        private static readonly DateTime SeedDate = new(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);

        public void Configure(EntityTypeBuilder<RoleEntity> builder)
        {
            builder.HasData(
                new RoleEntity
                {
                    Id = 1,
                    UserRole = UserRoleEnum.Admin,
                    Description = "Administrator role with full access",
                    CreatedAt = SeedDate,
                    CreatedBy = "System"
                },
                new RoleEntity
                {
                    Id = 2,
                    UserRole = UserRoleEnum.GameMaster,
                    Description = "Game Master role with limited access",
                    CreatedAt = SeedDate,
                    CreatedBy = "System"
                },
                new RoleEntity
                {
                    Id = 3,
                    UserRole = UserRoleEnum.Player,
                    Description = "Player role with basic access",
                    CreatedAt = SeedDate,
                    CreatedBy = "System"
                }
            );
        }
    }
}
