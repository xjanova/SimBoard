using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using SimBoard.App.Controls;
using SimBoard.Document;
using Avalonia.Platform.Storage;
using SimBoard.Spice;

namespace SimBoard.App.Views.Screens;

/// <summary>
/// The working editor: pick a part, place it, wire it, press Play, get real numbers.
///
/// This is the screen the mock screens were pictures of. The library is the real
/// catalogue, the canvas edits a real <see cref="CircuitDocument"/>, and Play generates
/// a deck from what is actually on the sheet and hands it to ngspice — so every voltage
/// shown came out of the drawn circuit rather than a fixture.
/// </summary>
public static class EditorView
{
    public static Control Build()
    {
        var doc = SampleCircuit();
        var canvas = new SchematicCanvas { Document = doc };

        var props = new StackPanel { Spacing = 4 };
        var report = new StackPanel { Spacing = 3 };
        var status = new TextBlock { Classes = { "mono" }, Foreground = SymbolRenderer.Meta };

        void Refresh()
        {
            ShowProperties(props, canvas, Refresh);
            ShowRules(report, doc);
            var nets = doc.ExtractNets();
            status.Text = $"อุปกรณ์ {doc.Parts.Count} · สาย {doc.Wires.Count} · เนต {nets.Count}";
        }

        canvas.DocumentChanged += (_, _) => Refresh();
        canvas.SelectionChanged += (_, _) => Refresh();

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("208,*,272") };

        var left = LibraryPanel(canvas);
        Grid.SetColumn(left, 0);

        var centre = new DockPanel { LastChildFill = true, Margin = new Thickness(6, 0) };
        var bar = Toolbar(canvas, report, status);
        DockPanel.SetDock(bar, Dock.Top);
        centre.Children.Add(bar);
        var statusRow = new Bevel { Classes = { "flat" }, Padding = new Thickness(6, 2), Child = status };
        DockPanel.SetDock(statusRow, Dock.Bottom);
        centre.Children.Add(statusRow);
        centre.Children.Add(new Bevel { Classes = { "workspace" }, Child = canvas });
        Grid.SetColumn(centre, 1);

        var right = RightPanel(props, report);
        Grid.SetColumn(right, 2);

        grid.Children.Add(left);
        grid.Children.Add(centre);
        grid.Children.Add(right);

