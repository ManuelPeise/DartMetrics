using System.ComponentModel;

namespace Tools.Deployer.Deployment;

/// <param name="Name">Containername, z. B. dartmetrics-api.</param>
/// <param name="Status">running, exited, … oder "nicht vorhanden" / "unbekannt".</param>
/// <param name="Health">healthy, starting, unhealthy oder leer (kein Healthcheck).</param>
internal sealed record ContainerState(string Name, string Status, string Health);

/// <summary>Fragt Docker Desktop und die Container ab.</summary>
internal static class DockerStatus
{
    public const string Missing = "nicht vorhanden";
    public const string Unknown = "unbekannt";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static string DesktopPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe");

    public static async Task<bool> IsRunningAsync()
    {
        try
        {
            var (exitCode, _) = await ProcessRunner.CaptureAsync("docker", ["info", "--format", "{{.ServerVersion}}"], Timeout);
            return exitCode == 0;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            // docker ist nicht installiert bzw. nicht im PATH.
            return false;
        }
    }

    public static async Task<List<ContainerState>> GetContainersAsync(IReadOnlyList<string> names)
    {
        var states = names.ToDictionary(name => name, name => new ContainerState(name, Missing, ""));
        try
        {
            // Fehlende Container liefern einen Fehler-Exit-Code, die vorhandenen stehen trotzdem in der Ausgabe.
            var (_, output) = await ProcessRunner.CaptureAsync(
                "docker",
                ["inspect", "--format", "{{.Name}}|{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{end}}", .. names],
                Timeout);

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = line.Split('|');
                if (parts.Length == 3)
                {
                    var name = parts[0].TrimStart('/');
                    if (states.ContainsKey(name))
                    {
                        states[name] = new ContainerState(name, parts[1], parts[2]);
                    }
                }
            }
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return names.Select(name => new ContainerState(name, Unknown, "")).ToList();
        }

        return names.Select(name => states[name]).ToList();
    }
}
