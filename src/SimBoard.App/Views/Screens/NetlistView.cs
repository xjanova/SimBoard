using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using SimBoard.App.Controls;
using SimBoard.Document;

namespace SimBoard.App.Views.Screens;

/// <summary>
/// Mode tab 4 — SPICE. Exactly what ngspice would be handed for the circuit that is on
/// the sheet right now, together with everything the builder had to compromise on to
/// produce it.
///
/// The netlist is the one artefact this program derives rather than estimates: every
/// line of the deck comes out of <see cref="CircuitDocument.ExtractNets"/> and the parts
/// actually placed, so nothing here is a guess. That is precisely why the compromises
/// have to be on screen. A deck that quietly swapped an ESP32 for a resistor and said
/// nothing would be the most convincing lie this app could tell, so
/// <see cref="NetlistResult.Approximations"/> and <see cref="NetlistResult.Blockers"/>
/// are rendered in full — no truncation, no folding into a count.
///
/// The corollary, and the rule the panel is written to: an empty warning list is not
/// evidence of anything. The builder reports what it noticed, and a part it had no
/// conversion for can fall out of the deck without a word — so the deck itself is read
/// back and reconciled against <see cref="CircuitDocument.Parts"/> here, and every
/// positive statement in the findings column is either that reconciliation or a
/// restatement of what the builder said, worded no wider than the check behind it.
/// </summary>
public static class NetlistView
{
    // Every user-facing string below is an inline Thai literal: no Keys entry exists for
    // any of them and the generated Keys.g.cs / Strings.g.cs are not hand-edited.
    // TODO: localise — move this screen's text into the generated string tables.

    /// <summary>Data face. Chrome.axaml carries the same list in the .mono class.</summary>
    private static readonly FontFamily Mono = new("Lucida Console, Consolas, monospace");

    // The severity palette EditorView.ShowRules already uses. Chrome has no token for
    // "this is an error" — the theme tokens describe surfaces, not findings — so these
    // are literals there and stay literals here. One presentation for rule findings
    // across the app is worth more than a second, tidier one that looks different.
    private const string ErrorInk = "#8a2b22";
    private const string WarnInk = "#8a6420";
    private const string PassInk = "#1c7a3e";
    private const string DimInk = "#8a8a8a";
    private const string LabelInk = "#5a5a5a";

    /// <summary>
    /// What the tab can ask the builder for. Transient is first, and default, because it
    /// is the exact card the editor's Play button runs — a SPICE tab showing a different
    /// deck from the one that gets simulated would be worse than showing none.
    /// </summary>
    private static readonly (string Label, Func<Analysis> Make)[] Analyses =
    [
        ("ทรานเซียนต์", () => Analysis.Transient(1e-6, 2e-3)),   // TODO: localise
        (".op จุดทำงาน", () => Analysis.OperatingPoint()),        // TODO: localise
        ("AC กวาดความถี่", () => Analysis.Ac(20, 10, 1e6)),      // TODO: localise
    ];

