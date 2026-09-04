# SimBoard

A professional electronics workbench for Windows: schematic capture → 2D breadboard
prototyping → SPICE simulation with virtual instruments → PCB layout with assisted
place & route. Built for repair technicians, R&D engineers, instructors and vocational
students — not a learning toy.

The interface is Thai/English, switchable live, wrapped in seven interchangeable retro
OS chrome themes. The circuit workspace stays dark under all of them so traces and
measured values keep their contrast.

**Plan, stack rationale, phases and risks: [PLAN.html](PLAN.html)** ·
brand and assets: [LOGO.html](LOGO.html)

---

## Status

The full UI shell is built and every screen in the design handoff renders and is
reachable. Nothing behind them is wired to real data yet — the screens carry the
spec's own fixture values.

| | |
|---|---|
| ✅ ngspice runs as a sidecar, verified against theory | `src/SimBoard.Spice` |
| ✅ Part cross-reference computed from parameters | `src/SimBoard.Parts` |
| ✅ Shell, 7 themes, TH/EN, retro control library | `src/SimBoard.App` |
| ✅ All 10 screens render and navigate | `Views/Screens`, `Views/Dialogs` |
| ⬜ Document model, live netlist, real editing | — |

## Running it

Fetch the simulation engine once (ngspice is a separate process and is not committed):

```powershell
pwsh tools/fetch-ngspice.ps1
```

Then:

```bash
dotnet run --project src/SimBoard.App
```

Any screen can be opened directly, which is also how the screenshot checks run:

```bash
dotnet run --project src/SimBoard.App -- --open pcb
```

`start · schematic · library · breadboard · sim · instruments · import · pcb · layers · settings`

**Ctrl+Shift+L** switches language · **เครื่องมือ** menu opens Preferences, where the seven
theme cards each render live chrome in that theme's own tokens.

## Part cross-reference

Substitution is computed from the parameters that decide whether a swap survives, so it
can say *why* a part fits and warn about what it trades away:

```bash
dotnet run --project tools/SimBoard.Xref -- 2N3904
```

```
✓ 1. PN2222A   TO-92  EBC   (drop-in)
✓ 2. 2N4401    TO-92  EBC   · fT slightly lower, no effect in general use
! 4. BC337     TO-92  CBE   ! different pinout — bend the legs or it dies on power-up
```

> ⚠️ The seeded figures are **not yet checked against datasheets**, and pinout in
> particular varies between manufacturers of the same part number. The tool says so
> before it says anything else. Use it to narrow the field, not to decide.

## The SPICE engine

ngspice runs as a **child process**, not a linked library. A circuit that hangs the
solver cannot take unsaved work with it, Stop is a real kill rather than a cooperative
flag, and the licence boundary stays clean. The cost is one process start per run — not
per sample: the netlist goes in once and the whole rawfile comes back once.

```bash
dotnet run --project spikes/S1.NgspiceSidecar
```

checks, on measurement rather than assertion: an RC step against its analytic solution,
a transistor-level 555 astable against the textbook oscillator formula, that Stop leaves
no orphan process, and that an unsolvable circuit surfaces as a typed failure naming the
offending node — never raw engine text.

## Layout

| Path | What |
|---|---|
| `src/SimBoard.App` | Avalonia UI — shell, themes, controls, all screens |
| `src/SimBoard.Spice` | ngspice sidecar host, rawfile reader, instrument measurements |
| `src/SimBoard.Document` | circuit model, net extraction, catalogue, netlist, ERC, import |
| `tools/SimBoard.Xref` | cross-reference CLI |
| `spikes/` | Phase 0 risk spikes |
| `_mockups/` | the original design handoff — source of truth for the UI spec |

Themes and UI strings are **generated**, not hand-written: `.build/extract_design.py`
pulls the 7×31 token sets and 125 bilingual strings out of the prototype, and
`gen_themes.py` / `gen_i18n.py` emit the Avalonia resources and the string table. A
table that size drifts from the spec the moment someone retypes it.

## Built with

C# 13 · .NET 10 · Avalonia 11 · SkiaSharp · ngspice 47

Avalonia over WPF even though this is Windows-first: the schematic and PCB canvas is the
hot path, and Avalonia hands a custom draw operation the SKCanvas directly where WPF goes
through a per-frame bitmap blit.

## Licence

MIT — see [LICENSE](LICENSE). Use it, fork it, ship it.

The simulation engine is deliberately **not** covered by that, because it is not part of
this repository. ngspice is fetched at setup time and runs as a separate process; nothing
here links against it, so its licence terms never reach into this code and this licence
never claims anything about it. That process boundary was chosen for crash isolation and a
working Stop button first — the clean licence line is what it also buys.

Xyce is a plausible future second engine and is GPLv3. If it is ever added it must stay
behind the same sidecar boundary, for the same reason.

Part figures in the catalogue are `Provenance.Unverified` — typed from general knowledge,
not transcribed from datasheets, and not yet checked against them. The UI says so before
it shows you a number. Do not treat them as authoritative for a build that matters.
