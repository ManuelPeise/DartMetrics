namespace Tools.Deployer.Deployment;

/// <summary>
/// Wie DartMetrics im gewählten Ordner läuft: Entwicklung (mit docker-compose.override.yml) oder Produktion.
/// Welche Instanz der Ordner ist (z. B. Beta-Worktree), steht in COMPOSE_PROJECT_NAME der .env.
/// </summary>
internal sealed record DeployTarget(string Name, bool IsProduction, string[] Services)
{
    public const string EnvFileName = ".env";

    public static readonly DeployTarget Development = new("Entwicklung (PC)", false, ["mysql", "api", "web", "mailpit"]);

    public static readonly DeployTarget Production = new("Produktion", true, ["mysql", "api", "web"]);

    public static IReadOnlyList<DeployTarget> All { get; } = [Development, Production];

    /// <summary>Argumente für eigene "docker compose"-Aufrufe (Stoppen, Logs …), wie in den Skripten.</summary>
    public string[] ComposePrefix => ["compose"];

    /// <summary>Produktion: nur docker-compose.yml laden (keine offenen DB/API-Ports, kein Mailpit), wie deploy.ps1 -Production.</summary>
    public Dictionary<string, string?> ProcessEnvironment => IsProduction
        ? new Dictionary<string, string?> { ["COMPOSE_FILE"] = "docker-compose.yml" }
        : new Dictionary<string, string?>();

    /// <summary>Name der Instanz wie in den Skripten: COMPOSE_PROJECT_NAME, sonst der Ordnername.</summary>
    public static string InstanceName(IReadOnlyDictionary<string, string> values, string repositoryPath) =>
        values.TryGetValue("COMPOSE_PROJECT_NAME", out var name) && name.Length > 0
            ? name
            : Path.GetFileName(repositoryPath.TrimEnd(Path.DirectorySeparatorChar)).ToLowerInvariant();

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
