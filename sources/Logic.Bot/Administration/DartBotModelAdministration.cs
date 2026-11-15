using System.Text.Json;
using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Logic.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Enums.Bot;
using Shared.Enums.User;
using Shared.Interfaces.Administration;
using Shared.Interfaces.Authentication;
using Shared.Interfaces.Logger;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot;
using Shared.Models.Bot.Ml;
using Shared.Models.Options;

namespace Logic.Bot.Administration
{
    /// <summary>
    /// Phase 6: Zielwahl-Modelle ansehen und aktivieren. Training und Vergleich laufen weiter in Tools.BotTrainer;
    /// die Metriken kommen aus MetricsJson des Trainingslaufs.
    /// </summary>
    public class DartBotModelAdministration : ALogicBase, IDartBotModelAdministration
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IClaimsHelper _claimsHelper;
        private readonly DartBotMlOptions _options;

        public DartBotModelAdministration(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper,
            IOptions<DartBotOptions> options) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
            _options = options.Value.Ml;
        }

        public async Task<DartBotModelOverview> GetModelsAsync()
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                var bots = UnitOfWork.DartBotUnitOfWork;

                var models = await bots.DartBotModelRepository.GetWhereAsync(
                    m => m.ModelType == DartBotModelTypeEnum.TargetSelection,
                    asNoTracking: true,
                    include: q => q.Include(m => m.Training).ThenInclude(t => t.Dataset));

                var active = await bots.DartBotActiveModelRepository.GetFirstOrDefaultAsync(
                    a => a.ModelType == DartBotModelTypeEnum.TargetSelection, asNoTracking: true);

                return new DartBotModelOverview
                {
                    MlEnabled = _options.Enabled,
                    Margin = _options.Margin,
                    ExpectedFeatureSchemaVersion = TargetCandidateFeatures.FeatureSchemaVersion,
                    Models = models
                        .OrderByDescending(m => m.VersionNumber)
                        .Select(m => ToListItem(m, active))
                        .ToList()
                };
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading dart bot models.", nameof(DartBotModelAdministration), exception);

                throw;
            }
        }

        public async Task ActivateModelAsync(int modelId)
        {
            try
            {
                var currentUser = EnsureCurrentUserIsAdmin();
                var bots = UnitOfWork.DartBotUnitOfWork;

                var model = await bots.DartBotModelRepository.GetFirstOrDefaultAsync(m => m.Id == modelId, asNoTracking: true)
                    ?? throw new ArgumentException($"Model {modelId} not found.");

                if (model.ModelType != DartBotModelTypeEnum.TargetSelection)
                {
                    throw new ArgumentException($"Model {modelId} is not a target selection model.");
                }

                var fileStatus = ModelFileCheck.Check(_options.Path, model);

                if (fileStatus != DartBotModelFileStatusEnum.Ok)
                {
                    throw new ArgumentException(fileStatus switch
                    {
                        DartBotModelFileStatusEnum.SchemaMismatch => $"Model {modelId} has feature schema {model.FeatureSchemaVersion}, expected {TargetCandidateFeatures.FeatureSchemaVersion}.",
                        DartBotModelFileStatusEnum.ShaMismatch => $"The file of model {modelId} was changed (SHA-256 mismatch).",
                        _ => $"The file of model {modelId} was not found on the server."
                    });
                }

                // Getrackt laden und direkt ändern (Unique Index auf ModelType: höchstens ein Eintrag).
                var active = await bots.DartBotActiveModelRepository.GetFirstOrDefaultAsync(
                    a => a.ModelType == DartBotModelTypeEnum.TargetSelection);

                if (active is null)
                {
                    active = new DartBotActiveModelEntity { ModelType = DartBotModelTypeEnum.TargetSelection };
                    await bots.DartBotActiveModelRepository.AddAsync(active);
                }
                else if (active.ModelId == modelId)
                {
                    return;
                }

                active.ModelId = modelId;
                active.ActivatedAt = DateTime.UtcNow;
                active.ActivatedBy = Truncate(currentUser.Email, 100);

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error activating dart bot model {modelId}.", nameof(DartBotModelAdministration), exception);

                throw;
            }
        }

        public async Task DeactivateTargetModelAsync()
        {
            try
            {
                EnsureCurrentUserIsAdmin();
                var repository = UnitOfWork.DartBotUnitOfWork.DartBotActiveModelRepository;

                var active = await repository.GetFirstOrDefaultAsync(a => a.ModelType == DartBotModelTypeEnum.TargetSelection);

                if (active is null)
                {
                    return;
                }

                repository.Delete(active);
                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error deactivating dart bot target model.", nameof(DartBotModelAdministration), exception);

                throw;
            }
        }

        private DartBotModelListItem ToListItem(DartBotModelEntity model, DartBotActiveModelEntity? active)
        {
            var isActive = active?.ModelId == model.Id;

            var item = new DartBotModelListItem
            {
                Id = model.Id,
                VersionNumber = model.VersionNumber,
                CreatedAt = model.CreatedAt,
                TrainingId = model.TrainingId,
                DatasetId = model.Training.DatasetId,
                DatasetRowCount = model.Training.Dataset.RowCount,
                TrainingDataCount = model.Training.TrainingDataCount,
                FeatureSchemaVersion = model.FeatureSchemaVersion,
                MlNetVersion = model.MlNetVersion,
                SizeBytes = model.SizeBytes,
                IsActive = isActive,
                ActivatedAt = isActive ? active!.ActivatedAt : null,
                ActivatedBy = isActive ? active!.ActivatedBy : null,
                FileStatus = ModelFileCheck.Check(_options.Path, model)
            };

            ReadMetrics(model.Training.MetricsJson, item);
            return item;
        }

        /// <summary>MetricsJson schreibt Tools.BotTrainer; fehlende oder alte Felder sind kein Fehler.</summary>
        private static void ReadMetrics(string? json, DartBotModelListItem item)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                var metrics = JsonSerializer.Deserialize<TrainingMetrics>(json, JsonOptions);

                if (metrics is null)
                {
                    return;
                }

                item.TopChoiceHitRate = metrics.TopChoiceHitRate;
                item.TopChoiceMeanRegret = metrics.TopChoiceMeanRegret;
                item.ComparedAt = metrics.Comparison?.ComparedAt;
                item.ComparedLegsPerLevel = metrics.Comparison?.LegsPerLevel;
                item.Comparison = metrics.Comparison?.Levels ?? [];
            }
            catch (JsonException)
            {
                // Unlesbare Metriken: Modell trotzdem anzeigen.
            }
        }

        private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

        private global::Shared.Models.Authentication.CurrentUser EnsureCurrentUserIsAdmin()
        {
            var currentUser = _claimsHelper.GetCurrentUserFromClaims();

            if (currentUser == null || currentUser.UserId == 0)
            {
                throw new Exception("Current user not found.");
            }

            if (currentUser.Roles == null || !currentUser.Roles.Contains(UserRoleEnum.Admin))
            {
                throw new UnauthorizedAccessException("Only administrators can manage dart bot models.");
            }

            return currentUser;
        }

        private sealed class TrainingMetrics
        {
            public double? TopChoiceHitRate { get; set; }
            public double? TopChoiceMeanRegret { get; set; }
            public ComparisonMetrics? Comparison { get; set; }
        }

        private sealed class ComparisonMetrics
        {
            public DateTime? ComparedAt { get; set; }
            public int? LegsPerLevel { get; set; }
            public List<DartBotModelComparisonLevel>? Levels { get; set; }
        }
    }
}
