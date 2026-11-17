using Tools.Deployer.Deployment;

namespace Tools.Deployer.Configuration;

internal enum IssueSeverity
{
    /// <summary>Nur Hinweis.</summary>
    Warning,

    /// <summary>Speichern möglich, Deployment nicht (z. B. Platzhalter).</summary>
    Error,

    /// <summary>Ungültiges Format: wird nicht gespeichert.</summary>
    Invalid,
}

internal sealed record EnvIssue(IssueSeverity Severity, string? Key, string Message);

/// <summary>Dieselben Prüfungen wie deploy.ps1, nur schon beim Eingeben.</summary>
internal static class EnvValidator
{
    public static readonly string[] RequiredKeys =
        ["MYSQL_ROOT_PASSWORD", "MYSQL_PASSWORD", "JWT_KEY", "ADMIN_PASSWORD", "CERT_PASSWORD"];

    private static readonly string[] BooleanKeys = ["FORWARDED_HEADERS_ENABLED", "SMTP_ENABLE_SSL"];

    public static List<EnvIssue> Validate(IReadOnlyDictionary<string, string> values, DeployTarget target, string repositoryPath)
    {
        var issues = new List<EnvIssue>();
        string Get(string key) => values.TryGetValue(key, out var value) ? value : "";

        foreach (var (key, value) in values)
        {
            if (value.Length == 0)
            {
                continue;
            }

            if (value.Contains('"') || value.Contains('\''))
            {
                issues.Add(new(IssueSeverity.Invalid, key, $"{key}: Anführungszeichen (\" oder ') sind nicht erlaubt."));
            }

            if (key.EndsWith("_PORT", StringComparison.Ordinal) && !(int.TryParse(value, out var port) && port is > 0 and <= 65535))
            {
                issues.Add(new(IssueSeverity.Invalid, key, $"{key}: kein gültiger Port (1–65535)."));
            }

            if ((key.EndsWith("_DAYS", StringComparison.Ordinal) || key.EndsWith("_PERMITS", StringComparison.Ordinal)
                 || key.EndsWith("_SECONDS", StringComparison.Ordinal)) && !(int.TryParse(value, out var number) && number > 0))
            {
                issues.Add(new(IssueSeverity.Invalid, key, $"{key}: muss eine positive ganze Zahl sein."));
            }

            if (BooleanKeys.Contains(key) && value is not ("true" or "false"))
            {
                issues.Add(new(IssueSeverity.Invalid, key, $"{key}: nur true oder false."));
            }
        }

        foreach (var key in RequiredKeys)
        {
            var value = Get(key);
            if (value.Length == 0)
            {
                issues.Add(new(IssueSeverity.Error, key, $"{key} fehlt."));
            }
            else if (value.StartsWith("change-me", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new(IssueSeverity.Error, key, $"{key} ist noch ein Platzhalter (change-me…)."));
            }
        }

        var jwtKey = Get("JWT_KEY");
        if (jwtKey.Length is > 0 and < 32)
        {
            issues.Add(new(IssueSeverity.Error, "JWT_KEY", "JWT_KEY muss mindestens 32 Zeichen lang sein."));
        }

        var legalName = Get("LEGAL_NAME");
        if (legalName.Length == 0 || legalName == "Max Mustermann")
        {
            issues.Add(new(IssueSeverity.Warning, "LEGAL_NAME", "Impressum/Datenschutz (LEGAL_*) ist noch nicht ausgefüllt."));
        }

@@END

        if (!File.Exists(Path.Combine(repositoryPath, "certs", "dartmetrics.pfx")))
        {
            issues.Add(new(IssueSeverity.Warning, null,
                @"HTTPS-Zertifikat certs\dartmetrics.pfx fehlt – die Erstinstallation erzeugt es (benötigt das .NET SDK)."));
        }

        return issues;
    }
}
