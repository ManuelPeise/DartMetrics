namespace Web.App.Components.Admin.Preview
{
    /// <summary>
    /// Beispieldaten für die Design-Phase der Admin-Oberfläche.
    /// Bewusst ohne API und ohne Abhängigkeit zu Backend-Modellen: die Seiten werden erst nach dem
    /// Merge mit dem Bot-Branch an die echten Endpunkte angebunden. Danach kann diese Datei entfallen.
    /// </summary>
    public static class AdminSampleData
    {
        public sealed class SampleBot
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Alias { get; set; } = "";
            public string? PlayingStyle { get; set; }
            public int ProfileVersion { get; set; } = 1;
            public int Level { get; set; }
            public int Rating { get; set; } = 1000;
            public long Xp { get; set; }
            public bool IsActive { get; set; } = true;
            public float Precision { get; set; }
            public float Consistency { get; set; }
            public float Tactics { get; set; }
            public float Pressure { get; set; }
            public float Risk { get; set; } = 0.5f;
        }

        public enum SampleLogType
        {
            Error,
            Warning,
            Information
        }

        public sealed record SampleLog(int Id, SampleLogType Type, string Module, string Message, string? Exception, string? Stacktrace, DateTime CreatedAtUtc);

        public static List<SampleBot> Bots() =>
        [
            new() { Id = 1, Name = "Robo Rookie", Alias = "rookie", Level = 0, Rating = 1000, Xp = 0, Precision = 0.15f, Consistency = 0.2f, Tactics = 0.2f, Pressure = 0.2f },
            new() { Id = 2, Name = "Steel Sam", Alias = "steel-sam", PlayingStyle = "Ausgewogen", ProfileVersion = 4, Level = 6, Rating = 1084, Xp = 3140, Precision = 0.39f, Consistency = 0.42f, Tactics = 0.42f, Pressure = 0.42f },
            new() { Id = 3, Name = "Treble Tina", Alias = "treble-tina", PlayingStyle = "Aggressiv", ProfileVersion = 9, Level = 12, Rating = 1213, Xp = 8420, Precision = 0.63f, Consistency = 0.65f, Tactics = 0.65f, Pressure = 0.65f, Risk = 0.8f },
            new() { Id = 4, Name = "Double Dieter", Alias = "double-dieter", PlayingStyle = "Defensiv", ProfileVersion = 14, Level = 19, Rating = 1355, Xp = 16910, Precision = 0.91f, Consistency = 0.92f, Tactics = 0.92f, Pressure = 0.92f, Risk = 0.25f },
            new() { Id = 5, Name = "Old Oche", Alias = "old-oche", ProfileVersion = 3, Level = 4, Rating = 990, Xp = 2010, IsActive = false, Precision = 0.31f, Consistency = 0.34f, Tactics = 0.34f, Pressure = 0.34f }
        ];

        public static List<SampleLog> Logs()
        {
            var now = DateTime.UtcNow;

            return
            [
                new(1, SampleLogType.Information, "UserRegistration", "Neuer Benutzer registriert: max.mustermann@example.com", null, null, now.AddMinutes(-12)),
                new(2, SampleLogType.Warning, "UserAuthentication", "Fehlgeschlagener Login-Versuch für anna@example.com", null, null, now.AddMinutes(-47)),
                new(3, SampleLogType.Error, "X01TrainingGame", "Error adding throw to training 42.", "Invalid throw number.",
                    "at Logic.Training.X01TrainingGame.AddThrowAsync(X01TrainingThrow model)\n   at Service.Api.Controllers.Training.X01TrainingGameController.AddThrow(...)", now.AddHours(-2)),
                new(4, SampleLogType.Information, "DartBotAdministration", "Bot \"Treble Tina\" angelegt.", null, null, now.AddHours(-5)),
                new(5, SampleLogType.Information, "DartBotLearning", "Steel Sam: Level 5 -> 6, neue Profilversion 4.", null, null, now.AddHours(-9)),
                new(6, SampleLogType.Warning, "EmailClient", "E-Mail-Versand verzögert, erneuter Versuch in 60 s.", "SMTP timeout", null, now.AddHours(-26)),
                new(7, SampleLogType.Error, "CheckoutTraining", "Error finishing checkout training.", "Training is already finished.",
                    "at Logic.Training.CheckoutTraining.FinishTrainingAsync(Int32 trainingId)", now.AddDays(-2)),
                new(8, SampleLogType.Information, "System", "Anwendung gestartet.", null, null, now.AddDays(-3))
            ];
        }
    }
}
