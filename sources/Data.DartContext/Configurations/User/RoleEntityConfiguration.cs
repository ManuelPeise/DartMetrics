using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.User
{
    public class RoleEntityConfiguration : AEntityBaseConfiguration<RoleEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<RoleEntity> builder)
        {
            builder.ToTable("Roles");

            builder.Property(r => r.UserRole).IsRequired();
            builder.Property(r => r.Description).HasMaxLength(255).IsRequired();

            // Jeder Enum-Wert darf nur einmal als Rolle existieren.
            builder.HasIndex(r => r.UserRole).IsUnique();

            // Eine Rolle, die noch vergeben ist, darf nicht gelöscht werden.
            builder.HasMany(r => r.UserRoles)
                .WithOne(ur => ur.Role)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
