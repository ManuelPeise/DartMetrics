using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    public class CreateDartBotTrainingJobModel
    {
        public DartBotTrainingJobPresetEnum Preset { get; set; } = DartBotTrainingJobPresetEnum.Quick;

        /// <summary>Runde auf Basis dieses Modells (null = Regelwerk in den Rollouts).</summary>
        public int? BaseModelId { get; set; }
    }
}
