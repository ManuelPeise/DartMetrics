namespace Tools.Deployer.Deployment;

/// <summary>Eine Installation von DartMetrics: welche .env, welche Container, welche Compose-Dateien.</summary>
internal sealed record DeployTarget(string Name, string EnvFileName, bool IsProduction, string[] Containers)
{
    public static readonly DeployTarget Development = new(
        "Entwicklung (PC)", ".env", false, ["mysql", "dartmetrics-api", "dartmetrics-web", "mailpit"]);

    public static readonly DeployTarget Production = new(
        "Produktion", ".env", true, ["mysql", "dartmetrics-api", "dartmetrics-web"]);

    public static IReadOnlyList<DeployTarget> All { get; } = [Development, Production];

    /// <summary>Argumente für eigene "docker compose"-Aufrufe (Starten, Stoppen, Logs …), wie in den Skripten.</summary>
    public string[] ComposePrefix => ["compose"];

    /// <summary>Produktion: nur docker-compose.yml laden (keine offenen DB/API-Ports, kein Mailpit), wie deploy.ps1 -Production.</summary>
    public Dictionary<string, string?> ProcessEnvironment => IsProduction
        ? new Dictionary<string, string?> { ["COMPOSE_FILE"] = "docker-compose.yml" }
        : new Dictionary<string, string?>();

    public IEnumerable<(string Title, string Url)> GetLinks(IReadOnlyDictionary<string, string> values)
    {

        yield return ("Web-App", "https://localhost:7254/");
        if (!IsProduction)
        {
            yield return ("Swagger", "http://localhost:8080/swagger");
            yield return ("Mailpit", "http://localhost:8025");
        }
    }

    public override string ToString() => Name;
}
