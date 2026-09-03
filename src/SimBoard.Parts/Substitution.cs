namespace SimBoard.Parts;

public enum Severity
{
    /// <summary>Do not fit this part. It will fail, and probably take something with it.</summary>
    Blocking,
    /// <summary>It will physically work but changes circuit behaviour. The user must decide.</summary>
    Serious,
    /// <summary>Worth knowing before soldering.</summary>
    Caution,
}

/// <summary>What a rule demands of the candidate, relative to the part being replaced.</summary>
public enum Compare { AtLeast, AtMost, GainOverlap }

/// <summary>
/// Findings are codes, not sentences. The product switches language live, so the engine
/// must never bake a language into its output.
/// </summary>
public enum FindingCode
{
    WrongPolarity, MissingData,
    BelowRating, HigherLoss, MarginThin, HeavilyOverRated,
    GainLower, GainHigher,
    SlowerSwitching,
    DifferentPinout, DifferentPackage,
    UnverifiedData,
}

public sealed record Finding(Severity Severity, FindingCode Code, params string[] Args);

public sealed record Substitute(Part Part, double Score, IReadOnlyList<Finding> Findings)
{
    /// <summary>False when at least one blocking finding applies — never present these as options.</summary>
    public bool Usable => !Findings.Any(f => f.Severity == Severity.Blocking);
    public bool NeedsAttention => Findings.Any(f => f.Severity == Severity.Serious);
}

public sealed record Rule(ParamKey Key, Compare Cmp, Severity WhenViolated);

/// <summary>
/// Ranks candidate substitutes by the parameters that actually decide whether a swap
/// survives — not by a lookup table. The advantage over a printed cross-reference is
/// that this can say <em>why</em>, and warn about the trade-off it is making.
/// </summary>
public static class Substitution
{
    private static readonly Rule[] BjtRules =
    [
        new(ParamKey.Vceo,   Compare.AtLeast,     Severity.Blocking),
        new(ParamKey.Ic,     Compare.AtLeast,     Severity.Blocking),
        new(ParamKey.Ptot,   Compare.AtLeast,     Severity.Blocking),
        new(ParamKey.Ft,     Compare.AtLeast,     Severity.Serious),
        new(ParamKey.HfeMin, Compare.GainOverlap, Severity.Serious),
    ];

    private static readonly Rule[] MosfetRules =
    [
        new(ParamKey.Vds,      Compare.AtLeast, Severity.Blocking),
        new(ParamKey.Id,       Compare.AtLeast, Severity.Blocking),
        new(ParamKey.RdsOn,    Compare.AtMost,  Severity.Serious),
        new(ParamKey.VgsThMax, Compare.AtMost,  Severity.Serious),
        new(ParamKey.Qg,       Compare.AtMost,  Severity.Caution),
    ];

    private static readonly Rule[] DiodeRules =
    [
        new(ParamKey.Vrrm, Compare.AtLeast, Severity.Blocking),
        new(ParamKey.If,   Compare.AtLeast, Severity.Blocking),
        new(ParamKey.Trr,  Compare.AtMost,  Severity.Serious),
        new(ParamKey.Vf,   Compare.AtMost,  Severity.Caution),
    ];

    public static IReadOnlyList<Rule> RulesFor(PartCategory c) => c switch
    {
        PartCategory.Bjt => BjtRules,
        PartCategory.Mosfet => MosfetRules,
        PartCategory.Diode or PartCategory.Zener => DiodeRules,
        _ => [],
    };

    /// <summary>
    /// Candidates that can stand in for <paramref name="original"/>, best first.
    /// Unusable candidates are dropped unless <paramref name="includeRejected"/> —
    /// "why can't I use this one" is a question worth being able to answer.
    /// </summary>
    public static IReadOnlyList<Substitute> Find(
        Part original, IEnumerable<Part> library, int limit = 10, bool includeRejected = false)
    {
        var rules = RulesFor(original.Category);
        var results = new List<Substitute>();

        foreach (var candidate in library)
        {
            if (candidate.Mpn.Equals(original.Mpn, StringComparison.OrdinalIgnoreCase)) continue;
            if (candidate.Category != original.Category) continue;

            var sub = Evaluate(original, candidate, rules);
            if (sub.Usable || includeRejected) results.Add(sub);
        }

        return [.. results.OrderByDescending(s => s.Usable)
                          .ThenByDescending(s => s.Score)
                          .Take(limit)];
    }

