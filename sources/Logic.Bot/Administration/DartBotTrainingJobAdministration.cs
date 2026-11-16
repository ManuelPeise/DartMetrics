using Data.DartContext.Entities.Bot;
using Logic.Shared;
using Logic.Bot.Ml;
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
using Shared.Models.Shared;

namespace Logic.Bot.Administration
{
    /// <summary>
    /// Trainingsaufträge aus dem Admin-Bereich. Ausgeführt werden sie vom BotTrainer-Worker (eigener Container);
    /// die API legt nur an, zeigt an und markiert Abbrüche.
    /// </summary>
    public class DartBotTrainingJobAdministration : ALogicBase, IDartBotTrainingJobAdministration
    {
        /// <summary>Ohne Lebenszeichen so lange gilt ein laufender Auftrag als verloren (Worker abgestürzt/neu gestartet).</summary>
        public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromMinutes(10);

        private readonly IClaimsHelper _claimsHelper;
        private readonly DartBotMlOptions _mlOptions;

        public DartBotTrainingJobAdministration(
            IUnitOfWork unitOfWork,
            IMessageLog messageLog,
            IClaimsHelper claimsHelper,
            IOptions<DartBotOptions> options) : base(unitOfWork, messageLog)
        {
            _claimsHelper = claimsHelper;
            _mlOptions = options.Value.Ml;
        }

        public async Task<List<DartBotTrainingJobListItem>> GetJobsAsync(int take = 20)
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                await FailLostJobsAsync();

                var jobs = await UnitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository.GetWhereAsync(
                    _ => true,
                    asNoTracking: true,
                    include: q => q
                        .Include(j => j.Model).ThenInclude(m => m!.Training)
                        .Include(j => j.PolicyModel).ThenInclude(m => m!.Training));

                return jobs
                    .OrderByDescending(j => j.Id)
                    .Take(Math.Clamp(take, 1, 100))
                    .Select(ToListItem)
                    .ToList();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error loading dart bot training jobs.", nameof(DartBotTrainingJobAdministration), exception);

