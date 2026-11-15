using Microsoft.Extensions.Configuration;

namespace Tools.BotTrainer
{
    /// <summary>
    /// Verbindung zur lokalen Docker-MySQL:
    /// 1. ConnectionStrings:DartContext aus Umgebung, Web.Core/appsettings.Development.json oder eigener appsettings (sofern kein Platzhalter);
    /// 2. sonst aus der .env im Lösungsordner (MYSQL_PASSWORD) wie in docker-compose.yml, Host localhost:3306.
    /// Das Passwort steht damit nie im Repo.
    /// </summary>
    public static class ConnectionStrings
    {
        private const string Placeholder = "SET_VIA_ENVIRONMENT";

        public static string Resolve(IConfiguration configuration)
        {
            var configured = configuration.GetConnectionString("DartContext");

            if (!string.IsNullOrWhiteSpace(configured) && !configured.Contains(Placeholder, StringComparison.Ordinal))
            {
                return configured;
            }

            var password = ReadEnvFile("MYSQL_PASSWORD")
                ?? throw new InvalidOperationException(
                    "Kein Datenbank-Passwort: ConnectionStrings__DartContext setzen oder MYSQL_PASSWORD in der .env im Lösungsordner pflegen.");

            return $"server=localhost;port=3306;database=DartContextDb;user=dartforge;password={password}";
        }

        /// <summary>Verbindung ohne Passwort für die Konsolenausgabe.</summary>
        public static string Describe(string connectionString)
        {
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !p.TrimStart().StartsWith("password", StringComparison.OrdinalIgnoreCase)
                            && !p.TrimStart().StartsWith("pwd", StringComparison.OrdinalIgnoreCase));
            return string.Join(';', parts);
        }

        /// <summary>Woher die Verbindung kommt (Umgebungsvariable überschreibt die Dateien).</summary>
        public static string Source(IConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionStrings__DartContext")))
            {
                return "Umgebungsvariable ConnectionStrings__DartContext";
            }

            var configured = configuration.GetConnectionString("DartContext");
            return string.IsNullOrWhiteSpace(configured) || configured.Contains(Placeholder, StringComparison.Ordinal)
                ? ".env (MYSQL_PASSWORD)"
                : "appsettings (Web.Core/appsettings.Development.json bzw. eigene)";
        }

        /// <summary>Sucht die .env im aktuellen Ordner und darüber (dotnet run läuft im Lösungsordner).</summary>
        private static string? ReadEnvFile(string key)
        {
            for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
            {
                var file = Path.Combine(dir.FullName, ".env");

                if (!File.Exists(file))
                {
                    continue;
                }

                foreach (var line in File.ReadLines(file))
                {
                    var trimmed = line.Trim();

                    if (trimmed.StartsWith(key + "=", StringComparison.Ordinal))
                    {
                        return trimmed[(key.Length + 1)..].Trim().Trim('"');
                    }
                }

                return null;
            }

            return null;
        }
    }
}