        canvas.AttachedToVisualTree += (_, _) => Refresh();   // the canvas fits itself on its first sized frame
        return grid;
    }

    /// <summary>A small circuit that already works, so the screen opens with something to run.</summary>
    private static CircuitDocument SampleCircuit()
    {
        var doc = new CircuitDocument { Title = "led-driver" };
        var v1 = doc.Place(PartCatalog.Require("VDC"), new GridPoint(2, 6));
        var r1 = doc.Place(PartCatalog.Require("R"), new GridPoint(10, 3));
        var led = doc.Place(PartCatalog.Require("LED"), new GridPoint(20, 3));
        var gnd = doc.Place(PartCatalog.Require("GND"), new GridPoint(10, 16));
        v1.Value = "5";
        r1.Value = "330";

        Wire(doc, v1, "1", r1, "1");
        Wire(doc, r1, "2", led, "1");
        Wire(doc, led, "2", gnd, "1");
        Wire(doc, v1, "2", gnd, "1");
        return doc;
    }

    private static void Wire(CircuitDocument doc, PartInstance a, string aPin, PartInstance b, string bPin)
    {
        var pinA = a.Definition.PinByNumber(aPin)!;
        var pinB = b.Definition.PinByNumber(bPin)!;
        var (pa, pb) = (a.PinAt(pinA), b.PinAt(pinB));
        var ea = Escape(pa, pinA.Side);
        var eb = Escape(pb, pinB.Side);
        var corner = pinA.Side is PinSide.Left or PinSide.Right
            ? new GridPoint(ea.X, eb.Y) : new GridPoint(eb.X, ea.Y);

        doc.Connect(pa, ea);
        doc.Connect(ea, corner);
        doc.Connect(corner, eb);
        doc.Connect(eb, pb);

        static GridPoint Escape(GridPoint p, PinSide s) => s switch
        {
            PinSide.Left => p.Offset(-2, 0),
            PinSide.Right => p.Offset(2, 0),
            PinSide.Top => p.Offset(0, -2),
            _ => p.Offset(0, 2),
        };
    }

    // ── panels ───────────────────────────────────────────────────────────

    private static Control LibraryPanel(SchematicCanvas canvas)
    {
        var list = new StackPanel { Spacing = 1 };

        foreach (var group in PartCatalog.All.GroupBy(p => p.Spice switch
        {
            SpiceKind.Behavioural => "โมดูล & เซนเซอร์",
            SpiceKind.Subcircuit => "ไอซี",
            SpiceKind.None => "สัญลักษณ์",
            _ => "อุปกรณ์พื้นฐาน",
        }))
        {
            list.Children.Add(new Bevel
            {
                Classes = { "caption" },
                Margin = new Thickness(0, 4, 0, 2),
                Child = new TextBlock { Text = group.Key },
            });

            foreach (var def in group)
            {
                var btn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(6, 2),
                    MinHeight = 0,
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = def.NameTh, FontSize = 10.5 },
                            new TextBlock
                            {
                                Classes = { "dense" },
                                Text = $"{def.Key} · {def.Pins.Count} ขา" +
                                       (def.IsSimulatable ? " · ซิมได้" : " · โมเดลพฤติกรรม"),
                                Foreground = new SolidColorBrush(Color.Parse("#6a6a6a")),
                            },
                        },
                    },
                };
                var captured = def;
                btn.Click += (_, _) =>
                {
                    canvas.PendingPart = captured;
                    canvas.Tool = EditorTool.Place;
                    canvas.Focus();
                };
                list.Children.Add(btn);
            }
        }

        return new Bevel
        {
            Classes = { "flat" },
            Padding = new Thickness(4),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    Docked(new Bevel
                    {
                        Classes = { "caption" },
                        Child = new TextBlock { Text = $"คลังอุปกรณ์ · {PartCatalog.All.Count} ตัว" },
                    }, Dock.Top),
                    new Bevel
                    {
                        Classes = { "sunken" },
                        Margin = new Thickness(0, 4, 0, 0),
                        Child = new ScrollViewer { Content = list },
                    },
                },
            },
        };
    }

    private static Control Toolbar(SchematicCanvas canvas, StackPanel report, TextBlock status)
    {
        var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        var buttons = new List<(Button B, EditorTool T)>();

        foreach (var (tool, glyph, tip) in new[]
                 {
                     (EditorTool.Select, "✛", "เลือก / ย้าย"),
                     (EditorTool.Wire, "⌁", "ต่อสาย"),
                     (EditorTool.Delete, "⌫", "ลบ"),
                 })
        {
            var b = new Button { Classes = { "tool" }, Content = glyph };
            ToolTip.SetTip(b, tip);
            var captured = tool;
            b.Click += (_, _) =>
            {
                canvas.Tool = captured;
                canvas.PendingPart = null;
                foreach (var (btn, t) in buttons)
                {
                    if (t == captured) { if (!btn.Classes.Contains("latched")) btn.Classes.Add("latched"); }
                    else btn.Classes.Remove("latched");
                }
                canvas.Focus();
            };
            buttons.Add((b, tool));
            tools.Children.Add(b);
        }
        buttons[0].B.Classes.Add("latched");

        var undo = new Button { Classes = { "tool" }, Content = "↶", Margin = new Thickness(8, 0, 0, 0) };
        ToolTip.SetTip(undo, "ย้อนกลับ (Ctrl+Z)");
        undo.Click += (_, _) => { canvas.History.Undo(); canvas.Focus(); canvas.InvalidateVisual(); };

        var redo = new Button { Classes = { "tool" }, Content = "↷" };
        ToolTip.SetTip(redo, "ทำซ้ำ (Ctrl+Y)");
        redo.Click += (_, _) => { canvas.History.Redo(); canvas.Focus(); canvas.InvalidateVisual(); };

        var open = new Button { Content = "เปิด", Margin = new Thickness(8, 0, 0, 0) };
        open.Click += async (_, _) => await OpenProject(canvas, report, status);

        var save = new Button { Content = "บันทึก" };
        save.Click += async (_, _) => await SaveProject(canvas, status);

        var play = new Button { Classes = { "default" }, Content = "▶ รันซิม", Margin = new Thickness(8, 0, 0, 0) };
        play.Click += async (_, _) =>
        {
            play.IsEnabled = false;
            status.Text = "กำลังจำลอง…";
            try { await Simulate(canvas, report, status); }
            finally { play.IsEnabled = true; }
        };

        var fit = new Button { Content = "พอดีจอ", Margin = new Thickness(4, 0, 0, 0) };
        fit.Click += (_, _) => canvas.ZoomToFit();

        tools.Children.Add(undo);
        tools.Children.Add(redo);
        tools.Children.Add(open);
        tools.Children.Add(save);
        tools.Children.Add(play);
        tools.Children.Add(fit);

        return new Bevel { Classes = { "flat" }, Padding = new Thickness(2, 3), Child = tools };
    }

    private static Control RightPanel(StackPanel props, StackPanel report) => new Bevel
    {
        Classes = { "flat" },
        Padding = new Thickness(4),
        Child = new DockPanel
        {
            LastChildFill = true,
            Children =
            {
                Docked(new Bevel { Classes = { "caption" }, Child = new TextBlock { Text = "คุณสมบัติ · PROPERTIES" } }, Dock.Top),
                Docked(new Bevel
                {
                    Classes = { "sunken" },
                    Height = 210,
                    Margin = new Thickness(0, 4),
                    Padding = new Thickness(6),
                    Child = new ScrollViewer { Content = props },
                }, Dock.Top),
                Docked(new Bevel { Classes = { "caption" }, Child = new TextBlock { Text = "ตรวจกฎ & ผลซิม" } }, Dock.Top),
                new Bevel
                {
                    Classes = { "sunken" },
                    Margin = new Thickness(0, 4, 0, 0),
                    Padding = new Thickness(6),
                    Child = new ScrollViewer { Content = report },
                },
            },
        },
    };

    // ── content ──────────────────────────────────────────────────────────

    private static async Task SaveProject(SchematicCanvas canvas, TextBlock status)
    {
        var top = TopLevel.GetTopLevel(canvas);
        if (top is null) return;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "บันทึกโปรเจกต์",
            SuggestedFileName = canvas.Document.Title + ProjectFile.Extension,
            DefaultExtension = ProjectFile.Extension.TrimStart('.'),
            FileTypeChoices = [new FilePickerFileType("SimBoard project") { Patterns = ["*" + ProjectFile.Extension] }],
        });
        if (file is null) return;

        try
        {
            ProjectFile.Save(canvas.Document, file.Path.LocalPath);
            status.Text = $"บันทึกแล้ว · {file.Name}";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            status.Text = "บันทึกไม่สำเร็จ — " + e.Message;
        }
    }

    private static async Task OpenProject(SchematicCanvas canvas, StackPanel report, TextBlock status)
    {
        var top = TopLevel.GetTopLevel(canvas);
        if (top is null) return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "เปิดโปรเจกต์",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("SimBoard project") { Patterns = ["*" + ProjectFile.Extension] }],
        });
        if (files.Count == 0) return;

        try
        {
            var (doc, warnings) = ProjectFile.Load(files[0].Path.LocalPath);
            canvas.Document = doc;
            status.Text = $"เปิดแล้ว · {files[0].Name} · อุปกรณ์ {doc.Parts.Count}";

            report.Children.Clear();
            foreach (var w in warnings)
                report.Children.Add(Warn(w, "#8a6420"));
        }
        catch (Exception e) when (e is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            status.Text = "เปิดไม่สำเร็จ";
            report.Children.Clear();
            report.Children.Add(Warn(e.Message, "#8a2b22"));
        }
    }

    private static void ShowProperties(StackPanel host, SchematicCanvas canvas, Action refresh)
    {
        var part = canvas.Selected;
        host.Children.Clear();
        if (part is null)
        {
            host.Children.Add(Dim("ยังไม่ได้เลือกอุปกรณ์ — คลิกที่ตัวใดตัวหนึ่ง"));
            return;
        }

        var d = part.Definition;
        host.Children.Add(new TextBlock { Text = $"{part.Designator}  {d.NameTh}", FontWeight = FontWeight.Bold });
        host.Children.Add(Row("ชนิด", d.Key));
        if (d.Mpn is { } mpn) host.Children.Add(Row("เบอร์", mpn));
        if (d.Package is { } pkg) host.Children.Add(Row("แพ็กเกจ", pkg));
        if (part.Value is not null)
        {
            // Editable, and routed through the undo stack like every other edit —
            // retyping a resistor value is the most common change there is.
            var field = new TextBox { Text = part.Value, FontSize = 10 };
            field.LostFocus += (_, _) => Commit();
            field.KeyDown += (_, ke) => { if (ke.Key == Avalonia.Input.Key.Enter) Commit(); };

            void Commit()
            {
                var typed = field.Text ?? "";
                if (typed == part.Value) return;
                canvas.History.Do(new SetValue(part.Id, part.Value, typed));
                canvas.InvalidateVisual();
                refresh();
            }

            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("76,*") };
            var lbl = new TextBlock { Text = "ค่า", FontSize = 10, Foreground = new SolidColorBrush(Color.Parse("#5a5a5a")) };
            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(field, 1);
            g.Children.Add(lbl);
            g.Children.Add(field);
            host.Children.Add(g);
        }
        host.Children.Add(Row("หมุน", $"{(int)part.Rotation}°"));
        host.Children.Add(Row("ตำแหน่ง", $"{part.Position.X * 2.54:0.0}, {part.Position.Y * 2.54:0.0} mm"));

        if (d.Digital is { } spec)
        {
            host.Children.Add(Head("ไฟเลี้ยง & ลอจิก"));
            host.Children.Add(Row("แรงดัน", $"{spec.VccMin:0.#}–{spec.VccMax:0.#} V (ปกติ {spec.VccTypical:0.#})"));
            host.Children.Add(Row("กระแส", $"{spec.Icc * 1000:0.##} mA"));
            if (spec.Bus != Bus.None) host.Children.Add(Row("บัส", spec.Bus.ToString()));
            if (spec.BusAddress is { } addr) host.Children.Add(Row("ที่อยู่", addr));
        }

        host.Children.Add(Head($"ขา ({d.Pins.Count})"));
        foreach (var pin in d.Pins)
            host.Children.Add(new TextBlock
            {
                Classes = { "dense" },
                Text = $"{pin.Number,3}  {pin.Name,-8} {pin.Kind}" + (pin.Description is { } dsc ? $" — {dsc}" : ""),
                TextWrapping = TextWrapping.Wrap,
            });

        if (d.NoteTh is { } note)
        {
            host.Children.Add(Head("ข้อควรระวัง"));
            host.Children.Add(new TextBlock
            {
                Text = note,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#8a2b22")),
            });
        }
    }

    private static void ShowRules(StackPanel host, CircuitDocument doc)
    {
        host.Children.Clear();
        var violations = ElectricalRuleCheck.Run(doc);

        if (violations.Count == 0)
        {
            host.Children.Add(new TextBlock
            {
                Text = "✓ ตรวจกฎผ่านหมด",
                Foreground = new SolidColorBrush(Color.Parse("#1c7a3e")),
                FontWeight = FontWeight.Bold,
            });
            return;
        }

        foreach (var x in violations.Take(14))
            host.Children.Add(new TextBlock
            {
                Text = $"{(x.Severity == RuleSeverity.Error ? "✕" : "!")} {x.Message}",
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse(
                    x.Severity == RuleSeverity.Error ? "#8a2b22" : "#8a6420")),
            });
    }

    private static async Task Simulate(SchematicCanvas canvas, StackPanel report, TextBlock status)
    {
        var doc = canvas.Document;
        var built = NetlistBuilder.Build(doc, Analysis.Transient(1e-6, 2e-3));
        report.Children.Clear();

        if (!built.CanSimulate)
        {
            foreach (var b in built.Blockers)
                report.Children.Add(Warn(b, "#8a2b22"));
            status.Text = "ซิมไม่ได้";
            return;
        }

        try
        {
            var result = await new NgspiceRunner().RunAsync(built.Deck);
            report.Children.Add(new TextBlock
            {
                Text = $"✓ ซิมเสร็จ · {result.PointCount:N0} จุด · {result.Elapsed.TotalMilliseconds:F0} ms",
                Foreground = new SolidColorBrush(Color.Parse("#1c7a3e")),
                FontWeight = FontWeight.Bold,
            });

            var volts = new Dictionary<string, double>();
            foreach (var net in built.Nets.Where(n => !n.IsGround))
            {
                var v = result[net.SpiceName];
                if (v is null || v.Count == 0) continue;
                volts[net.SpiceName] = v.Values[^1];
                report.Children.Add(new TextBlock
                {
                    Classes = { "mono" },
                    Text = $"{net.Name,-8} {v.Values[^1],8:0.000} V   " +
                           string.Join(", ", net.Connections.Take(3).Select(c => $"{c.Part.Designator}.{c.Pin.Name}")),
                });
            }

            // Put the answer on the sheet, not only in the panel.
            canvas.ShowResults(built.Nets, volts);

            foreach (var a in built.Approximations)
                report.Children.Add(Warn(a, "#8a6420"));

            status.Text = $"ซิมเสร็จ · {built.Nets.Count} เนต";
        }
        catch (SpiceException e)
        {
            report.Children.Add(Warn(e.Message, "#8a2b22"));
            if (e.Node is { } n) report.Children.Add(Warn($"โหนดที่มีปัญหา: {n}", "#8a2b22"));
            status.Text = "ซิมไม่ผ่าน";
        }
    }

    // ── small helpers ────────────────────────────────────────────────────

    private static Control Docked(Control c, Dock side) { DockPanel.SetDock(c, side); return c; }

    private static Control Row(string label, string value)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("76,*") };
        var l = new TextBlock { Text = label, FontSize = 10, Foreground = new SolidColorBrush(Color.Parse("#5a5a5a")) };
        var v = new TextBlock { Classes = { "mono" }, Text = value, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(l, 0);
        Grid.SetColumn(v, 1);
        g.Children.Add(l);
        g.Children.Add(v);
        return g;
    }

    private static Control Head(string text) => new TextBlock
    {
        Text = text,
        FontWeight = FontWeight.Bold,
        FontSize = 10,
        Margin = new Thickness(0, 6, 0, 2),
    };

    private static Control Warn(string text, string colour) => new TextBlock
    {
        Text = text,
        FontSize = 10,
        TextWrapping = TextWrapping.Wrap,
        Foreground = new SolidColorBrush(Color.Parse(colour)),
    };

    private static Control Dim(string text) => new TextBlock
    {
        Text = text,
        FontSize = 10,
        Foreground = new SolidColorBrush(Color.Parse("#8a8a8a")),
        TextWrapping = TextWrapping.Wrap,
    };
}
