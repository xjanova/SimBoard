using System.Runtime.InteropServices;

namespace SimBoard.Spice;

/// <summary>
/// Finds the ngspice console binary. We deliberately run the *console* build
/// (<c>ngspice_con.exe</c>) — the plain <c>ngspice.exe</c> opens a window.
/// </summary>
public static class NgspiceLocator
{
    /// <summary>Overrides every other lookup. Set it when shipping a bundled engine.</summary>
    public const string EnvVar = "SIMBOARD_NGSPICE";

    private static readonly string[] ExeNames =
        OperatingSystem.IsWindows() ? ["ngspice_con.exe", "ngspice.exe"] : ["ngspice"];

    private static string? _cached;

    public static string Find()
    {
        if (_cached is not null) return _cached;

        foreach (var candidate in Candidates())
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                return _cached = Path.GetFullPath(candidate);

        throw new SpiceException(SpiceFailure.EngineMissing,
            "The simulation engine is not installed. Run tools/fetch-ngspice.ps1, or set " +
            $"{EnvVar} to the full path of ngspice_con.exe.");
    }

    public static bool TryFind(out string path)
    {
        try { path = Find(); return true; }
        catch (SpiceException) { path = ""; return false; }
    }

    private static IEnumerable<string> Candidates()
    {
        if (Environment.GetEnvironmentVariable(EnvVar) is { Length: > 0 } fromEnv)
            yield return fromEnv;

        // Walk up from the running assembly to the repo root, so `dotnet run` from
        // any project finds tools/Spice64/bin without configuration.
        var dir = AppContext.BaseDirectory;
        for (int depth = 0; depth < 8 && dir is not null; depth++)
        {
            foreach (var exe in ExeNames)
            {
                yield return Path.Combine(dir, "tools", "Spice64", "bin", exe);
                yield return Path.Combine(dir, "ngspice", exe);
            }
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }

        foreach (var exe in ExeNames)
            foreach (var onPath in FromPath(exe))
                yield return onPath;

        if (OperatingSystem.IsWindows())
            foreach (var root in new[] { "C:\\Program Files", "C:\\", "D:\\" })
                foreach (var exe in ExeNames)
                    yield return Path.Combine(root, "Spice64", "bin", exe);
    }

    private static IEnumerable<string> FromPath(string exe)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string full;
            try { full = Path.Combine(dir.Trim('"'), exe); }
            catch (ArgumentException) { continue; }   // malformed PATH entries are common on Windows
            yield return full;
        }
    }

    /// <summary>For diagnostics: the engine banner, or null if it will not start.</summary>
    public static string? Version()
    {
        if (!TryFind(out var exe)) return null;
        try
        {
            using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, "--version")
            { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true });
            if (p is null) return null;
            var text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5000);
            return text.Split('\n').FirstOrDefault(l => l.Contains("ngspice-", StringComparison.OrdinalIgnoreCase))?.Trim();
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return null;
        }
    }
}
