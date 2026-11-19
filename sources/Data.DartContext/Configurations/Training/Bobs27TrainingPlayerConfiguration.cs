using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class Bobs27TrainingPlayerConfiguration : AEntityBaseConfiguration<Bobs27TrainingPlayerEntity>
    {
        public const int NameMaxLength = 40;
        public const int MaxPlayers = 8;

        protected override void ConfigureEntity(EntityTypeBuilder<Bobs27TrainingPlayerEntity> builder)
        {
            builder.ToTable("Bobs27TrainingPlayers", table =>
                table.HasCheckConstraint("CK_Bobs27TrainingPlayer_PlayerNumber", $"`PlayerNumber` BETWEEN 1 AND {MaxPlayers}"));

            builder.Property(x => x.Name).HasMaxLength(NameMaxLength).IsRequired();

            builder.HasOne(x => x.Bobs27Training)
                .WithMany(x => x.Players)
                .HasForeignKey(x => x.Bobs27TrainingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.Bobs27TrainingId, x.PlayerNumber }).IsUnique();
        }
    }
}
