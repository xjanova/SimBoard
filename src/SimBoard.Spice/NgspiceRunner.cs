using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SimBoard.Spice;

/// <summary>
/// Runs one analysis in a separate ngspice process and returns its rawfile.
///
/// A child process — not an in-process library — on purpose:
///   • a circuit that hangs the solver cannot take unsaved work down with it,
///   • Stop is a real kill, not a cooperative flag the solver may never check,
///   • it keeps a clean licence boundary around the engine.
/// The cost is one process start per run (~30 ms), not per sample: the netlist goes
/// in once and the whole rawfile comes back once.
/// </summary>
public sealed partial class NgspiceRunner(string? enginePath = null)
{
    private readonly string? _enginePath = enginePath;

    /// <summary>Runs a netlist to completion. Cancelling kills the engine and everything it started.</summary>
    /// <param name="netlist">A full deck. Must end with <c>.end</c>; analysis cards included.</param>
    public async Task<SimulationResult> RunAsync(string netlist, CancellationToken ct = default)
    {
        var exe = _enginePath ?? NgspiceLocator.Find();
        var work = Directory.CreateTempSubdirectory("simboard-sim-");
        var deck = Path.Combine(work.FullName, "circuit.cir");
        var raw = Path.Combine(work.FullName, "out.raw");

        try
        {
            await File.WriteAllTextAsync(deck, Normalise(netlist), new UTF8Encoding(false), ct)
                      .ConfigureAwait(false);

            // ngspice defaults to an ASCII rawfile: ~3x the bytes and a full float parse per
            // sample. At the sample counts this product produces that is the difference between
            // a snappy scope and a stuttering one. ngspice reads .spiceinit from its working
            // directory, so we can force binary without touching the user's deck.
            await File.WriteAllTextAsync(Path.Combine(work.FullName, ".spiceinit"),
                                         "set filetype=binary\n", ct).ConfigureAwait(false);

            var sw = Stopwatch.StartNew();
            var (exitCode, log) = await ExecuteAsync(exe, deck, raw, work.FullName, ct).ConfigureAwait(false);
            sw.Stop();

            ThrowIfEngineFailed(exitCode, log, raw);
            return RawFileReader.Read(raw, sw.Elapsed, log);
        }
        finally
        {
            try { work.Delete(recursive: true); } catch (IOException) { /* the OS will reap it */ }
        }
    }

    private static async Task<(int ExitCode, string Log)> ExecuteAsync(
        string exe, string deck, string raw, string workDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(exe)
        {
            // -b batch, -r rawfile. No -o: we want the log on stdout, interleaved with errors.
            ArgumentList = { "-b", "-r", raw, deck },
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var log = new StringBuilder();
        var drained = new TaskCompletionSource();
        int pending = 2;

        void OnData(object _, DataReceivedEventArgs e)
        {
            if (e.Data is null)
            {
                if (Interlocked.Decrement(ref pending) == 0) drained.TrySetResult();
                return;
            }
            lock (log) log.AppendLine(e.Data);
        }

        proc.OutputDataReceived += OnData;
        proc.ErrorDataReceived += OnData;

        try { proc.Start(); }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new SpiceException(SpiceFailure.EngineMissing,
                $"The simulation engine at '{exe}' could not be started.", engineLog: e.Message);
        }

        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        proc.StandardInput.Close();   // batch mode reads no input; leaving it open can hang the engine

        try
        {
            await proc.WaitForExitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            KillTree(proc);
            throw new SpiceException(SpiceFailure.Cancelled, "The simulation was stopped.");
        }

        // Give the pipes a moment to flush; never block Stop on them.
        await Task.WhenAny(drained.Task, Task.Delay(2000, CancellationToken.None)).ConfigureAwait(false);
        lock (log) return (proc.ExitCode, log.ToString());
    }

    private static void KillTree(Process proc)
    {
        try
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree: true);
            proc.WaitForExit(5000);
        }
        catch (Exception e) when (e is InvalidOperationException or NotSupportedException
                                    or System.ComponentModel.Win32Exception)
        {
            // Already gone, or the OS refused — either way there is nothing left to do.
        }
    }

    // ── turning engine noise into something a UI can act on ──────────────

    private static void ThrowIfEngineFailed(int exitCode, string log, string rawPath)
    {
        if (FindConvergenceFailure(log) is { } conv) throw conv;

        if (!File.Exists(rawPath) || new FileInfo(rawPath).Length == 0)
        {
            if (NetlistError().Match(log) is { Success: true } m)
                throw new SpiceException(SpiceFailure.NetlistRejected,
                    m.Groups[1].Value.Trim(), engineLog: log);

            throw new SpiceException(SpiceFailure.NoOutput,
                exitCode == 0
                    ? "The engine finished without producing results. The netlist may have no analysis card (.tran / .ac / .op)."
                    : $"The engine stopped unexpectedly (exit code {exitCode}).",
                engineLog: log);
        }
    }

    /// <summary>
    /// Non-convergence is the failure users actually hit, and it is not their fault in any
    /// way they can see. We name the node ngspice blamed so the schematic can point at it.
    /// </summary>
    private static SpiceException? FindConvergenceFailure(string log)
    {
        if (TimestepTooSmall().IsMatch(log))
            return new SpiceException(SpiceFailure.NonConvergence,
                "The solver could not find a stable operating point and gave up shrinking its timestep.",
                node: null, engineLog: log);

        if (SingularMatrix().Match(log) is { Success: true } sm)
            return new SpiceException(SpiceFailure.NonConvergence,
                "Part of the circuit has no path to ground, so the solver cannot resolve it.",
                node: sm.Groups["node"].Value, engineLog: log);

        if (IterationLimit().IsMatch(log))
            return new SpiceException(SpiceFailure.NonConvergence,
                "The solver hit its iteration limit before the circuit settled.",
                node: null, engineLog: log);

        return null;
    }

    /// <summary>Ensures the deck has a title line and a terminator, the two things ngspice silently needs.</summary>
    internal static string Normalise(string netlist)
    {
        var text = netlist.Replace("\r\n", "\n").TrimEnd() + "\n";

        // ngspice treats the first line as a comment/title and will eat a real element there.
        var first = text.AsSpan(0, text.IndexOf('\n') is var i && i > 0 ? i : text.Length).Trim();
        if (first.Length > 0 && first[0] is not ('*' or '.'))
            text = "* SimBoard deck\n" + text;

        if (!text.Contains("\n.end", StringComparison.OrdinalIgnoreCase)
            && !text.StartsWith(".end", StringComparison.OrdinalIgnoreCase))
            text += ".end\n";

        return text;
    }

    [GeneratedRegex(@"[Tt]imestep too small", RegexOptions.CultureInvariant)]
    private static partial Regex TimestepTooSmall();

    [GeneratedRegex(@"singular matrix.*?check (?:node|nodes)\s+(?<node>[^\s,]+)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex SingularMatrix();

    [GeneratedRegex(@"iteration limit reached|too many iterations", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IterationLimit();

    [GeneratedRegex(@"^\s*Error on line.*?:\s*(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex NetlistError();
}
