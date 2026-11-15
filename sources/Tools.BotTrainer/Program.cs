using Microsoft.Extensions.Configuration;
using Tools.BotTrainer;

// BotTrainer – Werkzeug für die ML-Phasen der DartBots (läuft nie in der API).
//   generate  Datensatz aus simulierten Situationen erzeugen (CSV) + Metadaten in DartBotTrainingDatasets
//   train     Zielwahl-Modell aus einem Datensatz trainieren (ZIP) + DartBotModelTrainings / DartBotModels
//
//   dotnet run --project Tools.BotTrainer -c Release -- generate --situations 1000 --rollouts 50 --seed 1 [--max-darts 60] [--no-db]
//   dotnet run --project Tools.BotTrainer -c Release -- train --dataset 4 [--seed 1] [--trees 200] [--leaves 64] [--min-leaf 20]
//
// Konfiguration: appsettings.json, Web.Core/appsettings.Development.json, Umgebungsvariablen (ConnectionStrings__DartContext, MlData__Path).

var arguments = Arguments.Parse(args);

if (arguments.Command is not ("generate" or "train"))
{
    Console.WriteLine("Verwendung:");
    Console.WriteLine("  generate --situations <n> [--rollouts 50] [--seed 1] [--max-darts 60] [--no-db]");
    Console.WriteLine("  train --dataset <id> [--seed 1] [--trees 200] [--leaves 64] [--min-leaf 20]");
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
var needsDatabase = arguments.Command == "train" || !arguments.Flag("no-db");
await using var provider = needsDatabase ? await Database.ConnectAsync(configuration) : null;

if (needsDatabase && provider is null)
{
    Console.Error.WriteLine(arguments.Command == "generate" ? "Mit --no-db nur die Datei erzeugen." : "Training braucht die Datenbank.");
    return 2;
}

return arguments.Command switch
{
    "generate" => await GenerateCommand.RunAsync(arguments, store, provider),
    _ => await TrainCommand.RunAsync(arguments, store, provider!)
};
