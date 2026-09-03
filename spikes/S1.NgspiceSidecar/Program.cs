using System.Diagnostics;
using SimBoard.Spice;

// ─────────────────────────────────────────────────────────────────────────────
// S1 — ngspice sidecar spike.
// The riskiest assumption in the whole plan: that C# can drive a real SPICE
// engine as a child process, get trustworthy numbers back, and stop it dead.
// Every check below either passes on measurable evidence or fails loudly.
// ─────────────────────────────────────────────────────────────────────────────

int failures = 0;
var fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures");

Section("engine");
var version = NgspiceLocator.Version();
if (version is null) { Fail("ngspice not found or will not start"); return 1; }
Console.WriteLine($"  {version}");
Console.WriteLine($"  {NgspiceLocator.Find()}");

var runner = new NgspiceRunner();

// ── Check 1 · a transient run returns trustworthy numbers, fast ────────────
Section("check 1 — RC step response against theory");
{
    var result = await runner.RunAsync(await File.ReadAllTextAsync(Path.Combine(fixtures, "rc-step.cir")));
    var t = result.Require("time");
    var vout = result.Require("out");

    // v(tau) = 5 * (1 - 1/e) for tau = R*C = 100 us
    const double tau = 10e3 * 10e-9;
    double expected = 5.0 * (1.0 - 1.0 / Math.E);
    double actual = SampleAt(t, vout, tau);

    Console.WriteLine($"  {result.PointCount:N0} points · {result.Elapsed.TotalMilliseconds:F0} ms");
    Check("v(out) at t=tau", actual, expected, tolerance: 0.01, unit: "V");
}

// ── Check 2 · a real nonlinear circuit — the product's own 555 fixture ─────
Section("check 2 — 555 astable against the textbook formula");
{
    var sw = Stopwatch.StartNew();
    var result = await runner.RunAsync(await File.ReadAllTextAsync(Path.Combine(fixtures, "555-astable.cir")));
    sw.Stop();

    var t = result.Require("time");
    var outp = result.Require("out");

    const double ra = 10e3, rb = 47e3, c = 10e-9;
    double fExpected = 1.4427 / ((ra + 2 * rb) * c);
    double dExpected = (ra + rb) / (ra + 2 * rb);

    double? f = Measure.Frequency(t, outp);
    double? d = Measure.DutyCycle(t, outp);
    double vpp = Measure.Vpp(outp);
    double rms = Measure.Rms(t, outp);

    Console.WriteLine($"  {result.PointCount:N0} points · {result.Elapsed.TotalMilliseconds:F0} ms  ({result.PlotName})");
    Console.WriteLine($"  Vpp {vpp:F2} V · RMS {rms:F2} V (time-weighted)");

    // A transistor-level 555 is not the idealised comparator the formula assumes,
    // so 10 % is the honest band here. Anything wider means we have a real problem.
    Check("frequency", f ?? double.NaN, fExpected, tolerance: 0.10, unit: "Hz", relative: true);
    Check("duty cycle", d ?? double.NaN, dExpected, tolerance: 0.10, unit: "", relative: true);

    // The transport budget from the plan: a 20 ms transient back in under 2 s.
    bool fast = sw.Elapsed.TotalSeconds < 2.0;
    Report(fast, $"20 ms transient in {sw.Elapsed.TotalSeconds:F2} s (budget 2.00 s)");
}

// ── Check 3 · Stop really stops, and leaves nothing behind ─────────────────
Section("check 3 — cancellation kills the engine, no orphans");
{
    int before = CountEngines();

    // A long run we will interrupt part-way through.
    var deck = (await File.ReadAllTextAsync(Path.Combine(fixtures, "555-astable.cir")))
        .Replace(".tran 2u 20m uic", ".tran 200n 4000m uic", StringComparison.Ordinal);

    using var cts = new CancellationTokenSource();
    var sw = Stopwatch.StartNew();
    var run = runner.RunAsync(deck, cts.Token);

    await Task.Delay(700);
    cts.Cancel();

    bool cancelled = false;
    try { await run; }
    catch (SpiceException e) when (e.Failure == SpiceFailure.Cancelled) { cancelled = true; }
    catch (OperationCanceledException) { cancelled = true; }
    sw.Stop();

    Report(cancelled, $"Stop surfaced as a Cancelled failure after {sw.Elapsed.TotalMilliseconds:F0} ms");

    await Task.Delay(400);   // let the OS reap
    int after = CountEngines();
    Report(after <= before, $"engine processes {before} → {after} (no orphan left running)");
}

// ── Check 4 · a broken circuit fails as something the UI can explain ───────
Section("check 4 — a floating node fails with a named node, not raw engine text");
{
    // R2 hangs off a node with no DC path to ground: classic singular matrix.
    const string broken = """
        * SimBoard fixture - deliberately unsolvable
        V1 in 0 DC 5
        R1 in mid 1k
        C1 mid floaty 1u
        R2 floaty orphan 1k
        .op
        .end
        """;
    try
    {
        await runner.RunAsync(broken);
        Fail("expected the engine to reject this circuit, but it succeeded");
    }
    catch (SpiceException e)
    {
        bool typed = e.Failure is SpiceFailure.NonConvergence or SpiceFailure.NetlistRejected or SpiceFailure.NoOutput;
        Report(typed, $"typed as {e.Failure}" + (e.Node is null ? "" : $", node '{e.Node}'"));
        Console.WriteLine($"    message shown to user: \"{e.Message}\"");
        Report(!e.Message.Contains("ngspice", StringComparison.OrdinalIgnoreCase),
               "message contains no engine jargon");
    }
}

Console.WriteLine();
Console.WriteLine(failures == 0
    ? "S1 PASSED — the sidecar architecture holds. Safe to build Phase 3 on it."
    : $"S1 FAILED — {failures} check(s) did not pass.");
return failures == 0 ? 0 : 1;

// ── helpers ────────────────────────────────────────────────────────────────

void Section(string name)
{
    Console.WriteLine();
    Console.WriteLine($"── {name} ".PadRight(74, '─'));
}

void Check(string label, double actual, double expected, double tolerance, string unit, bool relative = false)
{
    double err = relative ? Math.Abs(actual - expected) / Math.Abs(expected) : Math.Abs(actual - expected);
    bool ok = !double.IsNaN(actual) && err <= tolerance;
    string u = unit.Length > 0 ? " " + unit : "";
    Report(ok, $"{label}: {actual:G6}{u} vs theory {expected:G6}{u} " +
               $"({(relative ? $"{err * 100:F2} % off" : $"Δ {err:G3}{u}")})");
}

void Report(bool ok, string message)
{
    if (!ok) failures++;
    Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {message}");
}

void Fail(string message) { failures++; Console.WriteLine($"  [FAIL] {message}"); }

static double SampleAt(SpiceVector time, SpiceVector v, double at)
{
    for (int i = 1; i < time.Count; i++)
    {
        if (time.Values[i] < at) continue;
        double dt = time.Values[i] - time.Values[i - 1];
        double f = dt <= 0 ? 0 : (at - time.Values[i - 1]) / dt;
        return v.Values[i - 1] + f * (v.Values[i] - v.Values[i - 1]);
    }
    return v.Values[^1];
}

static int CountEngines()
{
    try { return Process.GetProcessesByName("ngspice_con").Length + Process.GetProcessesByName("ngspice").Length; }
    catch (InvalidOperationException) { return 0; }
}
