using System.Text.Json;

namespace Tools.Deployer;

/// <summary>Merkt sich Lösungsordner und Umgebung zwischen zwei Starts (%AppData%\DartMetricsDeployer\settings.json).</summary>
internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DartMetricsDeployer", "settings.json");

    public string? RepositoryPath { get; set; }

    public string? TargetName { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // Defekte oder gesperrte Datei: mit Standardwerten starten.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Nicht kritisch: dann wird beim nächsten Start erneut gesucht.
        }
    }
}
