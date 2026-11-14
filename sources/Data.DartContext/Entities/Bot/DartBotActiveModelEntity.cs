using Shared.Enums.Bot;

namespace Data.DartContext.Entities.Bot
{
    /// <summary>
    /// Aktives Modell je ModelType (genau eine Zeile pro Typ, Unique auf ModelType).
    /// Ein Modellwechsel ist ein UPDATE dieser Zeile.
    /// </summary>
    public class DartBotActiveModelEntity : AEntityBase
    {
        public DartBotModelTypeEnum ModelType { get; set; }

        public int ModelId { get; set; }

        public DartBotModelEntity Model { get; set; } = null!;

        public DateTime ActivatedAt { get; set; }

        public string? ActivatedBy { get; set; }
    }
}
