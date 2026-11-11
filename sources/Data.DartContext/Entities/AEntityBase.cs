namespace Data.DartContext.Entities
{
    public abstract class AEntityBase
    {
        public int Id { get; set; }

        /// <summary>Wird beim Speichern automatisch gesetzt (UTC).</summary>
        public DateTime CreatedAt { get; set; }

        public string CreatedBy { get; set; } = null!;

        /// <summary>Wird beim Speichern automatisch gesetzt (UTC).</summary>
        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }
    }
}