    public static Control Build()
    {
        // Chrome.axaml's text rules are exact-type selectors, so neither .mono nor the
        // base TextBlock foreground reaches a SelectableTextBlock. Both are set here to
        // the same values those rules carry. Selection colours are deliberately left to
        // the theme, which already styles this control.
        var deck = new SelectableTextBlock
        {
            FontFamily = Mono,
            FontSize = 10,
            Foreground = Ink("#1a1a1a"),
            TextWrapping = TextWrapping.NoWrap,
            Focusable = true,
        };
        ToolTip.SetTip(deck, "ลากเลือกแล้วกด Ctrl+C หรือใช้ปุ่มคัดลอกด้านบน");   // TODO: localise

        var netList = new StackPanel { Spacing = 5 };
        var netCaption = new TextBlock();
        var findings = new StackPanel { Spacing = 3 };
        var status = new TextBlock { Classes = { "mono" }, Margin = new Thickness(2, 4, 2, 0) };

        // Held so the copy and save buttons hand over the deck that is currently on
        // screen rather than rebuilding one and risking a different analysis card.
        string current = "";
        int analysis = 0;

        void Refresh()
        {
            var doc = Workspace.Document;
            var card = Analyses[analysis].Make();
            var built = NetlistBuilder.Build(doc, card);

            current = built.Deck;
            deck.Text = built.Deck;
            ShowNets(netList, netCaption, built.Nets);
            ShowFindings(findings, doc, built, card);

            status.Text = $"อุปกรณ์ {doc.Parts.Count} · เนต {built.Nets.Count} · {card.Description} · " +
                          $"เด็ค {LineCount(built.Deck)} บรรทัด";   // TODO: localise
        }

        var tools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Margin = new Thickness(0, 0, 0, 4),
        };
        var cards = new List<Button>();
        for (int i = 0; i < Analyses.Length; i++)
        {
            int index = i;
            var b = new Button { Content = Analyses[i].Label, FontSize = 10, Padding = new Thickness(8, 2) };
            b.Click += (_, _) =>
            {
                analysis = index;
                for (int k = 0; k < cards.Count; k++) Latch(cards[k], k == index);
                Refresh();
            };
            cards.Add(b);
            tools.Children.Add(b);
        }
        Latch(cards[0], true);

        var copy = new Button { Content = "คัดลอก", Margin = new Thickness(8, 0, 0, 0) };   // TODO: localise
        ToolTip.SetTip(copy, "คัดลอกเด็คทั้งก้อนไปคลิปบอร์ด");
        copy.Click += async (_, _) =>
        {
            if (TopLevel.GetTopLevel(copy)?.Clipboard is not { } clipboard) return;

            // async void, so a second click while the first is still awaiting would run
            // the whole body again with nothing joining the two. Latched off for the
            // duration instead.
            copy.IsEnabled = false;
            try { await clipboard.SetTextAsync(current); }
            finally { copy.IsEnabled = true; }

            Report(status, "คัดลอกเด็คไปคลิปบอร์ดแล้ว — วางลง ngspice ได้ทันที");   // TODO: localise
        };

        var save = new Button { Content = "บันทึก .cir" };   // TODO: localise
        save.Click += async (_, _) =>
        {
            save.IsEnabled = false;
            try { await SaveDeck(save, current, status); }
            finally { save.IsEnabled = true; }
        };

        tools.Children.Add(copy);
        tools.Children.Add(save);

        var left = new Bevel
        {
            Classes = { "flat" },
            Padding = new Thickness(4),
            Child = new DockPanel
            {
                LastChildFill = true,
                Children =
                {
                    Docked(tools, Dock.Top),
                    Docked(new Bevel
                    {
                        Classes = { "caption" },
                        Child = new TextBlock { Text = "เน็ตลิสต์ SPICE · เด็คที่ ngspice จะได้รับจริง" },
                    }, Dock.Top),
                    Docked(status, Dock.Bottom),
                    new Bevel
                    {
                        Classes = { "sunken" },
                        Margin = new Thickness(0, 4, 0, 0),
                        Padding = new Thickness(6),
                        Child = new ScrollViewer
                        {
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Content = deck,
                        },
                    },
                },
            },
        };
        Grid.SetColumn(left, 0);

        var rows = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto,*"), RowSpacing = 4 };
        var netHeader = new Bevel { Classes = { "caption" }, Child = netCaption };
        Grid.SetRow(netHeader, 0);
        var netWell = new Bevel
        {
            Classes = { "sunken" },
            Padding = new Thickness(6),
            Child = new ScrollViewer { Content = netList },
        };
        Grid.SetRow(netWell, 1);
        var findingHeader = new Bevel
        {
            Classes = { "caption" },
            Child = new TextBlock { Text = "คำเตือน & ตรวจกฎ" },   // TODO: localise
        };
        Grid.SetRow(findingHeader, 2);
        var findingWell = new Bevel
        {
            Classes = { "sunken" },
            Padding = new Thickness(6),
            Child = new ScrollViewer { Content = findings },
        };
        Grid.SetRow(findingWell, 3);
        rows.Children.Add(netHeader);
        rows.Children.Add(netWell);
        rows.Children.Add(findingHeader);
        rows.Children.Add(findingWell);

