using Data.DartContext.Entities.Training;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Training
{
    public class Bobs27TrainingConfiguration : AEntityBaseConfiguration<Bobs27TrainingEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<Bobs27TrainingEntity> builder)
        {
            builder.ToTable("Bobs27Trainings");

            // 1:1 - eine Session hat höchstens eine Bob's-27-Partie
            builder.HasOne(x => x.TrainingSession)
                .WithOne(x => x.Bobs27Training)
                .HasForeignKey<Bobs27TrainingEntity>(x => x.TrainingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.TrainingSessionId).IsUnique();
        }
    }
}
