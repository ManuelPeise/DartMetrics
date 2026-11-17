using System.Text.RegularExpressions;

namespace Tools.Deployer.Configuration;

/// <summary>Ein Eintrag der .env, wie er in der .env.example beschrieben ist.</summary>
/// <param name="Key">Variablenname, z. B. MYSQL_PASSWORD.</param>
/// <param name="Section">Abschnitt aus der .env.example (z. B. "Datenbank (MySQL)").</param>
/// <param name="Help">Kommentarzeilen direkt über dem Eintrag.</param>
/// <param name="DefaultValue">Wert aus der Vorlage (bei auskommentierten Einträgen der Standardwert).</param>
/// <param name="IsOptional">In der Vorlage auskommentiert: leer lassen = Standardwert aus docker-compose.yml.</param>
internal sealed record EnvField(string Key, string Section, string Help, string DefaultValue, bool IsOptional)
{
    public bool IsSecret => Key.Contains("PASSWORD", StringComparison.Ordinal) || Key == "JWT_KEY";
}

/// <summary>Liest die .env.example als Vorlage für das Einstellungsformular.</summary>
internal static partial class EnvTemplate
{
    public const string OtherSection = "Weitere Einträge";

    // COMPOSE_FILE wird über die Umgebungsauswahl gesteuert (Produktion), nicht im Formular.
    private static readonly HashSet<string> IgnoredKeys = ["COMPOSE_FILE"];

@@END

    /// <summary>KEY=Wert oder auskommentiert "# KEY=Wert".</summary>
    [GeneratedRegex(@"^\s*(#\s*)?([A-Z][A-Z0-9_]*)=(.*)$")]
    internal static partial Regex AssignmentRegex();

    public static List<EnvField> Parse(IEnumerable<string> lines)
    {
        var fields = new List<EnvField>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var help = new List<string>();
        var header = new List<string>();
        var section = "Allgemein";
        var inHeader = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                help.Clear();
                continue;
            }

            // Abschnitt: "# ----" / "# Titel" / "# ----"
            if (line.StartsWith("# ---", StringComparison.Ordinal))
            {
                if (inHeader && header.Count > 0)
                {
                    section = header[0];
                    help.Clear();
                    help.AddRange(header.Skip(1));
                    inHeader = false;
                }
                else
                {
                    inHeader = true;
                    header.Clear();
                }

                continue;
            }

            var match = AssignmentRegex().Match(line);
            if (match.Success && !inHeader)
            {
                var key = match.Groups[2].Value;
                if (!IgnoredKeys.Contains(key) && seen.Add(key))
                {
                    fields.Add(new EnvField(
                        key,
                        section,
                        string.Join(Environment.NewLine, help),
                        EnvFile.Unquote(match.Groups[3].Value),
                        match.Groups[1].Success));
                }

                help.Clear();
                continue;
            }

            if (line.StartsWith('#'))
            {
                var text = line.TrimStart('#').Trim();
                if (inHeader)
                {
                    header.Add(text);
                }
                else
                {
                    help.Add(text);
                }
            }
        }

        return fields;
    }
}
