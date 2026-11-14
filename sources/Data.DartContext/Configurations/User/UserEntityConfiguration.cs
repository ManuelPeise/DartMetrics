using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.User
{
    public class UserEntityConfiguration : AEntityBaseConfiguration<UserEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("Users", table => table.HasCheckConstraint("CK_User_Rating", "`Rating` >= 0"));

            builder.Property(u => u.Rating).HasDefaultValue(1000);

            builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            builder.Property(u => u.DisplayName).HasMaxLength(100);
            builder.Property(u => u.EmailAddress).HasMaxLength(255).IsRequired();

            // E-Mail ist Login-Kriterium -> eindeutig (MySQL-Standardkollation ist case-insensitive).
            builder.HasIndex(u => u.EmailAddress).IsUnique();

            builder.Ignore(u => u.Alias);

            // 1:1 User <-> Credentials, Credentials werden mit dem User gelöscht.
            builder.HasOne(u => u.UserCredentials)
                .WithOne(c => c.User)
                .HasForeignKey<UserCredentialsEntity>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 1:n User -> Rollenzuordnungen, Zuordnungen werden mit dem User gelöscht.
            builder.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