        var right = new Bevel
        {
            Classes = { "flat" },
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(4),
            Child = rows,
        };
        Grid.SetColumn(right, 1);

        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("*,312") };
        root.Children.Add(left);
        root.Children.Add(right);

        // Workspace.Subscribe rather than a hand-rolled Changed += / -= pair: MainWindow
        // rebuilds the workspace child on every mode-tab click and drops the old one, so a
        // handler on the static event has to come off with the screen. Subscribe owns
        // that, guards a re-attach that never detached, and runs the handler once on
        // attach — which is before the first layout pass, so the panel is populated on the
        // first frame without a second priming call. Each Refresh walks the union-find
        // twice (NetlistBuilder.Build, then ElectricalRuleCheck.Run), and opening the tab
        // used to pay for that twice over.
        _ = Workspace.Subscribe(root, (_, _) => Refresh());

        return root;
    }

    // ── content ──────────────────────────────────────────────────────────

    /// <summary>
    /// Every net, the node name it takes in the deck, and every pin sitting on it.
    /// SpiceName is what the deck actually contains, which is why it is shown next to
    /// the display name rather than instead of it — ground reads "GND" on the sheet and
    /// "0" in the file, and both facts matter when reading a run.
    /// </summary>
    private static void ShowNets(StackPanel host, TextBlock caption, IReadOnlyList<Net> nets)
    {
        host.Children.Clear();
        caption.Text = $"เนต · {nets.Count}";   // TODO: localise

        if (nets.Count == 0)
        {
            host.Children.Add(Dim("ยังไม่มีเนต — ต้องมีอุปกรณ์และสายที่เชื่อมถึงกันก่อน จึงจะเกิดโหนดขึ้นมา"));
            return;
        }

        foreach (var net in nets)
        {
            var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var name = new TextBlock { Classes = { "mono" }, Text = net.Name, FontWeight = FontWeight.Bold };
            var node = new TextBlock
            {
                Classes = { "mono" },
                Text = $"โหนด {net.SpiceName}",   // TODO: localise
                Foreground = Ink(net.IsGround ? PassInk : LabelInk),
            };
            Grid.SetColumn(name, 0);
            Grid.SetColumn(node, 1);
            head.Children.Add(name);
            head.Children.Add(node);
            host.Children.Add(head);

            // Unconditional: ExtractNets only returns groups that hold at least one pin,
            // so a pinless Net cannot reach this loop. An empty-case arm here would read
            // as a state the screen handles, when it is one the document model excludes.
            host.Children.Add(new TextBlock
            {
                Classes = { "dense" },
                Text = $"{net.PinCount} ขา: " +
                       string.Join(", ", net.Connections.Select(c => $"{c.Part.Designator}.{c.Pin.Name}")),
                TextWrapping = TextWrapping.Wrap,
                Foreground = Ink("#4a4a4a"),
            });
        }
    }

    /// <summary>
    /// Separately-labelled lists, in the order that decides whether a reader can trust
    /// the deck: what stopped the builder, what the run still needs from outside, where
    /// the deck stopped matching the sheet, what was approximated, and what the rule
    /// check found. None is merged into another and none is truncated — EditorView caps
    /// its findings at 14 because its panel is 272 px of a working editor; this column
    /// exists to show them all.
    ///
    /// The one thing this method may never do is read a claim out of an empty list. Two
    /// of the lists it shows are produced here, by reading the finished deck back, and
    /// the wording of every other line is held down to the question that was actually
    /// asked — the builder reporting nothing is a fact about the builder, not about the
    /// circuit.
    /// </summary>
    private static void ShowFindings(StackPanel host, CircuitDocument doc, NetlistResult built, Analysis card)
    {
        host.Children.Clear();

        var violations = ElectricalRuleCheck.Run(doc);
        var missing = PartsMissingFromDeck(doc, built.Deck);
        var needed = ExternalFiles(built.Deck);
        var unnamed = doc.Parts
            .Where(p => p.Definition.Spice == SpiceKind.Subcircuit && p.Definition.SpiceModel is null)
            .ToList();
        var undefined = UndefinedSubcircuits(built.Deck);

        // CanSimulate is Blockers.Count == 0, and the builder raises exactly two blockers:
        // no ground, and no parts. It is not a verdict on the deck. An .include ngspice
        // cannot resolve aborts the run just as dead, and that is checked below rather
        // than here — so this line names the question that was answered instead of
        // promising an outcome nobody tested.
        if (built.CanSimulate)
        {
            // The tick keeps its green only while nothing else in this column will stop
            // the run. At a glance the colour is the whole message, and a bold green one
            // over a deck that aborts on its first .include is the same lie moved a line
            // down.
            host.Children.Add(needed.Count == 0 && unnamed.Count == 0
                              && undefined.Count == 0 && missing.Count == 0
                ? Good("✓ ตัวสร้างเด็คไม่พบตัวขวาง")
                : Line("! ตัวสร้างเด็คไม่พบตัวขวาง แต่ยังมีของที่ต้องหามาเองก่อนรัน — ดูข้างล่าง", WarnInk));
            host.Children.Add(Dim(
                "ที่ตรวจมีสองข้อเท่านั้น: ผังมีจุดกราวด์ และมีอุปกรณ์อยู่อย่างน้อยหนึ่งตัว " +
                "ยังไม่ได้ลองรันจริง และยังไม่ได้แปลว่า ngspice จะรับเด็คนี้ — อ่านรายการที่เหลือในคอลัมน์นี้ประกอบ"));
        }
        else
        {
            host.Children.Add(Head($"ซิมไม่ได้ · {built.Blockers.Count} ข้อ"));   // TODO: localise
            foreach (var b in built.Blockers)
                host.Children.Add(Line($"✕ {b}", ErrorInk));
        }

        // An empty sheet is not a failure state and must not look like one: the deck
        // beside this is complete and valid, it simply has nothing in it. Saying so is
        // the difference between "the tab is broken" and "you have not drawn anything".
        if (doc.Parts.Count == 0)
            host.Children.Add(Dim(
                "ผังยังว่าง เด็คด้านซ้ายจึงมีแค่หัวเรื่อง คำสั่งวิเคราะห์ และ .end — " +
                "นั่นคือทุกอย่างที่ SPICE จะได้รับตอนนี้ ไม่มีอะไรถูกซ่อนไว้ " +
                "วาดวงจรในแท็บผังวงจรแล้วกลับมาดูใหม่ เด็คจะอัปเดตตาม"));

        // An .ac card over a deck with no AC magnitude anywhere solves to zero at every
        // node — a run that finishes, plots a flat line, and is worth nothing. The
        // builder writes "DC <value>" for a plain magnitude and the value verbatim
        // otherwise, so an AC drive only exists if the user typed one into a source.
        if (IsAcSweep(card) && doc.Parts.Count > 0 && !HasAcDrive(built.Deck))
            host.Children.Add(Line(
                "! ไม่มีแหล่งจ่ายตัวไหนในเด็คนี้ระบุขนาดสัญญาณ AC ไว้ — .ac จะแก้ออกมาเป็นศูนย์ทุกโหนด " +
                "ถ้าจะกวาดความถี่จริง ให้ใส่ค่าแหล่งจ่ายเป็น AC เช่น \"DC 0 AC 1\" ในช่องค่าของแหล่งจ่าย",
                WarnInk));   // TODO: localise

        // Model cards for discrete parts are written inline into the deck, but a .subckt
        // part emits ".include <name>.lib" and no .lib file ships with this program.
        // NgspiceRunner runs the deck in a freshly created empty temp directory, so an
        // unresolved include is not a warning — the engine stops on it.
        if (needed.Count > 0 || unnamed.Count > 0 || undefined.Count > 0)
        {
            host.Children.Add(Head(
                $"ต้องหามาเองก่อนรัน · {needed.Count + unnamed.Count + undefined.Count} ข้อ"));   // TODO: localise
            host.Children.Add(Dim(
                "โปรแกรมนี้ไม่ได้แถมไฟล์ไลบรารีมาให้ และตัวรันซิมทำงานในโฟลเดอร์ชั่วคราวที่ว่างเปล่า " +
                "ไฟล์ข้างล่างนี้จึงต้องอยู่ในที่ที่ ngspice หาเจอ ไม่งั้นมันจะหยุดทันทีที่อ่านบรรทัด .include"));
            foreach (var f in needed)
                host.Children.Add(Line($"! {f} — เด็คสั่ง .include ไฟล์นี้ แต่ไม่ได้มากับโปรแกรม", WarnInk));
            foreach (var p in unnamed)
                host.Children.Add(Line(
                    $"✕ {p.Designator} ({p.Definition.Key}) ไม่มีชื่อ .subckt กำกับไว้ — บรรทัด X ของมันจึงไม่ครบ",
                    ErrorInk));
            foreach (var (designator, model) in undefined)
                host.Children.Add(Line(
                    $"✕ {designator} เรียก .subckt \"{model}\" ที่ไม่มีตัวจริงอยู่ในเด็คนี้ " +
                    "และเด็คก็ไม่ได้ .include ไฟล์ไหนไว้ — ngspice จะหยุดตรงบรรทัดนี้",
                    ErrorInk));   // TODO: localise
        }

        // The reconciliation the honesty of this whole tab rests on. Everything above is
        // something the builder said; this is the deck read back and matched against what
        // is on the sheet, which is the only way a part that fell out without a word
        // (a Behavioural part with no DigitalSpec emits nothing and reports nothing)
        // ever becomes visible.
        if (doc.Parts.Count > 0)
        {
            int symbolic = doc.Parts.Count(p => p.Definition.Spice == SpiceKind.None);
            int expected = doc.Parts.Count - symbolic;

            host.Children.Add(Head($"ไม่ได้อยู่ในเด็ค · {missing.Count} ข้อ"));   // TODO: localise
            if (missing.Count == 0)
            {
                host.Children.Add(Dim(
                    $"เทียบชื่ออิลิเมนต์ทุกบรรทัดในเด็คกับดีซิกเนเตอร์ทีละตัวแล้ว: อุปกรณ์ {expected} ตัวที่ต้องมีอิลิเมนต์ " +
                    "มีอยู่ในเด็คครบ" + (symbolic > 0
                        ? $" อีก {symbolic} ตัวเป็นสัญลักษณ์ที่ไม่มีอิลิเมนต์ของตัวเองอยู่แล้ว (กราวด์ คอนเนกเตอร์)"
                        : "")));
            }
            else
            {
                host.Children.Add(Dim(
                    "ไม่มีบรรทัดไหนในเด็คอ้างถึงอุปกรณ์พวกนี้เลย — มันหายไปจากการซิม ไม่ใช่แค่ถูกประมาณค่า " +
                    "ผลที่ออกมาคือผลของวงจรที่ขาดมันไป บางตัวอาจมีเหตุผลอยู่ในรายการค่าประมาณข้างล่าง บางตัวไม่มี"));
                foreach (var p in missing)
                    host.Children.Add(Line($"✕ {p.Designator} ({p.Definition.Key}) — {p.Definition.NameTh}", ErrorInk));
            }
        }

        host.Children.Add(Head($"ค่าประมาณ · {built.Approximations.Count} ข้อ"));   // TODO: localise
        if (built.Approximations.Count == 0)
        {
            // Weakened deliberately. The old wording here claimed every part converted
            // directly, which nothing had checked — it was read off the length of this
            // list. What is true is only that the builder did not report anything.
            host.Children.Add(Dim(
                "ตัวสร้างเด็คไม่ได้รายงานค่าประมาณไว้ นี่คือคำบอกจากตัวสร้างเอง " +
                "ไม่ใช่หลักฐานว่าทุกตัวแปลงตรง ๆ ส่วนที่พิสูจน์กับผังจริงคือหัวข้อ \"ไม่ได้อยู่ในเด็ค\" ด้านบน"));
        }
        else
        {
            // The list mixes two very different things: a part modelled as its electrical
            // envelope, and a part the builder had no conversion for and left out of the
            // deck entirely. The second kind is the one that silently changes an answer,
            // so the heading says both out loud instead of letting it read as a footnote.
            host.Children.Add(Dim(
                "SPICE รันเฟิร์มแวร์ไม่ได้ ตัวดิจิทัลจึงถูกแทนด้วยกรอบทางไฟฟ้าของมัน " +
                "และบางรายการหมายถึงอุปกรณ์ที่แปลงไม่ได้เลย — ถูกข้ามไป ไม่ได้อยู่ในเด็คนี้"));
            foreach (var a in built.Approximations)
                host.Children.Add(Line($"! {a}", WarnInk));
        }

        // ERC001 and the builder's ground blocker fire on the same condition — no net
        // carries a ground pin — so printing both puts one fault in this column twice, in
        // two wordings, and leaves the reader counting it as two. The blocker list is what
        // CanSimulate is derived from and stays whole; the duplicate is dropped here,
        // keyed on the rule code rather than on matching the prose.
        bool groundShownAbove = !built.CanSimulate && !built.Nets.Any(n => n.IsGround);
        IReadOnlyList<RuleViolation> shown = groundShownAbove
            ? violations.Where(x => x.Code != "ERC001").ToList()
            : violations;

        host.Children.Add(Head($"ตรวจกฎ (ERC) · {shown.Count} ข้อ"));   // TODO: localise
        if (shown.Count == 0)
        {
            // "All rules pass" would be false while a ground error is standing above.
            host.Children.Add(groundShownAbove
                ? Dim("นอกจากเรื่องกราวด์ที่รายงานไว้ด้านบนแล้ว ไม่พบข้ออื่น")
                : Good("✓ ตรวจกฎผ่านหมด"));
            return;
        }

        if (groundShownAbove)
            host.Children.Add(Dim("เรื่องกราวด์รายงานไว้ในหัวข้อ \"ซิมไม่ได้\" ด้านบนแล้ว จึงไม่ซ้ำที่นี่"));

        foreach (var x in shown)
            host.Children.Add(Line(
                $"{(x.Severity == RuleSeverity.Error ? "✕" : "!")} {x.Message}",
                x.Severity == RuleSeverity.Error ? ErrorInk : WarnInk));
    }

    // ── reading the finished deck back ───────────────────────────────────

    /// <summary>
    /// Parts the sheet carries that no line of the deck names.
    ///
    /// Reconciliation rather than trust: <see cref="NetlistBuilder"/> can emit nothing for
    /// a part and add nothing to <see cref="NetlistResult.Approximations"/> on the same
    /// path — a <see cref="SpiceKind.Behavioural"/> part whose definition has no
    /// <see cref="PartDefinition.Digital"/> spec returns early and is simply gone. The
    /// only witness to that is the deck itself.
    ///
    /// <see cref="SpiceKind.None"/> parts are excluded: a ground symbol or a connector is
    /// meant to have no element of its own, so listing it as absent would invent a fault.
    /// </summary>
    private static List<PartInstance> PartsMissingFromDeck(CircuitDocument doc, string deck)
    {
        var emitted = ElementLines(deck).Select(t => t[0]).ToList();
        return
        [
            .. doc.Parts
                .Where(p => p.Definition.Spice != SpiceKind.None)
                .Where(p => !emitted.Any(name => NamesPart(name, p.Designator)))
        ];
    }

    /// <summary>
    /// Whether a SPICE element name was built from this designator. The builder makes
    /// three shapes: the designator alone (R1), the designator behind a type letter it
    /// had to force on (RSW1, XU1), and either of those with a suffix it appends for a
    /// second element of the same part (R1_B for a potentiometer's lower half, RU1_ICC
    /// for a digital part's supply load). Requiring the boundary after the designator to
    /// be end-of-name or '_' is what keeps R1 from matching R12.
    /// </summary>
    private static bool NamesPart(string elementName, string designator)
    {
        var body = elementName;
        if (body.Length > 0 && !body.StartsWith(designator, StringComparison.OrdinalIgnoreCase))
            body = body[1..];

        return body.StartsWith(designator, StringComparison.OrdinalIgnoreCase)
            && (body.Length == designator.Length || body[designator.Length] == '_');
    }

    /// <summary>Every file the deck tells ngspice to pull in, in deck order.</summary>
    /// <summary>
    /// X calls in the deck whose subcircuit is defined in no <c>.subckt</c> block and
    /// reachable through no <c>.include</c>.
    ///
    /// Read off the deck rather than off the catalogue, because the deck is what ngspice
    /// is handed: an imported file that carried its own .subckt headers resolves here and
    /// a catalogue part with no library does not, which is exactly the distinction that
    /// decides whether the run starts. Where the deck does include a file, the call is
    /// left alone — this cannot see inside a file it does not ship, and reporting a
    /// definition as missing on a guess would be its own fabrication.
    /// </summary>
    private static List<(string Designator, string Model)> UndefinedSubcircuits(string deck)
    {
        if (ExternalFiles(deck).Count > 0) return [];

        var defined = DeckLines(deck)
            .Where(l => l.StartsWith(".subckt ", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(t => t.Length >= 2)
            .Select(t => t[1])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return
        [
            .. ElementLines(deck)
                .Where(t => t.Length >= 3 && t[0][0] is 'X' or 'x')
                .Select(t => (Designator: t[0], Model: t[^1]))
                .Where(x => !defined.Contains(x.Model))
        ];
    }

    private static List<string> ExternalFiles(string deck) =>
    [
        .. DeckLines(deck)
            .Where(l => l.StartsWith(".include ", StringComparison.OrdinalIgnoreCase))
            .Select(l => l[".include ".Length..].Trim())
            .Where(f => f.Length > 0)
    ];

    /// <summary>
    /// True when some source in the deck carries an AC magnitude.
    ///
    /// Read off the deck rather than off the part values, because the deck is the thing
    /// ngspice is handed. Only tokens past the element name and its two nodes are
    /// considered, so a net a user happened to label "AC" cannot pass for a drive.
    /// </summary>
    private static bool HasAcDrive(string deck) =>
        ElementLines(deck).Any(t =>
            t.Length > 3
            && t[0][0] is 'V' or 'v' or 'I' or 'i'
            && t.Skip(3).Any(x => x.Equals("AC", StringComparison.OrdinalIgnoreCase)));

    private static IEnumerable<string> DeckLines(string deck) =>
        deck.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);

    /// <summary>Element lines only — comments and dot cards carry no element name.</summary>
    private static IEnumerable<string[]> ElementLines(string deck) =>
        DeckLines(deck)
            .Where(l => l[0] is not ('*' or '.'))
            .Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(t => t.Length > 0);

    /// <summary>Whether the card in the deck is the frequency sweep, read from the card itself.</summary>
    private static bool IsAcSweep(Analysis card) =>
        card.Card.StartsWith(".ac", StringComparison.OrdinalIgnoreCase);

    private static async Task SaveDeck(Control anchor, string deck, TextBlock status)
    {
        var top = TopLevel.GetTopLevel(anchor);
        if (top is null) return;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "บันทึกเน็ตลิสต์",   // TODO: localise
            SuggestedFileName = DeckFileName(Workspace.Document.Title),
            DefaultExtension = "cir",
            FileTypeChoices = [new FilePickerFileType("SPICE netlist") { Patterns = ["*.cir", "*.net"] }],
        });
        if (file is null) return;

        string message;
        try
        {
            // Through the IStorageFile the picker returned, not its LocalPath: a provider
            // is free to hand back a location that is not a local filesystem path, and
            // writing to LocalPath then either lands somewhere else or throws a type this
            // method could not sensibly enumerate.
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new StreamWriter(stream);
            await writer.WriteAsync(deck);
            message = $"บันทึกแล้ว · {file.Name}";   // TODO: localise
        }
        catch (Exception e)
        {
            // Deliberately total. The caller is an async void click handler, so anything
            // that escapes here is an unhandled exception on the UI thread rather than a
            // failed save — and a failed save is a status line, not a crash.
            message = "บันทึกไม่สำเร็จ — " + e.Message;   // TODO: localise
        }

        Report(status, message);
    }

    /// <summary>
    /// A save-dialog suggestion built from a free-text project title. The title is
    /// whatever the user typed, so characters the OS forbids in a name are dropped rather
    /// than handed to the picker, and the extension is added only when it is not already
    /// there — a project called "led.cir" must not be suggested as "led.cir.cir".
    /// </summary>
    private static string DeckFileName(string title)
    {
        var forbidden = System.IO.Path.GetInvalidFileNameChars();
        string kept = new([.. title.Where(c => Array.IndexOf(forbidden, c) < 0)]);
        var stem = kept.Trim();

        if (stem.Length == 0) stem = "netlist";
        return stem.EndsWith(".cir", StringComparison.OrdinalIgnoreCase) ? stem : stem + ".cir";
    }

    /// <summary>
    /// Writes to the screen's status line, unless the screen is no longer on screen.
    ///
    /// The save picker can stay open for minutes, and MainWindow replaces the workspace
    /// child outright on a mode-tab click. A detached TextBlock accepts a write and shows
    /// it to nobody, which turns a failed save into silence. Nothing here can reach the
    /// window's own status bar — this screen does not own it — so when the message has
    /// nowhere to land it is dropped knowingly rather than by accident.
    /// </summary>
    private static void Report(TextBlock status, string message)
    {
        if (TopLevel.GetTopLevel(status) is not null) status.Text = message;
    }

    // ── small helpers ────────────────────────────────────────────────────

    private static Control Docked(Control c, Dock side) { DockPanel.SetDock(c, side); return c; }

    private static void Latch(Button b, bool on)
    {
        if (on) { if (!b.Classes.Contains("latched")) b.Classes.Add("latched"); }
        else b.Classes.Remove("latched");
    }

    /// <summary>Lines the deck actually occupies, counted — never rounded or estimated.</summary>
    private static int LineCount(string deck) => deck.Count(c => c == '\n');

    private static SolidColorBrush Ink(string hex) => new(Color.Parse(hex));

    private static Control Head(string text) => new TextBlock
    {
        Text = text,
        FontWeight = FontWeight.Bold,
        FontSize = 10,
        Margin = new Thickness(0, 8, 0, 2),
    };

    private static Control Line(string text, string colour) => new TextBlock
    {
        Text = text,
        FontSize = 10,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Ink(colour),
    };

    private static Control Good(string text) => new TextBlock
    {
        Text = text,
        Foreground = Ink(PassInk),
        FontWeight = FontWeight.Bold,
        TextWrapping = TextWrapping.Wrap,
    };

    private static Control Dim(string text) => new TextBlock
    {
        Text = text,
        FontSize = 10,
        Foreground = Ink(DimInk),
        TextWrapping = TextWrapping.Wrap,
    };
}
