using System.Text;

namespace Tools.Deployer.Configuration;

/// <summary>
/// Liest und schreibt eine .env-Datei zeilengenau: Kommentare, Reihenfolge und nicht geänderte Zeilen bleiben erhalten.
/// </summary>
internal sealed class EnvFile
{
    private readonly List<string> lines;

    private EnvFile(string filePath, List<string> lines, bool exists)
    {
        FilePath = filePath;
        this.lines = lines;
        Exists = exists;
    }

    public string FilePath { get; }

    /// <summary>false: Datei gibt es noch nicht, der Inhalt stammt aus der Vorlage und wird beim Speichern angelegt.</summary>
    public bool Exists { get; private set; }

    public static EnvFile Load(string filePath, string fallbackPath)
    {
        if (File.Exists(filePath))
        {
            return new EnvFile(filePath, [.. File.ReadAllLines(filePath)], true);
        }

        string[] template = File.Exists(fallbackPath) ? File.ReadAllLines(fallbackPath) : [];
        return new EnvFile(filePath, [.. template], false);
    }

    /// <summary>Alle aktiven (nicht auskommentierten) Einträge.</summary>
    public Dictionary<string, string> ReadValues()
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            var match = EnvTemplate.AssignmentRegex().Match(line);
            if (match.Success && !match.Groups[1].Success)
            {
                values[match.Groups[2].Value] = Unquote(match.Groups[3].Value);
            }
        }

        return values;
    }

    /// <summary>
    /// Setzt einen Wert. Leer = Eintrag auskommentieren (docker-compose.yml nimmt dann den Standardwert).
    /// Eine vorhandene Zeile wird ersetzt, sonst die auskommentierte Vorlagenzeile, sonst wird angehängt.
    /// </summary>
    public void Set(string key, string value)
    {
        var active = -1;
        var commented = -1;
        for (var i = 0; i < lines.Count; i++)
        {
            var match = EnvTemplate.AssignmentRegex().Match(lines[i]);
            if (!match.Success || match.Groups[2].Value != key)
            {
                continue;
            }

            if (match.Groups[1].Success)
            {
                commented = commented < 0 ? i : commented;
            }
            else
            {
                active = i;
            }
        }

        if (value.Length == 0)
        {
            if (active >= 0)
            {
                lines[active] = $"# {key}=";
            }

            return;
        }

        var newLine = $"{key}={Quote(value)}";
        if (active >= 0)
        {
            lines[active] = newLine;
        }
        else if (commented >= 0)
        {
            lines[commented] = newLine;
        }
        else
        {
            lines.Add(newLine);
        }
    }

    public void Save()
    {
        File.WriteAllLines(FilePath, lines, new UTF8Encoding(false));
        Exists = true;
    }

    internal static string Unquote(string raw)
    {
        var value = raw.Trim();
        if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        // Unquotierte Werte: " #" leitet in Docker Compose einen Kommentar ein.
        var comment = value.IndexOf(" #", StringComparison.Ordinal);
        return comment >= 0 ? value[..comment].TrimEnd() : value;
    }

    /// <summary>
    /// Docker Compose ersetzt "$" in unquotierten Werten durch Variablen und schneidet ab "#" ab.
    /// In einfachen Anführungszeichen wird der Wert unverändert übernommen.
    /// </summary>
    private static string Quote(string value)
        => value.IndexOfAny(['$', '#']) >= 0 || value != value.Trim() ? $"'{value}'" : value;
}