                throw;
            }
        }

        public async Task<int> CreateJobAsync(CreateDartBotTrainingJobModel model)
        {
            try
            {
                var currentUser = EnsureCurrentUserIsAdmin();

                if (!Enum.IsDefined(model.Preset))
                {
                    throw new ArgumentException($"Unknown training preset '{model.Preset}'.");
                }

                await FailLostJobsAsync();

                var repository = UnitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository;

                if (await repository.AnyAsync(j => j.Status == DartBotTrainingJobStatusEnum.Queued
                                                   || j.Status == DartBotTrainingJobStatusEnum.Running))
                {
                    throw new BusinessRuleException("Es wartet oder läuft bereits ein Trainingsauftrag.");
                }

                var (situations, rollouts, legs) = DartBotTrainingPresets.For(model.Preset);

                if (model.BaseModelId is not null)
                {
                    await EnsureUsableBaseModelAsync(model.BaseModelId.Value);
                }

                var job = new DartBotTrainingJobEntity
                {
                    Preset = model.Preset,
                    Status = DartBotTrainingJobStatusEnum.Queued,
                    Situations = situations,
                    RolloutsPerCandidate = rollouts,
                    CompareLegs = legs,
                    // Neuer Seed je Auftrag, sonst entstünde immer derselbe Datensatz.
                    Seed = Random.Shared.Next(1, int.MaxValue),
                    PolicyModelId = model.BaseModelId,
                    RequestedBy = Truncate(currentUser.Email, 100),
                    ProgressText = "Wartet auf den Trainer …"
                };

                await repository.AddAsync(job);
                await UnitOfWork.SaveChangesAsync();

                return job.Id;
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage("Error creating dart bot training job.", nameof(DartBotTrainingJobAdministration), exception);

                throw;
            }
        }

        public async Task CancelJobAsync(int jobId)
        {
            try
            {
                EnsureCurrentUserIsAdmin();

                var job = await UnitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository.GetFirstOrDefaultAsync(j => j.Id == jobId)
                    ?? throw new ArgumentException($"Training job {jobId} not found.");

                switch (job.Status)
                {
                    case DartBotTrainingJobStatusEnum.Queued:
                        job.Status = DartBotTrainingJobStatusEnum.Cancelled;
                        job.FinishedAt = DateTime.UtcNow;
                        job.ProgressText = "Abgebrochen, bevor er gestartet wurde.";
                        break;

                    case DartBotTrainingJobStatusEnum.Running:
                        job.CancelRequested = true;
                        break;

                    default:
                        return;
                }

                await UnitOfWork.SaveChangesAsync();
            }
            catch (Exception exception)
            {
                await MessageLog.LogMessage($"Error cancelling dart bot training job {jobId}.", nameof(DartBotTrainingJobAdministration), exception);

                throw;
            }
        }

        /// <summary>Basis einer Runde: Zielwahl-Modell mit aktuellem Schema und gültiger Datei.</summary>
        private async Task EnsureUsableBaseModelAsync(int modelId)
        {
            var baseModel = await UnitOfWork.DartBotUnitOfWork.DartBotModelRepository.GetFirstOrDefaultAsync(m => m.Id == modelId, asNoTracking: true)
                ?? throw new ArgumentException($"Model {modelId} not found.");

            if (baseModel.ModelType != DartBotModelTypeEnum.TargetSelection)
            {
                throw new ArgumentException($"Model {modelId} is not a target selection model.");
            }

            if (baseModel.FeatureSchemaVersion != TargetCandidateFeatures.FeatureSchemaVersion)
            {
                throw new BusinessRuleException($"Modell Version {baseModel.VersionNumber} hat ein altes Schema und kann keine Basis sein.");
            }

            if (ModelFileCheck.Check(_mlOptions.Path, baseModel) != DartBotModelFileStatusEnum.Ok)
            {
                throw new BusinessRuleException($"Die Datei von Modell Version {baseModel.VersionNumber} fehlt oder wurde verändert.");
            }
        }

        /// <summary>Laufende Aufträge ohne Lebenszeichen blockieren sonst jeden neuen Auftrag.</summary>
        private async Task FailLostJobsAsync()
        {
            var limit = DateTime.UtcNow - HeartbeatTimeout;
            var repository = UnitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository;

            var lost = await repository.GetWhereAsync(j => j.Status == DartBotTrainingJobStatusEnum.Running
                                                           && (j.HeartbeatAt == null || j.HeartbeatAt < limit));

            if (lost.Count == 0)
            {
                return;
            }

            foreach (var job in lost)
            {
                job.Status = DartBotTrainingJobStatusEnum.Failed;
                job.FinishedAt = DateTime.UtcNow;
                job.ErrorMessage = "Keine Rückmeldung vom Trainer (Container gestoppt oder neu gestartet).";
            }

            await UnitOfWork.SaveChangesAsync();
        }

        private static DartBotTrainingJobListItem ToListItem(DartBotTrainingJobEntity job) => new()
        {
            Id = job.Id,
            Preset = job.Preset,
            Status = job.Status,
            Phase = job.Phase,
            ProgressPercent = job.ProgressPercent,
            ProgressText = job.ProgressText,
            Situations = job.Situations,
            CompareLegs = job.CompareLegs,
            RequestedBy = job.RequestedBy,
            CancelRequested = job.CancelRequested,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            FinishedAt = job.FinishedAt,
            ErrorMessage = job.ErrorMessage,
            DatasetId = job.DatasetId,
            ModelId = job.ModelId,
            ModelVersionNumber = job.Model?.VersionNumber,
            ComparisonPassed = job.ComparisonPassed,
            BaseModelId = job.PolicyModelId,
            BaseModelVersionNumber = job.PolicyModel?.VersionNumber,
            BenchmarkScore = BenchmarkReader.Score(job.Model?.Training?.MetricsJson),
            BaseBenchmarkScore = BenchmarkReader.Score(job.PolicyModel?.Training?.MetricsJson)
        };

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
                throw new UnauthorizedAccessException("Only administrators can manage dart bot training jobs.");
            }

            return currentUser;
        }
    }
}
