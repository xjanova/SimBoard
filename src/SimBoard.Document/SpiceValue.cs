using System.Globalization;
using System.Text.RegularExpressions;

namespace SimBoard.Document;

/// <summary>
/// Parses the engineering values people actually type and emits something SPICE cannot
/// misread.
///
/// The reason this exists rather than passing the text through: in SPICE, <c>M</c> means
/// MILLI. Everywhere else in electronics 1M is one megohm. A user typing "1M" for a
/// megohm gets a milliohm — a factor of a billion, in the direction of a short, with no
/// error message. Emitting a plain number removes the ambiguity permanently instead of
/// hoping the reader guessed the same way we did.
///
/// It also accepts the infix style people write by hand — 4k7, 1R2, 2u2 — which SPICE
/// does not understand at all.
/// </summary>
public static partial class SpiceValue
{
    /// <summary>
    /// SI prefixes as an engineer means them. 'M' is mega here, deliberately: this maps
    /// what the user typed, and no one writes a milliohm as "1M".
    /// </summary>
    private static double? Multiplier(string suffix) => suffix switch
    {
        "" => 1,
        "f" or "F" => 1e-15,
        "p" or "P" => 1e-12,
        "n" or "N" => 1e-9,
        "u" or "U" or "µ" or "μ" => 1e-6,
        "m" => 1e-3,
        "k" or "K" => 1e3,
        "M" => 1e6,
        "meg" or "MEG" or "Meg" => 1e6,
        "g" or "G" => 1e9,
        "t" or "T" => 1e12,
        "R" or "r" or "E" => 1,          // 4R7 and 4E7 both mean 4.7 ohms
        _ => null,
    };

    /// <summary>
    /// Parses "10k", "4k7", "100n", "1M", "2.2u", "1meg". Returns null when the text is
    /// not a plain magnitude — a PULSE(...) spec or an expression, which must pass through
    /// to SPICE untouched.
    /// </summary>
    public static double? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var s = text.Trim();

        // Infix form: the prefix letter stands in for the decimal point. 4k7 = 4.7k.
        if (Infix().Match(s) is { Success: true } infix)
        {
            var unit = Multiplier(infix.Groups["p"].Value);
            if (unit is null) return null;
            var whole = double.Parse(infix.Groups["a"].Value, CultureInfo.InvariantCulture);
            var frac = double.Parse("0." + infix.Groups["b"].Value, CultureInfo.InvariantCulture);
            return (whole + frac) * unit.Value;
        }

        if (Suffixed().Match(s) is { Success: true } m)
        {
            var unit = Multiplier(m.Groups["p"].Value);
            if (unit is null) return null;
            if (!double.TryParse(m.Groups["n"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
                return null;
            return n * unit.Value;
        }

        return null;
    }

    /// <summary>
    /// The value as SPICE should receive it. A magnitude becomes an unambiguous number;
    /// anything else — PULSE, SIN, a model name — is handed through unchanged.
    /// </summary>
    public static string ForSpice(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "0";
        return Parse(text) is { } v
            ? v.ToString("G12", CultureInfo.InvariantCulture)
            : text.Trim();
    }

    /// <summary>The value as a person should read it — the datasheet's own notation.</summary>
    public static string ForDisplay(double value, string unit = "") => Eng.Format(value, unit);

    /// <summary>True when the text is a magnitude rather than a waveform or expression.</summary>
    public static bool IsMagnitude(string? text) => Parse(text) is not null;

    // 4k7, 1R2, 2u2 — digits, prefix letter, digits.
    [GeneratedRegex(@"^(?<a>\d+)(?<p>[fFpPnNuUµμmkKMgGtTRrE]|meg|MEG|Meg)(?<b>\d+)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Infix();

    // 10k, 2.2u, 1meg, 47, 1e-9
    [GeneratedRegex(@"^(?<n>[+-]?(?:\d+\.?\d*|\.\d+)(?:[eE][+-]?\d+)?)\s*(?<p>meg|MEG|Meg|[fFpPnNuUµμmkKMgGtTRrE]?)\s*(?:[ΩΩohmsFHV]*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex Suffixed();
}
