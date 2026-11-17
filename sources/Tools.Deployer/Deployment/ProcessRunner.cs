using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Tools.Deployer.Deployment;

/// <summary>Startet externe Programme (PowerShell, docker) ohne Fenster und liefert die Ausgabe zeilenweise.</summary>
internal static class ProcessRunner
{
    /// <summary>PowerShell 7 (pwsh), falls installiert, sonst Windows PowerShell.</summary>
    public static string PowerShellExecutable { get; } = FindInPath("pwsh.exe") ?? "powershell.exe";

    /// <summary>
    /// Ruft ein Skript auf und gibt dessen Exit-Code weiter. Ein "throw" im Skript (update-containers.ps1) ergibt Exit-Code 1.
    /// Bewusst ohne doppelte Anführungszeichen im Befehl, die Windows PowerShell beim Kommandozeilen-Parsing verändern würde.
    /// </summary>
    public static string[] PowerShellArguments(string scriptPath, IEnumerable<string> scriptArguments)
    {
        var quotedPath = scriptPath.Replace("'", "''", StringComparison.Ordinal);
        var command =
            "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " +
            $"try {{ & '{quotedPath}' {string.Join(' ', scriptArguments)}; exit $LASTEXITCODE }} " +
            "catch { Write-Output ('FEHLER: ' + $_.Exception.Message); exit 1 }";
        return ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", command];
    }

    /// <summary>Führt ein Programm aus, meldet jede Zeile (stdout und stderr) und liefert den Exit-Code. Abbruch beendet den ganzen Prozessbaum.</summary>
    public static async Task<int> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory,
        IReadOnlyDictionary<string, string?> environment,
        Action<string> onOutput,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = CreateStartInfo(fileName, arguments, workingDirectory, environment) };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutput(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            throw;
        }

        return process.ExitCode;
    }

    /// <summary>Kurzer Aufruf mit gesammelter Ausgabe (Status-Abfragen). Bei Zeitüberschreitung: Exit-Code -1.</summary>
    public static async Task<(int ExitCode, string Output)> CaptureAsync(string fileName, IEnumerable<string> arguments, TimeSpan timeout)
    {
        using var process = new Process { StartInfo = CreateStartInfo(fileName, arguments, null, null) };
        using var cancellation = new CancellationTokenSource(timeout);
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellation.Token);
        var errorTask = process.StandardError.ReadToEndAsync(cancellation.Token);

        try
        {
            await process.WaitForExitAsync(cancellation.Token);
            await errorTask;
            return (process.ExitCode, await outputTask);
        }
        catch (OperationCanceledException)
        {
            Kill(process);
            return (-1, "");
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string fileName, IEnumerable<string> arguments, string? workingDirectory, IReadOnlyDictionary<string, string?>? environment)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (workingDirectory is not null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        foreach (var (key, value) in environment ?? new Dictionary<string, string?>())
        {
            if (value is null)
            {
                startInfo.Environment.Remove(key);
            }
            else
            {
                startInfo.Environment[key] = value;
            }
        }

        return startInfo;
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            // Prozess ist bereits beendet.
        }
    }

    private static string? FindInPath(string executable)
    {
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim(), executable);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (ArgumentException)
            {
                // Ungültiger Eintrag im PATH.
            }
        }

        return null;
    }
}