    private static Substitute Evaluate(Part original, Part candidate, IReadOnlyList<Rule> rules)
    {
        var findings = new List<Finding>();
        double score = 0;
        int scored = 0;

        if (original.Polarity != Polarity.None && candidate.Polarity != original.Polarity)
            findings.Add(new Finding(Severity.Blocking, FindingCode.WrongPolarity,
                candidate.Polarity.ToString(), original.Polarity.ToString()));

        foreach (var rule in rules)
        {
            if (rule.Cmp == Compare.GainOverlap) { ScoreGain(original, candidate, findings); continue; }

            double? o = original.Get(rule.Key), c = candidate.Get(rule.Key);
            if (o is null || c is null)
            {
                findings.Add(new Finding(Severity.Caution, FindingCode.MissingData, rule.Key.ToString()));
                continue;
            }

            // headroom > 1 means the candidate has margin over the original, whichever
            // direction "better" runs for this parameter.
            double headroom = rule.Cmp == Compare.AtLeast ? c.Value / o.Value : o.Value / c.Value;
            string unit = Eng.UnitOf(rule.Key);

            if (headroom < 0.999)
            {
                // How far below matters. A 250 MHz part replacing a 300 MHz one is a
                // footnote; a 3 MHz part replacing it is a different component.
                var severity = rule.WhenViolated == Severity.Blocking ? Severity.Blocking
                             : headroom >= 0.8 ? Severity.Caution
                             : Severity.Serious;
                findings.Add(new Finding(severity, Pick(rule),
                    rule.Key.ToString(), Eng.Format(c.Value, unit), Eng.Format(o.Value, unit)));
                score += Penalty(severity);
            }
            else
            {
                // Exactly equal is a perfect match, not a thin margin — say nothing.
                if (headroom is > 1.001 and < 1.15)
                    findings.Add(new Finding(Severity.Caution, FindingCode.MarginThin,
                        rule.Key.ToString(), $"{(headroom - 1) * 100:F0}"));
                else if (headroom > 12)
                    findings.Add(new Finding(Severity.Caution, FindingCode.HeavilyOverRated,
                        rule.Key.ToString(), $"{headroom:F0}"));

                // Reward adequate margin, then stop: a 15 A transistor in a 200 mA socket
                // is not a better answer, just a bigger one. Past 12x it scores worse than
                // a snug fit, because it usually is worse.
                score += headroom > 12 ? 1.0 : Math.Min(headroom, 2.5);
            }
            scored++;
        }

        // Physical fit outranks headroom. A part that beats the original on every
        // electrical number is still the wrong answer if it will not go in the hole.
        if (!string.Equals(candidate.Package, original.Package, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding(Severity.Serious, FindingCode.DifferentPackage,
                candidate.Package, original.Package));
            score += Penalty(Severity.Serious);
        }
        else score += 2.0;

        // The trap that bites technicians: identical electrical specs, legs in a different order.
        if (original.Pinout is { Length: > 0 } op && candidate.Pinout is { Length: > 0 } cp)
        {
            if (!string.Equals(op, cp, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding(Severity.Serious, FindingCode.DifferentPinout, cp, op));
                score += Penalty(Severity.Serious);
            }
            else score += 2.0;
        }

        if (candidate.Provenance == Provenance.Unverified)
            findings.Add(new Finding(Severity.Caution, FindingCode.UnverifiedData));

        return new Substitute(candidate, scored == 0 ? score : score / scored, findings);
    }

    private static double Penalty(Severity s) => s switch
    {
        Severity.Blocking => -100,
        Severity.Serious => -6,
        _ => -0.5,
    };

    /// <summary>
    /// Picks wording that matches the direction of the parameter. "Below the original"
    /// is only true for ratings where more is better; for R_DS(on) or V_F the candidate
    /// failing means it is *higher*, and saying "below" reads as nonsense to a technician.
    /// </summary>
    private static FindingCode Pick(Rule rule) => rule.Key switch
    {
        ParamKey.Ft or ParamKey.Trr or ParamKey.Qg => FindingCode.SlowerSwitching,
        _ when rule.Cmp == Compare.AtMost => FindingCode.HigherLoss,
        _ => FindingCode.BelowRating,
    };

    private static void ScoreGain(Part original, Part candidate, List<Finding> findings)
    {
        double? oMin = original.Get(ParamKey.HfeMin), cMin = candidate.Get(ParamKey.HfeMin);
        double? oMax = original.Get(ParamKey.HfeMax), cMax = candidate.Get(ParamKey.HfeMax);
        if (oMin is null || cMin is null) return;

        if (cMin < oMin * 0.7)
            findings.Add(new Finding(Severity.Serious, FindingCode.GainLower,
                $"{cMin:F0}", $"{oMin:F0}"));
        else if (oMax is not null && cMax is not null && cMax > oMax * 2.5)
            findings.Add(new Finding(Severity.Caution, FindingCode.GainHigher,
                $"{cMax:F0}", $"{oMax:F0}"));
    }
}
