using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimBoard.Document;

/// <summary>
/// Reading and writing <c>.sbp</c> project files.
///
/// Plain JSON, and deliberately so: a circuit someone spent a day on should be
/// recoverable with a text editor when the tool that wrote it is gone, and readable in a
/// diff so a change to a board can be reviewed like any other change.
///
/// Parts store the catalogue <see cref="PartDefinition.Key"/>, never the definition
/// itself. A file written today must pick up a corrected pin table shipped next month —
/// baking the pins into the document would freeze yesterday's mistakes into every saved
/// project.
/// </summary>
public static class ProjectFile
{
    public const string Extension = ".sbp";

    /// <summary>Bumped only for a change old readers cannot cope with.</summary>
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Save(CircuitDocument doc, string path)
    {
        var dto = new ProjectDto
        {
            Version = CurrentVersion,
            Title = doc.Title,
            Parts = [.. doc.Parts.Select(p => new PartDto
            {
                Id = p.Id,
                Key = p.Definition.Key,
                Designator = p.Designator,
                X = p.Position.X,
                Y = p.Position.Y,
                Rotation = (int)p.Rotation,
                Value = p.Value,
                Locked = p.Locked ? true : null,
            })],
            Wires = [.. doc.Wires.Select(w => new WireDto
            {
                Id = w.Id, Ax = w.A.X, Ay = w.A.Y, Bx = w.B.X, By = w.B.Y,
            })],
            Labels = [.. doc.Labels.Select(l => new LabelDto
            {
                Id = l.Id, Name = l.Name, X = l.At.X, Y = l.At.Y,
            })],
        };

        // Write to a temp file and move it into place. A crash mid-write must not leave a
        // truncated project where a working one used to be.
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(dto, Json));
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>
    /// Loads a project. Parts whose catalogue key no longer exists are reported rather
    /// than dropped in silence — a missing part changes the circuit, and the user has to
    /// know which one went.
    /// </summary>
    public static (CircuitDocument Document, IReadOnlyList<string> Warnings) Load(string path)
    {
        var dto = JsonSerializer.Deserialize<ProjectDto>(File.ReadAllText(path), Json)
                  ?? throw new InvalidDataException("ไฟล์โปรเจกต์ว่างเปล่าหรืออ่านไม่ออก");

        if (dto.Version > CurrentVersion)
            throw new InvalidDataException(
                $"ไฟล์นี้บันทึกจากโปรแกรมรุ่นใหม่กว่า (เวอร์ชัน {dto.Version}) — อัปเดตโปรแกรมก่อน");

        var warnings = new List<string>();
        var doc = new CircuitDocument { Title = dto.Title ?? "untitled" };

        foreach (var p in dto.Parts ?? [])
        {
            var def = PartCatalog.Find(p.Key);
            if (def is null)
            {
                warnings.Add($"ไม่มีอุปกรณ์ '{p.Key}' ในคลังแล้ว — {p.Designator} ถูกข้ามไป");
                continue;
            }

            doc.Parts.Add(new PartInstance
            {
                Id = p.Id,
                Definition = def,
                Designator = p.Designator,
                Position = new GridPoint(p.X, p.Y),
                Rotation = (Rotation)p.Rotation,
                Value = p.Value,
                Locked = p.Locked ?? false,
            });
        }

        foreach (var w in dto.Wires ?? [])
            doc.Wires.Add(new Wire { Id = w.Id, A = new GridPoint(w.Ax, w.Ay), B = new GridPoint(w.Bx, w.By) });

        foreach (var l in dto.Labels ?? [])
            doc.Labels.Add(new NetLabel { Id = l.Id, Name = l.Name, At = new GridPoint(l.X, l.Y) });

        doc.ReseedIds();
        return (doc, warnings);
    }

    // ── on-disk shape ────────────────────────────────────────────────────

    private sealed class ProjectDto
    {
        public int Version { get; set; }
        public string? Title { get; set; }
        public List<PartDto>? Parts { get; set; }
        public List<WireDto>? Wires { get; set; }
        public List<LabelDto>? Labels { get; set; }
    }

    private sealed class PartDto
    {
        public string Id { get; set; } = "";
        public string Key { get; set; } = "";
        public string Designator { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
        public int Rotation { get; set; }
        public string? Value { get; set; }
        public bool? Locked { get; set; }
    }

    private sealed class WireDto
    {
        public string Id { get; set; } = "";
        public int Ax { get; set; }
        public int Ay { get; set; }
        public int Bx { get; set; }
        public int By { get; set; }
    }

    private sealed class LabelDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int X { get; set; }
        public int Y { get; set; }
    }
}
