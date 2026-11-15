using Microsoft.Extensions.Configuration;
using Tools.BotTrainer;

// BotTrainer – Werkzeug für die ML-Phasen der DartBots (läuft nie in der API).
//   generate  Datensatz aus simulierten Situationen erzeugen (CSV) + Metadaten in DartBotTrainingDatasets
//   train     Zielwahl-Modell aus einem Datensatz trainieren (ZIP) + DartBotModelTrainings / DartBotModels
//   compare   Modell gegen Regelwerk in simulierten Legs je Skill-Stufe (Ergebnis in MetricsJson)
//   activate  Zielwahl-Modell aktiv setzen (DartBotActiveModels); deactivate entfernt es
//
//   dotnet run --project Tools.BotTrainer -c Release -- generate --situations 1000 --rollouts 50 --seed 1 [--max-darts 60] [--no-db]
//   dotnet run --project Tools.BotTrainer -c Release -- train --dataset 4 [--seed 1] [--trees 200] [--leaves 64] [--min-leaf 20]
//   dotnet run --project Tools.BotTrainer -c Release -- compare --model 3 [--legs 2000] [--margins 0,0.3,0.5,1] [--tactical 1] [--seed 1] [--straight-out]
//   dotnet run --project Tools.BotTrainer -c Release -- activate --model 3
//   dotnet run --project Tools.BotTrainer -c Release -- deactivate
//
// Konfiguration: appsettings.json, Web.Core/appsettings.Development.json, Umgebungsvariablen (ConnectionStrings__DartContext, MlData__Path).

var arguments = Arguments.Parse(args);

if (arguments.Command is not ("generate" or "train" or "compare" or "activate" or "deactivate"))
{
    Console.WriteLine("Verwendung:");
    Console.WriteLine("  generate --situations <n> [--rollouts 50] [--seed 1] [--max-darts 60] [--no-db]");
    Console.WriteLine("  train --dataset <id> [--seed 1] [--trees 200] [--leaves 64] [--min-leaf 20]");
    Console.WriteLine("  compare --model <id> [--legs 2000] [--margins 0,0.3,0.5,1] [--tactical 1] [--seed 1] [--max-darts 60] [--straight-out]");
    Console.WriteLine("  activate --model <id>");
    Console.WriteLine("  deactivate");
    return 1;
}

// Reihenfolge (später gewinnt): eigene appsettings → Entwicklungs-Einstellungen der API (gleiche lokale DB) → Umgebung.
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "Web.Core", "appsettings.Development.json"), optional: true)
    .AddEnvironmentVariables()
    .Build();

var store = new MlDataStore(configuration["MlData:Path"] ?? "ml_data");

// Datenbank zuerst prüfen: ein falsches Passwort soll nicht erst nach einer langen Simulation auffallen.
var needsDatabase = arguments.Command != "generate" || !arguments.Flag("no-db");
await using var provider = needsDatabase ? await Database.ConnectAsync(configuration) : null;

if (needsDatabase && provider is null)
{
    Console.Error.WriteLine(arguments.Command == "generate" ? "Mit --no-db nur die Datei erzeugen." : "Dieser Befehl braucht die Datenbank.");
    return 2;
}

return arguments.Command switch
{
    "generate" => await GenerateCommand.RunAsync(arguments, store, provider),
    "train" => await TrainCommand.RunAsync(arguments, store, provider!),
    "activate" => await ActivateCommand.ActivateAsync(arguments, store, provider!),
    "deactivate" => await ActivateCommand.DeactivateAsync(provider!),
    _ => await CompareCommand.RunAsync(arguments, store, provider!)
};
