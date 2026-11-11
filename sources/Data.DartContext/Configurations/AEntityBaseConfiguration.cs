using Data.DartContext.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.DartContext.Configurations
{
    /// <summary>
    /// Gemeinsame Konfiguration für alle Entities, die von <see cref="AEntityBase"/> erben.
    /// </summary>
    public abstract class AEntityBaseConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
        where TEntity : AEntityBase
    {
        public const int AuditUserMaxLength = 100;

        public void Configure(EntityTypeBuilder<TEntity> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.CreatedAt).IsRequired();
            builder.Property(e => e.CreatedBy).HasMaxLength(AuditUserMaxLength).IsRequired();
            builder.Property(e => e.UpdatedBy).HasMaxLength(AuditUserMaxLength);

            ConfigureEntity(builder);
        }

        protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
    }
}
