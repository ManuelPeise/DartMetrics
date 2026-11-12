using Data.DartContext.Entities.Log;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations.Log
{
    public class LogMessageEntityConfiguration : AEntityBaseConfiguration<LogMessageEntity>
    {
        protected override void ConfigureEntity(EntityTypeBuilder<LogMessageEntity> builder)
        {
            builder.ToTable("LogMessages");

            builder.Property(l => l.Message).HasMaxLength(1000).IsRequired();
            builder.Property(l => l.Module).HasMaxLength(200).IsRequired();
            builder.Property(l => l.ExceptionMessage).HasColumnType("text");
            builder.Property(l => l.Stacktrace).HasColumnType("mediumtext");
            builder.Property(l => l.LogMessageType).IsRequired();

            builder.HasIndex(l => l.LogMessageType);
            builder.HasIndex(l => l.CreatedAt);
        }
    }
}
