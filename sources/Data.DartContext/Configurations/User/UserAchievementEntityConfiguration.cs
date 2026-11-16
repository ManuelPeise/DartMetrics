using Data.DartContext.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.User
{
    public class UserAchievementEntityConfiguration : AEntityBaseConfiguration<UserAchievementEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<UserAchievementEntity> builder)
        {
            builder.ToTable("UserAchievements");

            builder.Property(a => a.AchievementKey).HasMaxLength(60).IsRequired();

            // Jeder Erfolg höchstens einmal je Spieler (fängt auch parallele Auswertungen ab).
            builder.HasIndex(a => new { a.UserId, a.AchievementKey }).IsUnique();

            builder.HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.UseUtcDateTimes();
        }
    }
}
