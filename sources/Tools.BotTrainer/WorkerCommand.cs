using System.Runtime.InteropServices;
using Data.DartContext.Entities.Bot;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums.Bot;
using Shared.Interfaces.UnitsOfWork;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Dauerbetrieb im Container: holt Trainingsaufträge aus DartBotTrainingJobs (aus dem Admin-Bereich angelegt) und führt
    /// generate → train → compare aus. Immer nur ein Auftrag gleichzeitig; aktiviert wird nie automatisch.
    /// </summary>
    public static class WorkerCommand
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);

        // Anteil der Schritte am Gesamtfortschritt (Datensatz dauert am längsten).
        private const int GenerateShare = 70;
        private const int TrainShare = 10;

        public static async Task<int> RunAsync(Arguments arguments, MlDataStore store, ServiceProvider provider)
        {
            using var stop = new CancellationTokenSource();
            using var sigterm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                stop.Cancel();
            });
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                stop.Cancel();
            };

            await FailInterruptedJobsAsync(provider);
            Console.WriteLine($"Worker bereit, prüfe alle {PollInterval.TotalSeconds:0} s auf neue Trainingsaufträge.");

            while (!stop.IsCancellationRequested)
            {
                var jobId = await ClaimNextJobAsync(provider);

                if (jobId is null)
                {
                    try
                    {
                        await Task.Delay(PollInterval, stop.Token);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    continue;
                }

                await RunJobAsync(jobId.Value, store, provider, stop.Token);
            }

            Console.WriteLine("Worker beendet.");
            return 0;
        }

        /// <summary>Markiert einen Auftrag, dessen Vergleich nach einem Neustart schon einmal wiederholt wurde.</summary>
        private const string ResumedMarker = "Vergleich nach Neustart des Trainers wiederholt.";

        /// <summary>
        /// Nach Neustart: Hat ein unterbrochener Auftrag schon ein Modell, wird nur der Vergleich wiederholt (einmal);
        /// sonst ist er verloren.
        /// </summary>
        private static async Task FailInterruptedJobsAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var running = await unitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository.GetWhereAsync(
                j => j.Status == DartBotTrainingJobStatusEnum.Running);

            foreach (var job in running)
            {
                if (job.ModelId is not null && job.ErrorMessage != ResumedMarker && !job.CancelRequested)
                {
                    job.Status = DartBotTrainingJobStatusEnum.Queued;
                    job.Phase = DartBotTrainingJobPhaseEnum.Compare;
                    job.ErrorMessage = ResumedMarker;
                    job.ProgressText = "Trainer neu gestartet – Vergleich wird wiederholt.";
                    Console.WriteLine($"Auftrag {job.Id}: Modell {job.ModelId} vorhanden, Vergleich wird wiederholt.");
                    continue;
                }

                job.Status = DartBotTrainingJobStatusEnum.Failed;
                job.FinishedAt = DateTime.UtcNow;
                job.ErrorMessage = "Der Trainer wurde während des Auftrags neu gestartet.";
                Console.WriteLine($"Auftrag {job.Id} als fehlgeschlagen markiert (unterbrochen).");
            }

            if (running.Count > 0)
            {
                await unitOfWork.SaveChangesAsync();
            }
        }

        private static async Task<int?> ClaimNextJobAsync(ServiceProvider provider)
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var queued = await unitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository.GetWhereAsync(
                j => j.Status == DartBotTrainingJobStatusEnum.Queued);

            var job = queued.OrderBy(j => j.Id).FirstOrDefault();

            if (job is null)
            {
                return null;
            }

            var resumed = job.ModelId is not null;

            job.Status = DartBotTrainingJobStatusEnum.Running;
            job.Phase = resumed ? DartBotTrainingJobPhaseEnum.Compare : DartBotTrainingJobPhaseEnum.Generate;
            job.StartedAt ??= DateTime.UtcNow;
            job.HeartbeatAt = DateTime.UtcNow;
            job.ProgressPercent = resumed ? GenerateShare + TrainShare : 0;
            job.ProgressText = resumed ? "Vergleich wird wiederholt …" : "Gestartet";
            await unitOfWork.SaveChangesAsync();

            return job.Id;
        }

        private static async Task RunJobAsync(int jobId, MlDataStore store, ServiceProvider provider, CancellationToken stop)
        {
            var job = await LoadAsync(provider, jobId);
            Console.WriteLine($"Auftrag {jobId}: {job.Preset}, {job.Situations} Situationen, {job.CompareLegs} Vergleichs-Legs, Seed {job.Seed}");

            using var heartbeatStop = CancellationTokenSource.CreateLinkedTokenSource(stop);
            var heartbeat = HeartbeatAsync(provider, jobId, heartbeatStop.Token);

            try
            {
                int modelId;
                int exitCode;

                if (job.ModelId is not null)
                {
                    // Fortsetzung nach Neustart: Datensatz und Modell gibt es schon.
                    modelId = job.ModelId.Value;
                    Console.WriteLine($"Auftrag {jobId}: setze mit dem Vergleich von Modell {modelId} fort.");
                }
                else
                {
                    // 1. Datensatz
                    var generate = Hooks(provider, jobId, DartBotTrainingJobPhaseEnum.Generate, 0, GenerateShare, stop);
                    exitCode = await GenerateCommand.RunAsync(Arguments.Parse(
                    [
                        "generate", "--situations", job.Situations.ToString(), "--rollouts", job.RolloutsPerCandidate.ToString(), "--seed", job.Seed.ToString()
                    ]), store, provider, generate);

                    var datasetId = generate.CreatedId ?? throw new InvalidOperationException($"Datensatz konnte nicht erzeugt werden (Exit-Code {exitCode}).");
                    await UpdateAsync(provider, jobId, j => j.DatasetId = datasetId);

                    // 2. Training
                    var train = Hooks(provider, jobId, DartBotTrainingJobPhaseEnum.Train, GenerateShare, TrainShare, stop);
                    await train.ReportOrCancelAsync("Modell wird trainiert …", 0);
                    exitCode = await TrainCommand.RunAsync(Arguments.Parse(
                    [
                        // Abstimmen kostet etwa die fünffache Trainingszeit; beim Funktionstest (Schnell) nicht.
                        "train", "--dataset", datasetId.ToString(), "--seed", job.Seed.ToString(),
                        .. (job.Preset == DartBotTrainingJobPresetEnum.Quick ? Array.Empty<string>() : ["--tune"])
                    ]), store, provider, train);

                    modelId = train.CreatedId ?? throw new InvalidOperationException($"Training fehlgeschlagen (Exit-Code {exitCode}), Details im Trainingslauf.");
                    await UpdateAsync(provider, jobId, j => j.ModelId = modelId);
                }

                // 3. Vergleich gegen das Regelwerk
                var compare = Hooks(provider, jobId, DartBotTrainingJobPhaseEnum.Compare, GenerateShare + TrainShare, 100 - GenerateShare - TrainShare, stop);
                exitCode = await CompareCommand.RunAsync(Arguments.Parse(
                [
                    // Prüfstand: fester Seed und Bot-Level, damit Modelle verschiedener Aufträge vergleichbar sind.
                    "compare", "--model", modelId.ToString(), "--legs", job.CompareLegs.ToString(), "--benchmark"
                ]), store, provider, compare);

                if (compare.ComparisonPassed is null)
                {
                    throw new InvalidOperationException($"Vergleich fehlgeschlagen (Exit-Code {exitCode}).");
                }

                await UpdateAsync(provider, jobId, j =>
                {
                    j.ErrorMessage = null;
                    j.Status = DartBotTrainingJobStatusEnum.Completed;
                    j.FinishedAt = DateTime.UtcNow;
                    j.ProgressPercent = 100;
                    j.ComparisonPassed = compare.ComparisonPassed;
                    j.ProgressText = compare.ComparisonPassed == true
                        ? "Fertig – Modell ist nicht schlechter als das Regelwerk und kann aktiviert werden."
                        : "Fertig – Modell ist schlechter als das Regelwerk, Aktivieren nicht empfohlen.";
                });

                Console.WriteLine($"Auftrag {jobId} fertig: Modell {modelId}, Vergleich {(compare.ComparisonPassed == true ? "bestanden" : "nicht bestanden")}.");
            }
            catch (OperationCanceledException) when (!stop.IsCancellationRequested)
            {
                await UpdateAsync(provider, jobId, j =>
                {
                    j.Status = DartBotTrainingJobStatusEnum.Cancelled;
                    j.FinishedAt = DateTime.UtcNow;
                    j.ProgressText = "Abgebrochen.";
                });

                Console.WriteLine($"Auftrag {jobId} abgebrochen.");
            }
            catch (Exception exception)
            {
                var message = stop.IsCancellationRequested
                    ? "Der Trainer wurde während des Auftrags gestoppt."
                    : exception.GetBaseException().Message;

                await UpdateAsync(provider, jobId, j =>
                {
                    j.Status = DartBotTrainingJobStatusEnum.Failed;
                    j.FinishedAt = DateTime.UtcNow;
                    j.ErrorMessage = message.Length <= 2000 ? message : message[..2000];
                });

                Console.Error.WriteLine($"Auftrag {jobId} fehlgeschlagen: {message}");
            }
            finally
            {
                heartbeatStop.Cancel();
                await heartbeat;
            }
        }

        /// <summary>Fortschritt in den Auftrag schreiben; Abbruch durch Admin oder durch Stoppen des Containers.</summary>
        private static JobHooks Hooks(ServiceProvider provider, int jobId, DartBotTrainingJobPhaseEnum phase, int offset, int share, CancellationToken stop) => new()
        {
            ReportAsync = async (text, percent) =>
            {
                var cancel = false;

                await UpdateAsync(provider, jobId, j =>
                {
                    j.Phase = phase;
                    j.ProgressText = text;
                    j.ProgressPercent = Math.Clamp(offset + share * percent / 100, 0, 100);
                    j.HeartbeatAt = DateTime.UtcNow;
                    cancel = j.CancelRequested;
                });

                return cancel || stop.IsCancellationRequested;
            }
        };

        /// <summary>Lebenszeichen auch während langer Schritte ohne Zwischenstand (Training).</summary>
        private static async Task HeartbeatAsync(ServiceProvider provider, int jobId, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(HeartbeatInterval, token);
                    await UpdateAsync(provider, jobId, j => j.HeartbeatAt = DateTime.UtcNow);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Lebenszeichen für Auftrag {jobId} fehlgeschlagen: {exception.GetBaseException().Message}");
            }
        }

        private static async Task<DartBotTrainingJobEntity> LoadAsync(ServiceProvider provider, int jobId)
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            return await unitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository.GetFirstOrDefaultAsync(j => j.Id == jobId, asNoTracking: true)
                ?? throw new InvalidOperationException($"Auftrag {jobId} nicht gefunden.");
        }

        /// <summary>Eigener Scope je Änderung: kurz, unabhängig vom DbContext des laufenden Befehls.</summary>
        private static async Task UpdateAsync(ServiceProvider provider, int jobId, Action<DartBotTrainingJobEntity> change)
        {
            await using var scope = provider.CreateAsyncScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var job = await unitOfWork.DartBotUnitOfWork.DartBotTrainingJobRepository.GetFirstOrDefaultAsync(j => j.Id == jobId)
                ?? throw new InvalidOperationException($"Auftrag {jobId} nicht gefunden.");

            change(job);
            await unitOfWork.SaveChangesAsync();
        }
    }
}
