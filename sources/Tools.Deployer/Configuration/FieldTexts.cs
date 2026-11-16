namespace Tools.Deployer.Configuration;

/// <summary>Verständliche Beschriftungen für die Einträge der .env (Fallback: der Variablenname).</summary>
internal static class FieldTexts
{
    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.Ordinal)
    {
        ["MYSQL_ROOT_PASSWORD"] = "MySQL Root-Passwort",
        ["MYSQL_PASSWORD"] = "MySQL App-Passwort",
        ["JWT_KEY"] = "JWT-Schlüssel",
        ["ADMIN_EMAIL"] = "Admin: E-Mail",
        ["ADMIN_PASSWORD"] = "Admin: Startpasswort",
        ["API_ENVIRONMENT"] = "API-Umgebung",
        ["SMTP_SERVER"] = "SMTP-Server",
        ["SMTP_PORT"] = "SMTP-Port",
        ["SMTP_USERNAME"] = "SMTP-Benutzer",
        ["SMTP_PASSWORD"] = "SMTP-Passwort",
        ["SMTP_FROM_ADDRESS"] = "Absenderadresse",
        ["SMTP_ENABLE_SSL"] = "SMTP mit SSL",
        ["CERT_PASSWORD"] = "Zertifikat-Passwort",
        ["WEB_APP_HOST"] = "Öffentliche Adresse",
        ["WEB_APP_PORT"] = "Öffentlicher Port",
        ["FORWARDED_HEADERS_ENABLED"] = "Hinter Reverse Proxy",
        ["RATE_LIMIT_AUTH_PERMITS"] = "Anmeldeversuche je Zeitfenster",
        ["RATE_LIMIT_AUTH_WINDOW_SECONDS"] = "Zeitfenster (Sekunden)",
        ["LEGAL_NAME"] = "Name",
        ["LEGAL_STREET"] = "Straße",
        ["LEGAL_CITY"] = "PLZ und Ort",
        ["LEGAL_COUNTRY"] = "Land",
        ["LEGAL_EMAIL"] = "E-Mail",
        ["LEGAL_PHONE"] = "Telefon",
        ["RETENTION_UNVERIFIED_DAYS"] = "Unbestätigte Konten löschen nach (Tagen)",
        ["RETENTION_LOG_DAYS"] = "Log-Einträge löschen nach (Tagen)",
        ["BETA_HTTPS_PORT"] = "Beta: HTTPS-Port",
        ["BETA_HTTP_PORT"] = "Beta: HTTP-Port",
    };

    private static readonly Dictionary<string, string> Notes = new(StringComparer.Ordinal)
    {
        ["MYSQL_ROOT_PASSWORD"] = "Wirkt nur beim allerersten Start (leere Datenbank). Danach muss es in MySQL selbst geändert werden.",
        ["MYSQL_PASSWORD"] = "Wirkt nur beim allerersten Start (leere Datenbank). Danach muss es in MySQL selbst geändert werden.",
        ["ADMIN_PASSWORD"] = "Wird nur beim ersten Start verwendet; beim ersten Login muss es geändert werden.",
    };

    public static string DisplayName(string key) => DisplayNames.TryGetValue(key, out var name) ? name : key;

    public static string Help(EnvField field)
    {
        var parts = new List<string> { field.Key };
        if (field.Help.Length > 0)
        {
            parts.Add(field.Help);
        }

        if (Notes.TryGetValue(field.Key, out var note))
        {
            parts.Add("Achtung: " + note);
        }

        return string.Join(" – ", parts);
    }
}
