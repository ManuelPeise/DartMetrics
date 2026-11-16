using Tools.BotTrainer;

// BotTrainer – Entwickler-Werkzeug für die DartBots, ohne Datenbank.
//   benchmark  Regelwerk gegen Tabellen-Zielwahl je Streuung (Darts pro Leg, gepaarter Vergleich)
//   calibrate  Streuung (mm) → 3-Dart-Durchschnitt mit Wegebuch + Tabelle (Grundlage für Bots nach Durchschnitt)
//
//   dotnet run --project Tools.BotTrainer -c Release -- benchmark [--legs 2000] [--sigmas 6,9,13,20,30] [--straight-out]

var arguments = Arguments.Parse(args);

return arguments.Command switch
{
    "benchmark" => BenchmarkCommand.Run(arguments),
    "calibrate" => CalibrateCommand.Run(arguments),
    _ => Usage()
};

static int Usage()
{
    Console.WriteLine("Verwendung:");
    Console.WriteLine("  benchmark [--legs 2000] [--sigmas 6,9,13,20,30] [--tolerances 0,0.05,0.2] [--samples 4000] [--straight-out]");
    Console.WriteLine("  calibrate [--legs 2000] [--sigmas 3,4,5,...] [--samples 4000]");
    return 1;
}
