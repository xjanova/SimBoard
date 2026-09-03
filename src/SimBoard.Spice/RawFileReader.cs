using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace SimBoard.Spice;

/// <summary>
/// Reads ngspice's native rawfile. The header is ASCII lines terminated by a
/// <c>Binary:</c> or <c>Values:</c> marker; everything after it is the sample data.
/// Only the first plot in a multi-plot file is returned — one analysis, one result.
/// </summary>
public static class RawFileReader
{
    public static SimulationResult Read(string path, TimeSpan elapsed, string engineLog)
    {
        using var fs = File.OpenRead(path);
        return Read(fs, elapsed, engineLog);
    }

    public static SimulationResult Read(Stream stream, TimeSpan elapsed, string engineLog)
    {
        var bytes = ReadAll(stream);
        var found = FindMarker(bytes);
        if (found is null)
            throw new SpiceException(SpiceFailure.RawFileCorrupt,
                "The rawfile has no data marker — ngspice wrote a header but no samples.", engineLog: engineLog);

        var (headerEnd, dataStart, isBinary) = found.Value;
        var header = Encoding.ASCII.GetString(bytes, 0, headerEnd);
        var h = ParseHeader(header, engineLog);

        var values = bytes.AsSpan(dataStart);
        var vectors = h.IsComplex && isBinary
            ? ReadComplexMagnitudes(values, h)
            : ReadReal(values, h, isBinary ? null : Encoding.ASCII.GetString(values));

        return new SimulationResult(h.PlotName, vectors, elapsed, engineLog);
    }

    // ── header ───────────────────────────────────────────────────────────

    private sealed record Header(
        string PlotName, bool IsComplex, int VariableCount, int DeclaredPoints,
        string[] Names, string[] Units);

    private static Header ParseHeader(string text, string engineLog)
    {
        string plot = "Unknown";
        bool complex = false;
        int nVars = 0, nPoints = 0;
        var names = new List<string>();
        var units = new List<string>();
        bool inVariables = false;

        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0) continue;

            if (!inVariables)
            {
                if (line.StartsWith("Plotname:", StringComparison.OrdinalIgnoreCase))
                { plot = line[9..].Trim(); continue; }
                if (line.StartsWith("Flags:", StringComparison.OrdinalIgnoreCase))
                { complex = line.Contains("complex", StringComparison.OrdinalIgnoreCase); continue; }
                if (line.StartsWith("No. Variables:", StringComparison.OrdinalIgnoreCase))
                { nVars = ParseInt(line[14..]); continue; }
                if (line.StartsWith("No. Points:", StringComparison.OrdinalIgnoreCase))
                { nPoints = ParseInt(line[11..]); continue; }
                if (line.StartsWith("Variables:", StringComparison.OrdinalIgnoreCase))
                { inVariables = true; continue; }
                continue;
            }

            // "\t<index>\t<name>\t<type>[ optional flags]"
            var parts = line.Split(['\t', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;
            names.Add(parts[1]);
            units.Add(parts[2]);
            if (names.Count == nVars) break;
        }

        if (nVars <= 0 || names.Count == 0)
            throw new SpiceException(SpiceFailure.RawFileCorrupt,
                "The rawfile header declares no variables.", engineLog: engineLog);

        return new Header(plot, complex, names.Count, nPoints, [.. names], [.. units]);
    }

