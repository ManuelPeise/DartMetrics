using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Data.DartContext.Entities.Bot;
using Shared.Enums.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot.Ml;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Phase 5: setzt das aktive Zielwahl-Modell (DartBotActiveModels) bzw. entfernt es.
    /// Neue Bot-Spiele merken sich das Modell beim Start; laufende Spiele bleiben beim alten Stand.
    /// </summary>
    public static class ActivateCommand
    {
        public static async Task<int> ActivateAsync(Arguments arguments, MlDataStore store, ServiceProvider provider)
        {
            var modelId = arguments.Int("model", 0);

            if (modelId == 0)
            {
                Console.Error.WriteLine("--model <Id> fehlt (Id aus DartBotModels).");
                return 1;
            }

            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var bots = unitOfWork.DartBotUnitOfWork;

            var model = await bots.DartBotModelRepository.GetFirstOrDefaultAsync(m => m.Id == modelId);

            if (model is null)
            {
                Console.Error.WriteLine($"Modell {modelId} nicht gefunden.");
                return 1;
            }

            if (model.ModelType != DartBotModelTypeEnum.TargetSelection ||
                model.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
            {
                Console.Error.WriteLine($"Modell {modelId} ist kein Zielwahl-Modell mit FeatureSchemaVersion {TargetCandidateFeatures.FeatureSchemaVersion}.");
                return 1;
            }

            var path = store.PathOf(model.StorageKey, mustBeNew: false);

            if (!File.Exists(path) ||
                !string.Equals(MlDataStore.Sha256Of(path), model.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine($"Modelldatei fehlt oder wurde verändert: {path}");
                return 1;
            }

            var active = await bots.DartBotActiveModelRepository.GetFirstOrDefaultAsync(a => a.ModelType == DartBotModelTypeEnum.TargetSelection);

            if (active is null)
            {
                active = new DartBotActiveModelEntity { ModelType = DartBotModelTypeEnum.TargetSelection };
                await bots.DartBotActiveModelRepository.AddAsync(active);
            }

            active.ModelId = model.Id;
            active.ActivatedAt = DateTime.UtcNow;
            active.ActivatedBy = "BotTrainer";
            await unitOfWork.SaveChangesAsync();

            Console.WriteLine($"Modell {model.Id} (Version {model.VersionNumber}) ist aktiv. Gilt für neu gestartete Bot-Spiele.");
            Console.WriteLine("Die API braucht die Datei unter DartBot:Ml:Path (Container: /data/ml).");
            return 0;
        }

        public static async Task<int> DeactivateAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var repository = unitOfWork.DartBotUnitOfWork.DartBotActiveModelRepository;

            var active = await repository.GetFirstOrDefaultAsync(a => a.ModelType == DartBotModelTypeEnum.TargetSelection);

            if (active is null)
            {
                Console.WriteLine("Kein Zielwahl-Modell aktiv.");
                return 0;
            }

            repository.Delete(active);
            await unitOfWork.SaveChangesAsync();
            Console.WriteLine("Zielwahl-Modell deaktiviert: neue Bot-Spiele sind regelbasiert.");
            return 0;
        }
    }
}
