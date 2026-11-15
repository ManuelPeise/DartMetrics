using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.User
{
    public class UserAvatarEntityConfiguration : AEntityBaseConfiguration<UserAvatarEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<UserAvatarEntity> builder)
        {
            builder.ToTable("UserAvatars");

            builder.Property(a => a.ContentType).HasMaxLength(50).IsRequired();

            // MEDIUMBLOB (bis 16 MB); der Server begrenzt auf AvatarRules.MaxBytes.
            builder.Property(a => a.Data).HasColumnType("mediumblob").IsRequired();

            // Pro User höchstens ein Bild; wird mit dem User gelöscht.
            builder.HasIndex(a => a.UserId).IsUnique();

            builder.HasOne(a => a.User)
                .WithOne()
                .HasForeignKey<UserAvatarEntity>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
