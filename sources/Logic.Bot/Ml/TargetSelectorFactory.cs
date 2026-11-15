using System.Collections.Concurrent;
using Data.DartContext.Entities.Bot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Enums.Bot;
using Shared.Interfaces.Bot;
using Shared.Models.Bot.Ml;
using Shared.Models.Options;

namespace Logic.Bot.Ml
{
    /// <summary>
    /// Singleton: lädt jedes Modell einmal (nach Id) und baut daraus die Hybrid-Zielwahl.
    /// Modelldateien werden nie überschrieben, daher ist der Cache je Id dauerhaft gültig.
    /// </summary>
    public sealed class TargetSelectorFactory : ITargetSelectorFactory
    {
        private readonly ITargetSelector _rules;
        private readonly DartBotMlOptions _options;
        private readonly ILogger<TargetSelectorFactory> _logger;
        private readonly ConcurrentDictionary<int, Lazy<LoadedTargetModel?>> _models = new();

        public TargetSelectorFactory(ITargetSelector rules, IOptions<DartBotOptions> options, ILogger<TargetSelectorFactory> logger)
        {
            _rules = rules;
            _options = options.Value.Ml;
            _logger = logger;
        }

        public ITargetSelector For(DartBotModelEntity? targetModel)
        {
            if (targetModel is null || !_options.Enabled || targetModel.ModelType != DartBotModelTypeEnum.TargetSelection)
            {
                return _rules;
            }

            var loaded = _models.GetOrAdd(targetModel.Id, _ => new Lazy<LoadedTargetModel?>(() => TryLoad(targetModel))).Value;

            return loaded is null ? _rules : new HybridTargetSelector(loaded, _rules, _options.Margin);
        }

        private LoadedTargetModel? TryLoad(DartBotModelEntity model)
        {
            try
            {
                if (model.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
                {
                    _logger.LogWarning("Modell {ModelId} hat FeatureSchemaVersion {Version}, erwartet {Expected} – regelbasiert.",
                        model.Id, model.FeatureSchemaVersion, TargetCandidateFeatures.FeatureSchemaVersion);
                    return null;
                }

                var path = ModelFileCheck.ResolvePath(_options.Path, model.StorageKey);

                if (path is null || !File.Exists(path))
                {
                    _logger.LogWarning("Modelldatei für Modell {ModelId} nicht gefunden ({StorageKey}) – regelbasiert.", model.Id, model.StorageKey);
                    return null;
                }

                var loaded = LoadedTargetModel.Load(path, model.Sha256);
                _logger.LogInformation("Zielwahl-Modell {ModelId} (Version {Version}) geladen.", model.Id, model.VersionNumber);
                return loaded;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Modell {ModelId} konnte nicht geladen werden – regelbasiert.", model.Id);
                return null;
            }
        }
    }
}
