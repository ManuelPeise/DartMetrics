using Shared.Enums.Bot;

namespace Shared.Models.Bot
{
    public class CreateDartBotTrainingJobModel
    {
        public DartBotTrainingJobPresetEnum Preset { get; set; } = DartBotTrainingJobPresetEnum.Quick;
    }
}
