using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>Modell aus DartBotModels laden: Datei vorhanden, SHA-256 und FeatureSchemaVersion passen.</summary>
    public static class ModelLoader
    {
        public static async Task<(DartBotModelEntity? Entity, LoadedTargetModel? Model, string? Error)> LoadAsync(
            IUnitOfWork unitOfWork, MlDataStore store, int modelId)
        {
            var entity = await unitOfWork.DartBotUnitOfWork.DartBotModelRepository.GetFirstOrDefaultAsync(m => m.Id == modelId, asNoTracking: true);

            if (entity is null)
            {
                return (null, null, $"Modell {modelId} nicht gefunden.");
            }

            if (entity.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
            {
                return (entity, null, $"Modell {modelId} hat FeatureSchemaVersion {entity.FeatureSchemaVersion}, erwartet {TargetCandidateFeatures.FeatureSchemaVersion}.");
            }

            var path = store.PathOf(entity.StorageKey, mustBeNew: false);

            if (!File.Exists(path) || !string.Equals(MlDataStore.Sha256Of(path), entity.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return (entity, null, $"Modelldatei fehlt oder wurde verändert: {path}");
            }

            return (entity, LoadedTargetModel.Load(path, entity.Sha256), null);
        }
    }
}
