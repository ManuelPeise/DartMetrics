namespace Tools.Deployer.Deployment;

/// <summary>Findet den Lösungsordner "sources" (docker-compose.yml, .env.example, deploy\deploy.ps1).</summary>
internal static class RepositoryLocator
{
    public static bool IsValid(string? path)
        => !string.IsNullOrWhiteSpace(path)
           && File.Exists(Path.Combine(path, "docker-compose.yml"))
           && File.Exists(Path.Combine(path, ".env.example"))
           && File.Exists(Path.Combine(path, "deploy", "deploy.ps1"));

    /// <summary>Akzeptiert auch den Repository-Stamm (dann wird "sources" ergänzt).</summary>
    public static string? Normalize(string? path)
    {
        if (IsValid(path))
        {
            return Path.GetFullPath(path!);
        }

        if (!string.IsNullOrWhiteSpace(path))
        {
            var sources = Path.Combine(path, "sources");
            if (IsValid(sources))
            {
                return Path.GetFullPath(sources);
            }
        }

        return null;
    }

    /// <summary>Gemerkter Ordner, sonst aufwärts vom Programmordner (z. B. Tools.Deployer\bin\Debug\…).</summary>
    public static string? Find(string? remembered)
    {
        var path = Normalize(remembered);
        if (path is not null)
        {
            return path;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            path = Normalize(directory.FullName);
            if (path is not null)
            {
                return path;
            }
        }

        return null;
    }
}
