using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Data.DartContext.Configurations.Bot
{
    /// <summary>
    /// UTC-Behandlung für die DartBot-Entities (bewusst nicht global, damit Bestandsdaten unberührt bleiben).
    /// MySQL speichert DateTime ohne Zeitzone: beim Lesen wird DateTimeKind.Utc gesetzt,
    /// beim Schreiben werden lokale Zeiten nach UTC umgerechnet.
    /// Gilt auch für DateTime?-Properties (EF Core reicht NULL nie an den Converter weiter).
    /// </summary>
    internal static class BotUtcDateTimeExtensions
    {
        private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
            v => ToUtc(v),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        public static void UseUtcDateTimes<TEntity>(this EntityTypeBuilder<TEntity> builder)
            where TEntity : class
        {
            foreach (var property in builder.Metadata.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(UtcConverter);
                }
            }
        }

        private static DateTime ToUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Local => value.ToUniversalTime(),
                // Unspecified wird als UTC interpretiert (Code verwendet DateTime.UtcNow).
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}
