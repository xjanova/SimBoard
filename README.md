# SimBoard

Professional electronics workbench: schematic capture → 2D breadboard → SPICE
simulation with virtual instruments → PCB layout with assisted place & route.

Full plan, stack rationale, phases and risks: **[PLAN.html](PLAN.html)**
Logo and brand: **[LOGO.html](LOGO.html)**, assets in `assets/`

## Status

Phase 0 — de-risking spikes. **S1 passed.**

## Getting the simulation engine

ngspice runs as a separate process and is not committed. Fetch it once:

```powershell
pwsh tools/fetch-ngspice.ps1
```

Or set `SIMBOARD_NGSPICE` to an existing `ngspice_con.exe`.

## Running the S1 spike

```
dotnet run --project spikes/S1.NgspiceSidecar
```

It checks, on real measurements rather than assertions about intent:
an RC step against its analytic solution, a transistor-level 555 astable
against the textbook oscillator formula, that Stop kills the engine and
leaves no orphan process, and that an unsolvable circuit surfaces as a
typed failure naming the offending node — never raw engine text.

## Layout

| Path | What |
|---|---|
| `src/SimBoard.Spice` | ngspice sidecar host, rawfile reader, instrument measurements |
| `spikes/` | Phase 0 risk spikes |
| `tests/` | unit tests |
| `_mockups/` | the original design handoff — source of truth for the UI spec |
| `tools/` | engine fetch script; `tools/Spice64` is downloaded, not committed |