    private static int ParseInt(string s) =>
        int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    // ── data ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Locates the end of the ASCII header. ngspice writes CRLF on Windows and LF elsewhere,
    /// and switches between "Binary:" and "Values:" depending on the <c>filetype</c> variable —
    /// so match the bare word and step over whatever newline follows.
    /// </summary>
    private static (int HeaderEnd, int DataStart, bool IsBinary)? FindMarker(byte[] bytes)
    {
        foreach (var (word, binary) in new[] { ("Binary:", true), ("Values:", false) })
        {
            int idx = IndexOf(bytes, Encoding.ASCII.GetBytes(word));
            if (idx < 0) continue;

            int start = idx + word.Length;
            if (start < bytes.Length && bytes[start] == (byte)'\r') start++;
            if (start < bytes.Length && bytes[start] == (byte)'\n') start++;
            return (idx, start, binary);
        }
        return null;
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i + needle.Length <= haystack.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < needle.Length; j++)
                if (haystack[i + j] != needle[j]) { ok = false; break; }
            if (ok) return i;
        }
        return -1;
    }

    private static SpiceVector[] ReadReal(ReadOnlySpan<byte> data, Header h, string? asciiBody)
    {
        if (asciiBody is not null) return ReadAscii(asciiBody, h);

        // Trust the byte count over the declared header count: ngspice writes the
        // header before it knows how many timesteps the solver will take.
        int available = data.Length / (8 * h.VariableCount);
        int points = h.DeclaredPoints > 0 ? Math.Min(h.DeclaredPoints, available) : available;
        if (points <= 0)
            throw new SpiceException(SpiceFailure.NoOutput,
                "ngspice produced a rawfile with zero samples.");

        var vectors = new double[h.VariableCount][];
        for (int v = 0; v < h.VariableCount; v++) vectors[v] = new double[points];

        int offset = 0;
        for (int p = 0; p < points; p++)
            for (int v = 0; v < h.VariableCount; v++, offset += 8)
                vectors[v][p] = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);

        return Build(h, vectors);
    }

    /// <summary>AC results arrive as complex pairs; we keep the magnitude, which is what a Bode plot draws.</summary>
    private static SpiceVector[] ReadComplexMagnitudes(ReadOnlySpan<byte> data, Header h)
    {
        int available = data.Length / (16 * h.VariableCount);
        int points = h.DeclaredPoints > 0 ? Math.Min(h.DeclaredPoints, available) : available;
        if (points <= 0)
            throw new SpiceException(SpiceFailure.NoOutput, "ngspice produced a rawfile with zero samples.");

        var vectors = new double[h.VariableCount][];
        for (int v = 0; v < h.VariableCount; v++) vectors[v] = new double[points];

        int offset = 0;
        for (int p = 0; p < points; p++)
            for (int v = 0; v < h.VariableCount; v++, offset += 16)
            {
                double re = BinaryPrimitives.ReadDoubleLittleEndian(data[offset..(offset + 8)]);
                double im = BinaryPrimitives.ReadDoubleLittleEndian(data[(offset + 8)..(offset + 16)]);
                vectors[v][p] = double.Hypot(re, im);
            }

        return Build(h, vectors);
    }

    private static SpiceVector[] ReadAscii(string body, Header h)
    {
        var nums = new List<double>();
        foreach (var tok in body.Split(['\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(tok, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                nums.Add(d);

        // ASCII rows are "<point index> <v0>\n\t<v1>\n\t<v2>…", so the index is one extra number per point.
        int stride = h.VariableCount + 1;
        int points = nums.Count / stride;
        if (points <= 0)
            throw new SpiceException(SpiceFailure.NoOutput, "The ASCII rawfile contains no samples.");

        var vectors = new double[h.VariableCount][];
        for (int v = 0; v < h.VariableCount; v++) vectors[v] = new double[points];
        for (int p = 0; p < points; p++)
            for (int v = 0; v < h.VariableCount; v++)
                vectors[v][p] = nums[p * stride + 1 + v];

        return Build(h, vectors);
    }

    private static SpiceVector[] Build(Header h, double[][] data)
    {
        var result = new SpiceVector[h.VariableCount];
        for (int v = 0; v < h.VariableCount; v++)
            result[v] = new SpiceVector(h.Names[v], h.Units[v], data[v]);
        return result;
    }

    private static byte[] ReadAll(Stream s)
    {
        if (s is MemoryStream ms) return ms.ToArray();
        using var buf = new MemoryStream();
        s.CopyTo(buf);
        return buf.ToArray();
    }
}
