using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.User
{
    public class UserCredentialsEntityConfiguration : AEntityBaseConfiguration<UserCredentialsEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<UserCredentialsEntity> builder)
        {
            builder.ToTable("UserCredentials");

            builder.Property(c => c.PasswordHash).HasMaxLength(512).IsRequired();
            builder.Property(c => c.PasswordChangedAt).IsRequired();
            builder.Property(c => c.RefreshTokenHash).HasMaxLength(512);

            // Pro User genau ein Credentials-Datensatz.
            builder.HasIndex(c => c.UserId).IsUnique();

            // Schneller Lookup beim Refresh-Token-Flow.
            builder.HasIndex(c => c.RefreshTokenHash);
        }
    }
}
