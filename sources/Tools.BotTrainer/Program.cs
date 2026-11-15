using System.Diagnostics;
using System.Text.Json;
using Data.DartContext;
using Data.DartContext.Entities.Bot;
using Logic.Bot.Ml;
using Logic.Shared.DI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Enums.Bot;
using Shared.Interfaces.UnitsOfWork;
using Shared.Models.Bot.Ml;
using Tools.BotTrainer;

// BotTrainer – Werkzeug für die ML-Phasen der DartBots (läuft nie in der API).
//   generate  Datensatz aus simulierten Situationen erzeugen (CSV) und Metadaten in DartBotTrainingDatasets speichern
//
//   dotnet run --project Tools.BotTrainer -- generate --situations 1000 --rollouts 50 --seed 1 [--max-darts 60] [--no-db]
//
// Konfiguration: appsettings.json, Umgebungsvariablen (ConnectionStrings__DartContext, MlData__Path).

var arguments = Arguments.Parse(args);

if (arguments.Command != "generate")
{
    Console.WriteLine("Verwendung: generate --situations <n> [--rollouts 50] [--seed 1] [--max-darts 60] [--no-db]");
    return 1;
}

// Reihenfolge (später gewinnt): eigene appsettings → Entwicklungs-Einstellungen der API (gleiche lokale DB) → Umgebung.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "Web.Core", "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables()
    .Build();

// Datenbank zuerst prüfen: ein falsches Passwort soll nicht erst nach einer langen Simulation auffallen.
var useDatabase = !arguments.Flag("no-db");
ServiceProvider? provider = null;

if (useDatabase)
{
    var connectionString = ConnectionStrings.Resolve(configuration);
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddHttpContextAccessor();
    services.AddDbContext<DartDatabaseContext>(o => o.UseMySQL(connectionString));
    services.AddSharedServices();
    provider = services.BuildServiceProvider();

    Console.WriteLine($"Datenbank: {ConnectionStrings.Describe(connectionString)} (Quelle: {ConnectionStrings.Source(configuration)})");

    await using var probe = provider.CreateAsyncScope();
    try
    {
        // OpenConnection statt CanConnect: CanConnect verschluckt den Grund.
        await probe.ServiceProvider.GetRequiredService<DartDatabaseContext>().Database.OpenConnectionAsync();
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Keine Verbindung zur Datenbank: {exception.GetBaseException().Message}");
        Console.Error.WriteLine("Mit --no-db nur die Datei erzeugen.");
        return 2;
    }
}

var situations = arguments.Int("situations", 1000);
var options = new RolloutOptions
{
    RolloutsPerCandidate = arguments.Int("rollouts", 50),
    MaxDartsPerLeg = arguments.Int("max-darts", 60),
    Seed = arguments.Int("seed", 1)
};

// Ablage: relativer StorageKey unter dem ML-Datenordner (Volume ml_data), Datei wird nie überschrieben.
var root = Path.GetFullPath(configuration["MlData:Path"] ?? "ml_data");
var storageKey = $"datasets/target-selection-v{TargetCandidateFeatures.FeatureSchemaVersion}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-seed{options.Seed}.csv";
var path = Path.Combine(root, storageKey);
Directory.CreateDirectory(Path.GetDirectoryName(path)!);

Console.WriteLine($"Erzeuge {situations} Situationen × Kandidaten × {options.RolloutsPerCandidate} Rollouts (Seed {options.Seed}) → {path}");

var stopwatch = Stopwatch.StartNew();
var sampler = new SituationSampler(options.Seed);
var evaluator = new CandidateEvaluator();

using (var writer = new CsvDatasetWriter(path))
{
    for (var i = 0; i < situations; i++)
    {
        var situationOptions = new RolloutOptions
        {
            RolloutsPerCandidate = options.RolloutsPerCandidate,
            MaxDartsPerLeg = options.MaxDartsPerLeg,
            Seed = unchecked(options.Seed * 397 + i)
        };

        writer.Write(evaluator.Evaluate(sampler.Next(), situationOptions));

        if ((i + 1) % 50 == 0 || i + 1 == situations)
        {
            Console.WriteLine($"  {i + 1}/{situations} Situationen, {writer.RowCount} Zeilen, {stopwatch.Elapsed:hh\\:mm\\:ss}");
        }
    }
}

var file = new FileInfo(path);
var sha256 = CsvDatasetWriter.Sha256Of(path);
var rowCount = File.ReadLines(path).LongCount() - 1;

Console.WriteLine($"Fertig: {rowCount} Zeilen, {file.Length / 1024.0 / 1024.0:0.0} MB, SHA-256 {sha256}");

if (!useDatabase)
{
    Console.WriteLine("--no-db: keine Metadaten gespeichert.");
    return 0;
}

await using var scope = provider!.CreateAsyncScope();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

var dataset = new DartBotTrainingDatasetEntity
{
    StorageKey = storageKey,
    Format = DartBotFileFormatEnum.Csv,
    SourceType = DartBotDatasetSourceTypeEnum.Simulation,
    RowCount = rowCount,
    SizeBytes = file.Length,
    Sha256 = sha256,
    FeatureSchemaVersion = TargetCandidateFeatures.FeatureSchemaVersion,
    RandomSeed = options.Seed,
    GeneratorParametersJson = JsonSerializer.Serialize(new
    {
        situations,
        options.RolloutsPerCandidate,
        options.MaxDartsPerLeg,
        strategy = "RuleBasedTargetSelector",
        label = nameof(TargetCandidateFeatures.ExpectedDartsToFinish)
    })
};

await unitOfWork.DartBotUnitOfWork.DartBotTrainingDatasetRepository.AddAsync(dataset);
await unitOfWork.SaveChangesAsync();

Console.WriteLine($"Metadaten gespeichert: DartBotTrainingDatasets.Id = {dataset.Id}");
return 0;
