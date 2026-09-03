# Handoff: ElectroBench ME — Electronics Simulation Workbench

> ภาษาไทยอยู่ท้ายไฟล์ (ดู **ภาคผนวก ก · สรุปภาษาไทย**)

## Overview

ElectroBench ME is a professional electronics workbench application: schematic capture → 2D breadboard prototyping → SPICE simulation with virtual instruments → PCB layout with AI-assisted auto-placement and routing. It targets professional repair technicians, circuit design/R&D engineers, electronics instructors, vocational students (ปวช./ปวส.), and serious makers — **not** a toy or children's learning app. The UI is dense and information-rich but organised into a stable three-pane shell so nothing is more than one click away.

The visual identity is deliberately retro OS chrome (Windows ME by default) wrapped around a **modern dark EDA workspace**. Seven interchangeable chrome themes ship with the product, selectable at runtime. The entire interface is bilingual Thai/English, switchable live without restart.

---

## About the Design Files

The files in `design/` are **design references created in HTML** — a prototype that shows intended look, layout, and information architecture. They are **not production code to copy directly.**

`design/ElectroBench ME.dc.html` is a single-file HTML prototype using a small runtime (`support.js`) that renders an inline template plus a logic class. It uses **inline styles only** and CSS custom properties for theming. Do not port that runtime; it exists so the design could stream into a preview tool.

**Your task:** recreate these designs in the target codebase's existing environment, using its established patterns and component libraries. If no environment exists yet, choose the framework best suited to the product. Given the requirements (canvas-heavy editing, real-time SPICE simulation, multi-window instruments, native file I/O for Gerber/netlist imports, and 1000+ part libraries), a sensible stack would be:

- **Desktop shell**: Electron or Tauri (Tauri preferred for binary size and native file dialogs)
- **UI layer**: React + TypeScript
- **Canvas/editor**: a dedicated 2D scene layer — PixiJS or a hand-rolled Canvas2D/WebGL renderer, **not** DOM nodes per component (a schematic can hold thousands of primitives)
- **Simulation core**: ngspice/Xyce compiled to WASM, or a native sidecar process communicating over IPC
- **Chrome theming**: CSS custom properties, exactly as the prototype demonstrates

The retro chrome is a **skin over standard controls**. Build a real component library (Button, Panel, TitleBar, ListView, TabStrip, ComboBox, Checkbox, Radio, Slider, StatusBar) whose styling is driven entirely by the theme token set in *Design Tokens* below. Do not hard-code Windows ME colours into components.

## Fidelity

**High fidelity (hifi).** Colours, typography, spacing, bevel construction, and copy are final and should be reproduced precisely. Exact hex values, border constructions, and pixel dimensions are listed throughout. The one intentional exception: **component icons in the library grid are placeholders** (monospace glyphs such as `555`, `Q`, `▶`, `☀`). Real vector part symbols must be commissioned or sourced; the layout reserves a 58px-tall dark tile per part for them.

---

## Global Layout & Shell

Design canvas: **1480 × 920 px** for the application window (a desktop app; the layout is not responsive down to mobile). Minimum supported window: 1280 × 800. Above the window in the prototype sits a **black mockup navigator bar** (44px, dark `#1b1d21`) with numbered screen buttons and theme/language toggles — that bar is a **prototype affordance only. Do not build it.** Screens 7, 9, and 10 are modal dialogs over the main window, not separate routes.

### Shell structure (screens 2–10)

```
┌──────────────────────────────────────────────────── 1480 ───┐
│ TitleBar                                              22px │
│ MenuBar                                                20px │
│ Toolbar                                                30px │
├─────────┬───────────────────────────────────┬───────────────┤
│ Library │  Mode tabs (22px)                 │ Layers  278px │
│  panel  │  ───────────────────────────────  │  panel        │
│  236px  │  Workspace (dark, flex:1)         │ Properties    │
│         │                                   │ Sim result    │
├─────────┴───────────────────────────────────┴───────────────┤
│ StatusBar                                              22px │
└─────────────────────────────────────────────────────────────┘
```

- Left panel `236px` fixed, right panel `278px` fixed, centre `flex:1; min-width:0`.
- Panel separators: `border-right/left: 1px solid var(--shad)` plus `box-shadow: 1px 0 0 var(--lite) inset` — the classic two-tone divider.
- Workspace inset `margin: 0 6px 4px`, sunken border, background `#12161b`.
- The three panels are resizable by drag in the real product (not shown in mocks); persist widths per project.

### TitleBar (22px)

- Background `var(--tb)`; text `var(--tbFg)`, `700 11px Tahoma`, left-aligned after a 14×14 app icon.
- Title format: `<filename> — Sheet <n>` + ` — ElectroBench ME`, e.g. `555-astable-reg.ebp — แผ่นที่ 1 — ElectroBench ME`.
- Window controls right-aligned, `gap: 2px`: minimise 16×14, maximise 16×14, close 16×14. Geometry and colour come from `--minBg/--maxBg/--closeBg`, `--ctlRad`, `--ctlFg`. In macOS/Aqua themes these become 50%-radius traffic lights with no glyph (`--ctlFg: transparent`).

### MenuBar (20px)

Items, `11px`, padding `0 9px`, in order:
`ไฟล์ / File` · `แก้ไข / Edit` · `มุมมอง / View` · `วางอุปกรณ์ / Place` · `ซิมูเลต / Simulate` · `เครื่องมือ / Tools` · `บอร์ด / Board` · `หน้าต่าง / Window` · `ช่วยเหลือ / Help`
Bottom border `1px solid var(--shad)`.

### Toolbar (30px)

Four groups separated by `1px solid var(--shad)` + `1px 0 0 var(--lite)` inset dividers, `padding-right:5px; margin-right:3px`:

1. **File**: new (white page glyph), open (amber folder), save — buttons `23 × 22`.
2. **Tools** — 9 buttons `23 × 22`, glyphs `✛ ⌁ ⊹ ⌗ ⎋ ⧉ ⌖ T ⟂` = select, wire, junction, bus, net-label, place-part, probe, text, dimension. Active tool renders pressed (`--press` background, `--sunkBc` border); index 1 (wire) is active in all mocks.
3. **Simulation transport**: `▶ เล่น/Play` (pressed while running), `⏸ พัก/Pause`, `■ หยุด/Stop`, `ทีละสเต็ป/Step`. Play triangle is a CSS border triangle, `#1c7a3e`. Stop square `8×8`, `#8a2b22`.
4. **Grid + Zoom** combos: labels `กริด/Grid`, `ย่อ/ขยาย/Zoom`; sunken fields, `10px 'Lucida Console'` values `2.54 mm` and `160%`, with a `15 × 17` dropdown stub (`▼`, 7px).
- Right end: engine badge — pressed pill, `10px 'Lucida Console'`, green dot `7px` `#1c7a3e` with `box-shadow: 0 0 4px #34d17a`, text `SPICE3f5`.

### Mode tabs (22px, above workspace)

`ผังวงจร/Schematic` · `บอร์ดทดลอง 2D/Breadboard 2D` · `ลายปริ๊น/PCB` · `เนตลิสต์/Netlist`.
Active tab: `--face` background, border `var(--lite) var(--shad) transparent var(--lite)`, `700 11px`, `position:relative; top:1px; z-index:2`, padding `3px 13px 4px`. Inactive: `--face2`, border `var(--tabBc)`, `#3a3a3a` text, padding `2px 13px 3px`.
Right end: sheet indicator `แผ่น 1 / 3 · A3` in `9px 'Lucida Console'`, `#4a4a4a`.

Mode tab ↔ screen mapping: Schematic tab covers screens 2/5/6/7/9/10; Breadboard tab → screen 4; PCB tab → screen 8.

### StatusBar (22px)

Sunken cells, `gap:2px`, `padding:1px 2px`, each `padding:0 7px`, `10px`, `white-space:nowrap`:
`พร้อม/Ready` · `X 184.15  Y 92.70 mm` · `กริด 2.54 mm` · `เนต 14 · อุปกรณ์ 22` · `DRC 0 / ERC 0` · `เลือกอยู่: R2` · `SPICE3f5 · TRAN` · flexible spacer · run indicator.
Run indicator idle: grey dot `#7a7a7a` + `พร้อมใช้งาน/Ready`. Running: green dot with `0 0 4px #34d17a` glow, `animation: blink 1.2s infinite`, bold `กำลังจำลอง/Simulating`.

---

## Screens

Screenshots in `screens/`. All copy below is final — reproduce verbatim in both languages.

### 1 · Start / Project picker — `01-start-project-picker.png`

Full-window desktop backdrop `var(--desk)` with a `repeating-linear-gradient(0deg, rgba(255,255,255,.025) 0 1px, transparent 1px 3px)` scanline overlay, centring a **1060px** welcome window.

- **Left rail** `250px`, `linear-gradient(160deg,#1d3b52,#0e1f2c)`, padding `20px 16px`, `gap:10px`:
  - Wordmark `700 22px/1.15 Tahoma`, `letter-spacing:-.01em`, `#fff`, with ` ME` in `#e8b04a`.
  - Subtitle `11px/1.55`, `#9fbccd`. TH: "โปรแกรมทดลองวงจรอิเล็กทรอนิกส์ระดับมืออาชีพ — ออกแบบ วางบนบอร์ดทดลอง จำลองการทำงานด้วยเครื่องมือวัดจริง และทำลายปริ๊นต่อได้ในไฟล์เดียว" EN: "Professional electronics workbench — schematic capture, breadboard prototyping, SPICE-accurate simulation with real instruments, and PCB layout in one project file."
  - Footer `9px/1.5 'Lucida Console'`, `#6f93a8`: `v4.2.1 · build 20260903` / `Simulation core: SPICE3f5 / Xyce` / `ลิขสิทธิ์ใช้งานเชิงพาณิชย์ · 1 เครื่อง / Commercial licence · 1 seat`.
- **Recent projects** table, white sunken, height `262px`, columns `1fr / 92px / 78px` (`ชื่อไฟล์`, `ชนิด`, `แก้ไขเมื่อ`). Header `--face`, `10px`, cells `11px`, row divider `1px solid #efedea`, 13×13 amber file chip (`#e8b04a`, border `#8a6420`). Eight rows: `555-astable-reg.ebp` (03-09-2026), `smps-12v-3a.ebp` (01-09), `esp32-sensor-node.ebp` (28-08), `audio-preamp-ne5532.ebp` (26-08), `h-bridge-bts7960.ebp` (22-08), `7seg-counter-4026.ebp` (19-08), `lab-psu-0-30v.ebp` (15-08), `i2c-oled-driver.ebp` (11-08). Types: `ผัง + PCB / Sch + PCB`, `บอร์ดทดลอง / Breadboard`, `ผังวงจร / Schematic`.
- **Templates** `250px`, 2-column grid, `gap:6px`, raised cards with a 44px dark thumbnail (`#12161b`, border `#404040`, `9px 'Lucida Console'` tag `#7f97ab`) and a `10px/1.3` label of fixed height 26px: `555` ตั้งเวลา 555 astable · `OPA` ออปแอมป์ขยายเสียง · `PSU` ภาคจ่ายไฟเรกูเลต · `MCU` บอร์ด MCU ขั้นต่ำ · `74x` วงจรนับลอจิก · `···` ผังเปล่า A3.
- **Footer**: checked checkbox `แสดงหน้านี้เมื่อเปิดโปรแกรม / Show this page at startup`; right-aligned buttons `min-width:96px`: **default** `สร้างใหม่/New project` (bold + `0 0 0 1px #000` default-button ring), `เปิด.../Open...`, `นำเข้า.../Import...`. Divider above: `1px solid var(--shad)` + `0 -1px 0 var(--lite) inset`.

### 2 · Schematic editor — `02-schematic-editor.png`

Workspace is an SVG scene, viewBox `0 0 1100 700`, `preserveAspectRatio: xMidYMid meet`.

- Background `#12161b` + dot grid: `<pattern id="dots" width=16 height=16>` with `circle r=1 fill=#2b3440`.
- Wires `stroke #93a9bd`, `stroke-width 1.6`, `stroke-linecap: square`; power rails (top `y=90`, bottom `y=610`) `#b9cbdb` at `2`. Junction dots `circle r=3 fill=#93a9bd`.
- Symbol library as `<defs>` reusable groups (`#resV`, `#resH`, `#capV`, `#ecapV`, `#diodeV`, `#ledV`, `#gnd`) drawn with `stroke: currentColor`, `stroke-width 1.6` — colour is inherited so selection/highlight only changes `color`. IEC-style rectangular resistors.
- Circuit content: **555 astable + 7805 regulator**. `J1 DC IN`, `D1 1N4007`, `U1 LM7805 (TO-220)`, `C1 470µ/25V`, `C2 100n`, `U2 NE555 (DIP-8)` as a 140×200 box with numbered pin labels (`1 GND … 8 VCC`, `9px 'Lucida Console'`, `#7f97ab`), `R1 10k`, `R2 47k` (selected), `R4 470Ω`, `D2 LED RED`, `C3/C4 10n`, `J2 MCU` header.
- Reference labels `10px 'Lucida Console'`, `#8fa8bd`. Net labels (toggleable, prop `netLabels`) `#6fd3e0` — bare text for rails (`+9V_IN`, `+5V`, `GND`), boxed for header pins (`#12161b` fill, `#2c5f68` stroke).
- **Selection**: selected part drawn in `#e8b04a`, wrapped in `stroke-dasharray="3 2"` bounding rect plus four `7×7` amber corner handles.
- **Test point**: `TP1` — `circle r=4`, `stroke #d76a5a`, label `9px` same colour.
- **Title block** bottom-right, `288 × 96`, `stroke #2b3440`, ruled rows: `555 ASTABLE + 5V REG · SHEET 1/3`, `ELECTROBENCH ME · REV C`, `DATE 03-09-2026`, `SCALE 1:1`, `DRC 0 ERR`, `NETS 14` — all `9px 'Lucida Console'`, `#7f97ab`.

**Library panel (left).** Caption bar 19px `var(--cap)`, `700 10px`, text `คลังอุปกรณ์ · COMPONENT LIBRARY`, right chevron `▾`. Search row: sunken field placeholder `ค้นหาอุปกรณ์ / เบอร์ไอซี / Search part or MPN` (`10px`, `#8a8a8a`) + `22×19` search button. Category tree in a white sunken box, rows `padding:3px 5px`, `11px`, each row = expander (`+`/`−`, 8px) + `16×14` category chip + label + right-aligned count in `9px 'Lucida Console'`. Selected row: `--sel` background, white text, amber chip (`#e8b04a`/`#7d5a19`); unselected chips `#c9d3dc`/`#6f7d89`.

| Chip | Category (TH / EN) | Count |
|---|---|---|
| `R` | Passive · R / C / L | 1,284 |
| `D` | ไดโอด & LED / Diodes & LEDs | 642 |
| `Q` | ทรานซิสเตอร์ / MOSFET | 1,130 |
| `IC` | ไอซี & ออปแอมป์ / ICs & op-amps | 2,465 |
| `&` | ลอจิกเกต 74xx / 40xx | 388 |
| `µ` | ไมโครคอนโทรลเลอร์ / Microcontrollers | 214 |
| `S` | เซนเซอร์ / Sensors | 506 |
| `M` | มอเตอร์ & รีเลย์ / Motors & relays | 178 |
| `V` | ภาคจ่ายไฟ & เร็กกูเลเตอร์ / Power & regulators | 421 |
| `7` | จอแสดงผล LCD / OLED / Displays | 163 |
| `J` | คอนเนกเตอร์ & สวิตช์ / Connectors & switches | 597 |
| `⎓` | เครื่องมือวัด & แหล่งจ่าย / Test & measurement | 84 |

Below the tree, a subcategory strip on `#f7f6f3`: header `PASSIVE › ตัวต้านทาน & ตัวเก็บประจุ` (`10px`, `#5a5a5a`) and a 4-column grid of `#12161b` symbol tiles (`40 × 24` SVG previews). Selected tile: border `--sel` + `box-shadow: 0 0 0 1px #e8b04a`. Captions `8px/1.2 'Lucida Console'`, `#444`: `R · 1/4W`, `C · film`, `C · elec`, `L · coil`. Footer buttons: `วางลงวงจร/Place`, `ที่ใช้บ่อย/Favorites`.

**Right panel.** Three stacked sections, each with a 19px `var(--cap)` caption:
1. `เลเยอร์ · LAYERS` — 184px white sunken list; rows `10px`, `padding:2px 4px`: eye `👁` (or `·` when hidden), lock `🔒` (or `·`), 9px colour chip, name, right-aligned object count. Selected row `--sel`/white/amber chip. Below: 5 buttons `+`, `−`, `▲`, `▼`, `เพิ่มเติม.../More...` (last is `flex:2`).
2. `คุณสมบัติ · PROPERTIES` — 44×34 dark symbol preview (amber symbol) + `700 12px 'Lucida Console'` designator `R2` + kind `ตัวต้านทาน · SMD 0805`. Then label/value rows: label `10px` width 76px, value sunken field `10px 'Lucida Console'`, 18px tall — `ชื่ออ้างอิง R2`, `ค่า 47 kΩ`, `ผิดพลาด ±1 %`, `กำลังไฟ 0.125 W`, `ฟุตพรินต์ R_0805_2012`, `โมเดล SPICE R 47k TC1=0`, `เลเยอร์ สัญญาณ`, `หมุน / ล็อก 90° · OFF`.
3. `ผลซิมูเลชันของ R2` — white sunken readout `10px/1.75 'Lucida Console'`, rows divided `1px solid #f3f1ee`: `แรงดันคร่อม 2.86 V`, `กระแส 60.8 µA`, `กำลังสูญเสีย 0.17 mW`, `อุณหภูมิผิว 28.4 °C`, `ค่ายอมรับได้ 0.14 %`, `เนต THR / DIS`. Buttons: `จับสัญญาณ/Probe`, `ส่งเข้าสโคป/To scope`.

### 3 · Component library (full view) — `03-component-library.png`

Replaces the workspace with a `--face` panel.

- **Filter bar**: 280px sunken text field showing typed query `555` with a `1px × 12px` black caret; `ค้นหา/Search` button; `เรียงตาม/Sort by` + combo `ใช้บ่อยที่สุด/Most used`; right: grid/list view toggle (`24 × 20`, grid pressed).
- **Chips row** `gap:5px`, `10px`, `padding:2px 9px`: `ทั้งหมด/All` (active — `--sel`, white, border `#051239`), `ในสต็อก/In stock`, `มีโมเดล SPICE/Has SPICE model`, `ผ่านการรับรอง/Verified`, `ทะลุแผ่น/Through-hole`, `SMD`. Inactive chips `#e8e6e2`, border `#ffffff #9a9691 #9a9691 #ffffff`.
- **Part grid**: white sunken container, `padding:8px`, `grid-template-columns: repeat(6,1fr)`, `gap:8px`. Card = `1px solid #cfcdc8` on `#f8f8f6`; 58px dark thumbnail (`#12161b`) holding the (placeholder) symbol in `700 15px 'Lucida Console'` `#8fb4cf`, with a category tag top-left in `8px` `#e8b04a`; body `padding:4px 5px` = MPN `700 10px 'Lucida Console'` `#12161b`, description `9px/1.3` `#5a5a5a` clamped to 24px, and two badges `8px 'Lucida Console'` `#4a6a80` on `#e4ebf1`/`#c3cfd9` (package + `SPICE`).
  24 seeded parts: NE555P, TL074CN, LM358N, LM7805, LM317T, ULN2003A, 2N3904, BD139, IRFZ44N, 1N4007, 1N4148, LED-5R, R-0805, C-ELEC, L-100U, 74HC00, 74HC595, ATMEGA328P, ESP32-WROOM, STM32F411, DS18B20, MPU-6050, SRD-05VDC, SSD1306 (see prototype for each description/package).
- **Datasheet pane** `290px`, left divider: caption `ดาต้าชีต · DATASHEET`; 150px dark pin-out drawing (DIP-8 body `#1b2027`/`#8fb4cf`, notch arc, `14×6` pins `#6f8ba1`, pin names `7px`, part name `10px` `#cfdce6`, footer `DIP-8 · 9.81 × 6.35 mm`); spec table `10px/1.7`, label `#4a4a4a` + monospace value: Supply `4.5 – 16 V`, Output current `±200 mA`, Max frequency `500 kHz`, Timing error `1.0 %`, Temp range `0 – 70 °C`, Package `DIP-8 / SO-8`, SPICE model `NE555.LIB`, Footprint `DIP254P762X508-8`, Lifecycle `ยังผลิตอยู่/Active`. Buttons: default `วางลงวงจร/Place`, `เปิด PDF/Open PDF`.

### 4 · Breadboard 2D — `04-breadboard-2d.png`

SVG viewBox `0 0 1120 700`.

- **830-point breadboard**: 812 × 418 rounded body `#efece4` (border `#c9c5ba`) with a 4px offset dark shadow plate beneath. Tie-points via `<pattern id="tie" width=19 height=19>` — `5×5` dark socket `#2f3134` with a `4×4` `#8d8f92` insert, giving the 0.1″ pitch grid. Power rails: red line `#c0473a` and blue `#3a5fa0`, `stroke-width 2`, with `+`/`−` markers. Centre channel `#e4e0d6`, 44px tall, with a `#cfcabd` parting line. Column numbers 1/6/11/16/21/26/31/36/40 and row letters J/F/E/A in `9px 'Lucida Console'` `#8d8f92`.
- **Parts drawn as physical objects** (this is the point of the 2D view): DIP-8 IC `#23262a` with notch arc and `9×14` `#8a8f96` legs; two axial resistors — `76×16` `rx:7` bodies `#e0cfa8`/`#b6a377` with real 4-band colour codes (`#6b4a2a`,`#111`,`#c0473a`,`#c9a227` = 10 kΩ and `#c9a227`,`#5a3fa0`,`#c0473a` = 47 kΩ) and bent `#9aa0a6` leads; red LED as `circle r=13` `#d94f3d`/`#8f2f22` with a specular highlight and a soft `#3f7d68` glow ellipse; electrolytic cap `20×34` `#2c3f6b`.
- **Jumper wires**: `stroke-width 3.4`, `stroke-linecap:round`, cubic Béziers in real jumper colours `#c0473a`, `#1b1d20`, `#c9a227`, `#3f9d5a`, `#3a5fa0`.
- **Bench instruments on the right** (216px wide panels, `#d6d3ce`, `#404040` border, `#4a5f7a` header strip):
  - *DC POWER SUPPLY · 0-30V*: green 7-seg voltage `9.00` (`26px 'Lucida Console'` `#48e08a` on `#0a1a12`), amber current `0.128`, two rotary knobs (circles with indicator lines), red/black binding posts, footer `CV MODE · OVP 12.0V`.
  - *FUNCTION GENERATOR · 2MHz*: green waveform preview on `#04160f`, frequency readout `1.000 kHz`, four waveform buttons `SINE/SQR/TRI/SWEEP`, footer `AMPL 5.00 Vpp · OFFSET 0.00 V` / `DUTY 50% · OUT 50Ω`.
- **Bottom tables** (`#0d1418`, `#2b3440` border, `9px 'Lucida Console'` `#8fa8bd`):
  - `ตารางจุดต่อและค่าที่วัดได้` — columns ROW / NET / TIE-POINTS / V(DC) / I(mA); rows J1 +9V_IN 40 9.00 128.4 · F11 TRIG 5 4.21 0.02 · E16 OUT 5 3.98 9.41 · E31 LED_A 5 1.94 9.38 · J- GND 40 0.00 —.
  - `บอร์ดทดลองที่ติดตั้ง` — `▣ 830-PT BB-830 (กำลังใช้)` in `#e8b04a`, then `#6f8ba1` rows `▢ 400-PT BB-400`, `▢ PERF PB-9X15 2.54mm`, `▢ PCB 2L PCB-100X80`, `▢ DEV UNO / ESP32 / STM32F4`.

### 5 · Simulation running — `05-simulation-running.png`

Same schematic scene plus live overlays. This is the "กด Play แล้วปล่อยไฟเข้าไป" state.

- **Current flow**: animated dashes over the conducting path. `stroke #e8b04a`, `stroke-width 2.4`, `stroke-dasharray 7 5`, `filter: drop-shadow(0 0 3px rgba(232,176,74,.9))`, `animation: flow .8s linear infinite` where `@keyframes flow { to { stroke-dashoffset: -24 } }`. The LED branch uses `#5fd0a8` at `.5s` (faster = higher current) — **speed encodes magnitude, colour encodes net class.**
- **Lit LED**: `circle r=16` `#d94f3d` at `.28` opacity behind `circle r=9` `#ff7a5f` at `.7` with `animation: blink 1s steps(2) infinite`.
- **Node voltage tags**: `#0d1418` box stroked in the net colour, value in `10px 'Lucida Console'` — `5.02 V`, `5.00 V` (amber `#e8b04a`), `4.21 V`, `3.98 V` (green `#5fd0a8`), `2.14 V` (cyan `#6fd3e0`).
- **Run HUD** top-left: `196 × 52` `#0d1418` box, `#e8b04a` stroke — `▶ RUN · TRAN 0-20ms` (`11px`, amber) and `t = 12.480 ms · f = 1.44 kHz` (`10px`, `#8fa8bd`).
- **Docked scope** across the bottom of the workspace, `184px`: 17px `var(--cap)` caption `ออสซิลโลสโคป — CH1 OUT · CH2 THR` with `▭ ✕` controls; graticule via `<pattern id="scopegrid" width=40 height=40>` (`#1d4a3c` 1px lines plus centre tick marks) on `#04160f`; CH1 square wave `#5fd0a8` at 2px, CH2 charge/discharge ramp `#e8b04a` at 1.6px, both with a 3px drop-shadow glow; corner labels `2V/div`, `0.2ms/div` in `9px` `#3f7d68`. Right-side measurement box `170px`: `CH1 Vpp 4.98 V`, `CH2 Vpp 1.72 V`, `FREQ 1.442 kHz`, `DUTY 63.2 %`, `RISE 142 ns`, plus `ออโต้/Auto` and `เคอร์เซอร์/Cursor` buttons.
- Toolbar Play button renders pressed; StatusBar shows the blinking green `กำลังจำลอง`.

### 6 · Instruments — `06-instruments.png`

Three floating instrument windows over a dimmed schematic (opacity `.35`). Each window: raised frame, `box-shadow: var(--raiseSh), 5px 5px 12px rgba(0,0,0,.45)`, 19px `var(--tb2)` caption.

- **MSO-4CH oscilloscope**, 660px at `left:14px; top:12px`: 240px-tall `#04160f` display with `scopegrid`; CH1 square `#5fd0a8` 2.2px, CH2 ramp `#e8b04a` 1.8px, an 8-channel digital trace group `#6fd3e0` 1.6px at the bottom, and a dashed `#c9a227` trigger cursor with a `T` marker. Corner legends `CH1 2V/div`, `CH2 1V/div`, `D0-D7`, `50µs/div`. Right rail `118px`: four pressed-style knob readouts (`ฐานเวลา 50 µs/div`, `CH1 · แนวตั้ง 2 V/div`, `CH2 · แนวตั้ง 1 V/div`, `ทริกเกอร์ CH1 ↑ 2.5 V`) then `ออโต้/Auto` + `ช็อตเดียว/Single`. Bottom strip: 5 white measurement cells (`CH1 Vpp 4.98 V`, `ความถี่ 1.442 kHz`, `ดิวตี้ 63.2 %`, `ขาขึ้น 142 ns`, `RMS 3.14 V`).
- **6½-digit multimeter**, 296px, top-right: `#0a1a12` display — header `DC V · AUTO` / `TP1 → GND` (`9px`, `#2f7a55`), primary reading `700 38px/1.1 'Lucida Console'` `#48e08a` right-aligned `3.9812`, footer `MIN 0.0021 / MAX 4.9903 / V`. Below, a `repeat(4,1fr)` grid of 8 function buttons `V⎓ V∼ A⎓ A∼ Ω ⊣C ▶| Hz` (first pressed), then a white readout `RANGE 10 V`, `RATE 10 rdg/s`, `REL OFF`.
- **8-channel logic analyzer**, 520px, bottom-centre: 44px channel legend column (`D0`–`D7`, pressed cells 14px tall) + `#04160f` waveform area with eight `#48e08a` 1.4px digital traces and a dashed trigger line; right box `104px`: `SR 1 MS/s`, `DEPTH 1 M`, `PROTO I²C`, `ADDR 0x3C`, `TRIG START`.

### 7 · Import layout / netlist — `07-import-gerber-netlist.png`

Modal, **760px**, centred (`translate(-50%,-50%)`), over a `rgba(10,14,18,.45)` scrim. Caption `นำเข้าลายวงจร / เนตลิสต์ / Import layout / netlist` with `?` and `✕`.

- File row: label `ไฟล์/File`, sunken path field `10px 'Lucida Console'` showing `D:\Projects\amp-v3\gerber\amp-v3.zip`, `เลือกไฟล์.../Browse...` button (84 × 21).
- **Package contents** table (196px), columns `1fr / 96px / 60px` = `ไฟล์`, `เลเยอร์ปลายทาง`, `ขนาด`; rows `9px/1.9 'Lucida Console'` with a `9×9` `#4f7cb0` layer chip: `amp-v3-F_Cu.gbr` Top copper 84 KB · `amp-v3-B_Cu.gbr` Bottom copper 71 KB · `amp-v3-F_Mask.gbr` Top mask 22 KB · `amp-v3-F_Silk.gbr` Top silk 39 KB · `amp-v3-Edge.gbr` Board outline 4 KB · `amp-v3.drl` Drill 11 KB · `amp-v3.net` Netlist 18 KB · `amp-v3-BOM.csv` BOM 6 KB.
- **Preview** 280px: dark board render (green `#12311f`/`#2f6b47`, copper `#c98b4b` traces, white silk outlines, vias) with footer `100.0 × 80.0 mm · 2L · 44 NETS · 22 PARTS`.
- **Options** (checkboxes, first four checked): `จับคู่ฟุตพรินต์กับคลังอุปกรณ์อัตโนมัติ` · `สร้างผังวงจรย้อนกลับจากเนตลิสต์` · `แนบโมเดล SPICE ให้อุปกรณ์ที่รู้จัก` · `รวมเป็นเลเยอร์ใหม่ ไม่ทับของเดิม` · `หน่วยเป็นมิลลิเมตร (ไม่ใช่ mil)` (unchecked).
- **Supported formats** box, `9px/1.8 'Lucida Console'`: `GERBER RS-274X · EXCELLON · KICAD` / `EAGLE .BRD · ALTIUM .PCBDOC` / `SPICE .CIR / .NET · ALTIUM NETLIST` / `IMAGE TRACE .PNG / .JPG → แปลงเป็นลายอัตโนมัติ`. Below it a barber-pole progress bar: `repeating-linear-gradient(90deg,#0a246a 0 8px,transparent 8px 10px)` with `animation: bar 1.6s linear infinite` (`translateX(-100%)` → `translateX(320%)`).
- Footer: default `นำเข้า/Import`, `ยกเลิก/Cancel` (min-width 88, height 23).

### 8 · PCB + AI auto-place & route — `08-pcb-ai-autoplace.png`

The user-requested extension: "ต่อยอดทำลายปริ๊นเพื่อทำแผ่นวงจร สำหรับวางอุปกรณ์ได้อัตโนมัติด้วยเอไอ".

- **Board canvas** (`flex:1`, `#0e1013`): 700 × 560 board `#12311f` with `#2f6b47` 2px edge and `rx:6`; pad field via `<pattern id="pads" width=24 height=24>` (`r=4` `#c69a5c` pad, `r=1.6` `#101215` hole) at `.22` opacity; copper traces `#c98b4b` `stroke-width 7`, `linecap/linejoin round`; bottom-layer traces `#5f7fb0` at 5px, `.8` opacity; vias = `r=9` copper annulus + `r=3.4` dark hole; silkscreen outlines `#e6e6e0` 1.6px with `U1/U2/J1/J2/D2` designators `12px 'Lucida Console'` at `.9`; a selected footprint marked by dashed `#e8b04a` box + 8px corner handles; **unrouted ratsnest** as dashed `#6fd3e0` 1.4px at `.75`; board outline repeated as dashed `#8fd0a8`; footer `PCB-100X80 · 2 LAYER · 1.6mm FR-4 · 35µm Cu` and `DRC 0 / 0`.
- **Layer chips** overlaid top-left, `9px 'Lucida Console'`, `padding:3px 8px`: `TOP CU` active (`#e8b04a` on `#8a6420` border, text `#2b1f06`), then `BOT CU`, `SILK`, `MASK`, `DRILL`, `OUTLINE` (`#1b2027`/`#3a4652`/`#8fa8bd`).
- **AI panel** `314px`, right. Caption uses a distinct violet gradient `linear-gradient(90deg,#7a5fa8,#b79ad4)` with `✦` — AI features are the only place violet appears; keep that mapping.
  - Description `10px/1.55`: TH "AI อ่านผังวงจรแล้วจัดวางอุปกรณ์ เดินลายทองแดง และเว้นระยะความร้อน/แรงดันสูงให้อัตโนมัติ ก่อนส่งออกเป็นไฟล์ Gerber สำหรับสั่งทำแผ่นวงจร".
  - **Placement goals** (checkboxes; 1,2,3,5 checked): `ลายสั้นที่สุด` · `กระจายความร้อน (U1, Q1)` · `เว้นระยะไฟแรงสูง 2.0 mm` · `จัดกลุ่มตามบล็อกวงจร` · `ลดจำนวนเวีย`. Plus combos `ความกว้างลาย 0.35 mm` and `เวีย (นอก/ใน) 0.8 / 0.4`.
  - **Progress** block: `กำลังจัดวาง.../Placing...` + `68%`; the bar is a **segmented** 22-cell strip (15 filled `--sel`, rest `#e6e4e0`) inside a 14px sunken well — period-correct, not a smooth bar. Log lines `9px/1.6 'Lucida Console'`: `· PLACEMENT PASS 3/4 · 22 PARTS`, `· RATSNEST LEN 1,842 → 1,106 mm`, `· THERMAL: U1 MOVED TO EDGE`, `· ROUTED 38 / 44 NETS`.
  - **DRC table** (white sunken, `9px/1.75`), value column green `#1c7a3e`: Track↔track PASS · Track↔pad PASS · Drill size PASS · Current capacity PASS · Board edge 0.5 mm PASS · Silk over pad PASS · Unrouted nets `6`.
  - Buttons: default `✦ จัดวาง + เดินลาย/Auto-place & route` + `ยกเลิก`; then `ส่งออก Gerber/Export Gerber` and `พิมพ์ลายปริ๊น 1:1/Print 1:1 artwork`.

### 9 · Layer manager — `09-layer-manager.png`

Modal **700px** over a `.4` scrim.

- **Layer table** (280px): columns `26px 26px 26px 1fr 62px 54px` = eye, lock, colour, `ชื่อเลเยอร์`, `ชิ้นงาน`, `ความทึบ`. Rows `10px`, `line-height:2.1`. Selected row `--sel` + white text + amber swatch with white border; others `#8fa8bd`/`#55666f` swatch, dividers `#f1efec`.

| # | Layer (TH / EN) | Objects | Opacity | State |
|---|---|---|---|---|
| 1 | วงจรหลัก · Schematic | 148 | 100% | visible |
| 2 | ราง +5V / +9V · Power rails | 22 | 100% | visible |
| 3 | สัญญาณ · Signal nets | 61 | 100% | **selected** |
| 4 | กราวด์ · Ground | 18 | 100% | visible |
| 5 | ป้ายเนต · Net labels | 14 | 80% | visible |
| 6 | หมายเหตุ & มิติ · Notes & dimensions | 9 | 65% | **locked** |
| 7 | ทองแดงด้านบน · Top copper | 96 | 100% | visible |
| 8 | ทองแดงด้านล่าง · Bottom copper | 74 | 55% | **hidden** |
| 9 | ซิลค์สกรีน · Silkscreen | 41 | 90% | visible |

- Buttons under the table: `เพิ่ม/Add`, `ลบ/Delete`, `จัดกลุ่ม/Group`, `รวมเลเยอร์/Merge`, `นำเข้าชุดเลเยอร์/Import set`.
- **Right column** 238px: `คุณสมบัติเลเยอร์` fields — `ชนิด สัญญาณ / ทองแดง`, `โหมดผสาน ปกติ`, `ใช้กับซิมูเลชัน ใช้`, `พิมพ์ออก พิมพ์`, `สแนป 2.54 mm`; then an opacity slider (4px sunken track, `9 × 16` raised thumb at 76%, value `80%`). Below: `สีเลเยอร์` — 8×2 swatch grid, cells 15px, `1px solid #6a6a6a`; hint `9px/1.6 'Lucida Console'`: "ดับเบิลคลิกชื่อเลเยอร์เพื่อเปลี่ยนชื่อ · Ctrl+คลิก เพื่อเลือกหลายเลเยอร์". Footer `ตกลง` (default) / `ยกเลิก`.

> **Note for implementation:** the 16 colour swatches render as empty bordered cells in the prototype (no fill was assigned). Populate them from the layer palette: `#e8b04a #6fd3e0 #5fd0a8 #d76a5a #93a9bd #c98b4b #5f7fb0 #b79ad4 #48e08a #ff7a5f #8fa8bd #c9a227 #3f9d5a #2c3f6b #8a6420 #e6e6e0`.

### 10 · Settings — `10-settings-language.png`, `11-settings-appearance-themes.png`

Modal **620px**. Tab strip: `ทั่วไป/General`, `ภาษา/Language`, `ธีมหน้าตา/Appearance`, `ซิมูเลชัน/Simulation`, `บอร์ด/Boards`, `คีย์ลัด/Shortcuts`. Only Language and Appearance are wired in the prototype; the tab body is a sunken group box (`border-color: var(--lite) var(--shad) var(--shad) var(--lite)`, `padding:12px`).

**Language tab** — radio group `ภาษาที่ใช้แสดงผล/Interface language`: `ไทย (Thai) — ค่าเริ่มต้น`, `English (United States)`, `ไทย + English (แสดงคู่กัน)`; the selected radio follows the active language. Then combos: `ฟอนต์อินเทอร์เฟซ Tahoma 8 pt`, `หน่วยความยาว มิลลิเมตร (mm)`, `รูปแบบตัวเลข 1,234.56`, `คำนำหน้าหน่วย k / M / µ / n`, `ปฏิทิน พุทธศักราช (พ.ศ.)` — note the Buddhist-era calendar option for Thai. Three checked checkboxes: `ใช้ชื่อเบอร์อุปกรณ์เป็นภาษาอังกฤษเสมอ`, `แสดงคำแนะนำเครื่องมือสองภาษา`, `สลับภาษาด้วยคีย์ลัด Ctrl+Shift+L`. Right column: a live 250px preview window (`คุณสมบัติ — R2`, `ตัวต้านทาน 47 kΩ · ผิดพลาด ±1%`, readouts แรงดัน/กระแส/ความถี่, Run/Stop buttons) and the note: "เปลี่ยนภาษาแล้วใช้งานได้ทันที ไม่ต้องปิดโปรแกรม · ชื่อเบอร์อุปกรณ์และหน่วยจะคงเป็นภาษาอังกฤษตามมาตรฐาน".

**Appearance tab** — `ธีมหน้าต่างและปุ่ม/Window and control theme` as a 2-column grid of 7 theme cards, `gap:7px`, each `padding:6px`, sunken. Card = an **84px live chrome thumbnail** (miniature title bar + a sunken field + a raised button, built from that theme's own tokens) beside a radio + `700 10px` theme name + `9px/1.4` description. Then `ขนาดตัวอักษร Tahoma 8 pt` combo and three checked options: `เปิดแอนิเมชันหน้าต่าง`, `เงาใต้เมนูและหน้าต่าง`, `ใช้ธีมนี้กับทุกหน้าต่างเครื่องมือวัด`. Right column repeats the live preview window (now `ตกลง`/`ยกเลิก` buttons) plus the note: "ธีมเปลี่ยนเฉพาะกรอบหน้าต่าง ปุ่ม และแผงควบคุม — พื้นที่ทำงานวงจรยังเป็นโทนมืดเสมอเพื่อให้เห็นลายวงจรและค่าที่วัดได้ชัด".

Footer: `ตกลง/OK` (default), `ยกเลิก/Cancel`, `นำไปใช้/Apply`.

---

## Interactions & Behavior

| Trigger | Result |
|---|---|
| Drag a part from library tree/grid → workspace | Ghost preview snapped to `2.54 mm` grid; drop commits with an auto-incremented designator (R5, C5…) |
| Click part | Selects; amber recolour + dashed bbox + 4 corner handles; Properties + Sim result panels bind to it |
| `R` / space during drag | Rotate 90° |
| Wire tool + click node | Rubber-band wire, orthogonal routing, junction dot auto-inserted at a 3-way meet |
| Toolbar `▶ Play` | Run transient sim → screen 5 overlays: flow dashes, node tags, lit indicators, HUD, docked scope. Toolbar Play latches pressed; StatusBar dot turns green and blinks |
| `Pause` / `Stop` / `Step` | Freeze animation while keeping values / clear overlays / advance one timestep |
| Mode tabs | Switch the same project between Schematic / Breadboard / PCB / Netlist representations — **the netlist is shared; a change in one view updates all** |
| Theme buttons or Appearance tab | Rewrites the CSS custom-property set on the app root; instant, no reload, no restart |
| ไทย/EN toggle, `Ctrl+Shift+L` | Swaps every UI string; part numbers and SI units stay English |
| Eye / lock icon in Layers | Toggle visibility (`--sel` row keeps selection) / lock editing |
| Double-click layer name | Inline rename; `Ctrl+click` multi-selects layers |
| `✦ Auto-place & route` | Runs placement passes; segmented progress + streaming log lines; DRC table refreshes live |
| Import dialog `Browse` → `Import` | Barber-pole progress, footprint matching, netlist reverse-engineering into new layers |

**Animations** (only four in the whole product — keep it restrained):
- `flow` — `stroke-dashoffset: 0 → -24`, `.8s linear infinite` (`.5s` for high-current branches). Current flow.
- `blink` — `opacity: 1 → .35` at 50%, `1s steps(2)` for LEDs, `1.2s` ease for the status dot.
- `bar` — barber-pole progress, `translateX(-100%) → translateX(320%)`, `1.6s linear`.
- No page transitions, no fades on panels. Retro chrome should feel instantaneous.

## State Management

```ts
type Screen = 'start'|'schematic'|'library'|'breadboard'|'sim'|'instruments'|'import'|'pcb'|'layers'|'settings';
type Lang   = 'th'|'en';
type Theme  = 'me'|'xp'|'silver'|'mac'|'macos'|'aqua'|'classic';

interface AppState {
  screen: Screen;              // route / active modal
  lang: Lang;                  // persisted
  theme: Theme;                // persisted
  settingsTab: number;         // 0..5, Appearance = 2
  netLabels: boolean;          // show net labels on schematic
  activeTool: number;          // 0..8 toolbar tools
  mode: 0|1|2|3;               // schematic | breadboard | pcb | netlist
  running: boolean;            // simulation transport
  selection: PartId[];         // 'R2' in all mocks
  activeLayer: LayerId;        // 'signal' in all mocks
  layers: Layer[];             // visible, locked, opacity, colour, objectCount
  zoom: number; grid: number;  // 1.6, 2.54mm
}
```

The real product additionally needs: project document model (parts, nets, wires, footprints, sheets), a netlist derived from the schematic and shared by all four modes, simulation result buffers per node/branch, instrument channel configuration, and an undo/redo command stack. Language and theme are user preferences, not document state. In the prototype `running` is derived (`screen === 'sim'`); in production it is real transport state.

## Design Tokens

### Workspace (theme-independent — never restyled)

| Token | Value | Use |
|---|---|---|
| workspace bg | `#12161b` | Schematic/breadboard canvas |
| pcb canvas bg | `#0e1013` | PCB canvas |
| grid dot | `#2b3440` | 16px dot pattern |
| wire / symbol | `#93a9bd` | Default net + symbol stroke, 1.6px |
| rail | `#b9cbdb` | Power rails, 2px |
| label | `#8fa8bd` | Reference designators, 10px mono |
| meta text | `#7f97ab` | Pin names, title block |
| selection / current | `#e8b04a` | Selected object, current flow, active chip |
| net class A | `#5fd0a8` | Output/signal probe, scope CH1 |
| net class B | `#6fd3e0` | Net labels, ratsnest, logic traces |
| test point | `#d76a5a` | TP markers |
| scope screen | `#04160f` bg, `#1d4a3c` graticule, `#48e08a` trace | Instruments |
| DMM screen | `#0a1a12` bg, `#48e08a` primary, `#2f7a55` labels | Multimeter |
| pcb copper | `#c98b4b` top, `#5f7fb0` bottom, `#12311f`/`#2f6b47` substrate, `#e6e6e0` silk | PCB |
| breadboard | `#efece4` body, `#2f3134`/`#8d8f92` tie-point, `#c0473a`/`#3a5fa0` rails | Breadboard |
| AI accent | `linear-gradient(90deg,#7a5fa8,#b79ad4)` | AI panel captions only |
| ok / warn / err | `#1c7a3e` / `#c9a227` / `#8a2b22` | DRC, transport, alerts |

### Chrome themes (CSS custom properties on the app root)

Every control reads these; switching a theme is one object swap. Full definitions live in the prototype's `themeTokens()`.

| Token | Purpose |
|---|---|
| `--face` `--face2` `--press` | Control face, inactive tab / secondary face, pressed face |
| `--lite` `--shad` `--dark` | Bevel highlight, shadow, outline |
| `--raiseBc` `--sunkBc` `--tabBc` | Border-colour shorthands for raised / sunken / tab edges |
| `--raiseSh` `--raiseSh1` `--sunkSh` | Box-shadows completing the bevel (2-tone in ME, gloss in XP/Aqua) |
| `--tb` `--tb2` `--cap` `--tbFg` `--capFg` | Window caption, dialog caption, panel caption + their text colours |
| `--sel` | Selection highlight (list rows, chips) |
| `--rad` `--tbrad` `--ctlRad` | Control radius, window radius, window-button radius |
| `--frame` `--frameBc` | Window frame fill + border |
| `--closeBg/Fg/Bc` `--minBg/Bc` `--maxBg/Bc` | Window buttons |
| `--desk` | Desktop backdrop on the Start screen |

**Windows ME (default)** — `--face:#d6d3ce`, `--face2:#c9c5bf`, `--press:#cbc7c1`, `--lite:#fff`, `--shad:#808080`, `--dark:#404040`; raised border `#ffffff #404040 #404040 #ffffff` + `inset -1px -1px 0 #808080, inset 1px 1px 0 #f2efe9`; sunken border `#808080 #ffffff #ffffff #808080` + `inset 1px 1px 0 #404040`; caption `linear-gradient(90deg,#0a246a 0%,#2f5f9e 62%,#8fb7e0 100%)`; `--sel:#0a246a`; `--rad:0`; desktop `radial-gradient(120% 100% at 30% 0%,#4d7f97,#245a72 45%,#123a4c)`.

**Windows XP · Luna Blue** — `--face:#ece9d8`, `--shad:#aca899`, borders flatten to a single `#003c74` (`#7f9db9` sunken), `--raiseSh: inset 0 1px 0 #fff, inset 0 -7px 7px -7px rgba(0,0,0,.13)`, `--sunkSh:none`; caption `linear-gradient(180deg,#1a86ec 0%,#4ba6f8 7%,#1273e2 34%,#0a5cd0 72%,#1272e0 100%)`; `--sel:#316ac5`; `--rad:3px`, `--tbrad:8px`; close button `linear-gradient(180deg,#f4977f,#cf4326)`, min/max blue gel.

**Windows XP · Silver** — same geometry, neutral palette: `--face:#e9e9ee`, `--shad:#a8a8b2`, borders `#7b7b88`, caption `linear-gradient(180deg,#9c9cb2,#c2c2d2 8%,#8e8ea6 38%,#6e6e86 76%,#9494ac)`, `--sel:#5c6ea8`.

**Macintosh · Platinum** — `--face:#dcdcdc`, black 1px borders, pinstripe caption `repeating-linear-gradient(180deg,#f2f2f2 0 1px,#c4c4c4 1px 2px)` with **black** caption text, `--raiseSh: inset 0 1px 0 #fff, inset -1px -1px 0 #a4a4a4`, `--rad:5px`, `--ctlRad:2px`.

**macOS · Sonoma** — `--face:#f2f2f4`, `--shad:#d2d2d8`, hairline borders `#d0d0d6`, caption `linear-gradient(180deg,#f7f7f9,#e8e8ec)` with `#1d1d1f` text, `--sel:#0a6cff`, `--rad:6px`, `--tbrad:11px`; traffic lights `#ff5f57 / #febc2e / #28c840`, `--ctlRad:50%`, `--ctlFg:transparent`; desktop `linear-gradient(160deg,#2b3a6b,#5b4a8f 45%,#8c5c96 75%,#c98a7a)`.

**Aqua 3D · Liquid** — glossy gel: `--face: linear-gradient(180deg,#fbfdff,#dce8f6)`, `--raiseSh: inset 0 1px 0 rgba(255,255,255,.95), inset 0 6px 6px -6px #fff, inset 0 -7px 8px -7px rgba(20,70,140,.28), 0 1px 2px rgba(20,60,120,.18)`, caption `linear-gradient(180deg,#a8dcff,#57b0f7 42%,#1476e2 52%,#3f9df4 78%,#8fd0ff)`, `--rad:9px`, `--tbrad:13px`, radial-gradient gel traffic lights, desktop `radial-gradient(130% 110% at 25% 0%,#8fd8ff,#37a0ef 35%,#0d5fc4 70%,#063f8f)`.

**Classic 2000** — ME geometry with `--face:#d4d0c8` and a flat solid `#0a246a` caption; desktop `#3a6ea5`.

### Typography

- **UI**: `Tahoma, 'Leelawadee UI', 'Noto Sans Thai', Verdana, sans-serif`. Tahoma is the period-correct face **and** renders Thai acceptably; Leelawadee UI / Noto Sans Thai carry Thai where Tahoma's coverage is weak. Verify Thai vowel/tone-mark stacking at 10–11px on the target platform before shipping; if it breaks, use Noto Sans Thai for Thai runs and keep Tahoma for Latin.
- **Sizes**: 11px body/menus/tabs · 10px panel rows, captions, table cells · 9px dense monospace tables, hints · `700` weight for captions, designators, default buttons.
- **Data/technical**: `'Lucida Console', ui-monospace, monospace` at 9–11px for every measured value, MPN, net name, and log line. Large readouts: 38px (DMM), 26px (PSU volts), 18px (PSU amps), 14px (function generator).
- Never mix: prose in Tahoma, numbers in Lucida Console. That split is the core typographic rule.

### Spacing, borders, radii

- Spacing scale: `2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20 px`. Panel padding `4–7px`, dialog padding `10–12px`, control gap `2–6px`.
- Control heights: `18px` (compact field), `19–20px` (field/small button), `21–24px` (dialog button), `22px` (toolbar/status/title), `30px` (toolbar).
- Bevels are **1px borders + inset shadows**, never `border: 2px`. Raised = light top/left, dark bottom/right; sunken = the inverse plus `inset 1px 1px 0 var(--dark)`.
- Default (focused) button adds `0 0 0 1px #000` outside the bevel — the classic default-button ring.
- Radii come only from `--rad` / `--tbrad` / `--ctlRad`; hard-code no radius anywhere.

## Assets

- **Circuit symbols, breadboard, PCB, instrument screens**: all hand-authored inline SVG in the prototype (`<defs>` groups `#resV #resH #capV #ecapV #diodeV #ledV #gnd` and patterns `#dots #tie #pads #scopegrid`). These are production-usable as a starting symbol set; extend to a full IEC/ANSI library.
- **Component library icons**: **placeholders** (monospace glyphs). Need real artwork — either a licensed part-symbol set or commissioned vectors. Reserve the 58px dark tile per card.
- **Category chips**: 1–2 character text badges, not icons. Can stay as-is or be upgraded to vectors.
- **Emoji used as stand-in glyphs**: `👁 🔒 🔍` in the Layers list and library search. Replace with real 1-bit-style icons; emoji break the period aesthetic and render inconsistently across platforms.
- No raster images, no external fonts, no icon library dependency.

## Files

```
design_handoff_electrobench/
├── README.md                       ← this document
├── design/
│   ├── ElectroBench ME.dc.html     ← the HTML design reference (template + logic + theme tokens)
│   └── support.js                  ← prototype runtime (do NOT port)
└── screens/
    ├── 01-start-project-picker.png
    ├── 02-schematic-editor.png
    ├── 03-component-library.png
    ├── 04-breadboard-2d.png
    ├── 05-simulation-running.png
    ├── 06-instruments.png
    ├── 07-import-gerber-netlist.png
    ├── 08-pcb-ai-autoplace.png
    ├── 09-layer-manager.png
    ├── 10-settings-language.png
    ├── 11-settings-appearance-themes.png
    ├── theme-xp-luna.png
    ├── theme-xp-silver.png
    ├── theme-mac-platinum.png
    ├── theme-macos-sonoma.png
    ├── theme-aqua-3d-liquid.png
    └── theme-classic-2000.png
```

To read the design source directly: open `design/ElectroBench ME.dc.html` in a browser (it runs standalone). The theme token sets are in the logic class method `themeTokens()`; all bilingual strings are in the `D` dictionary at the top of `renderVals()` as `[thai, english]` pairs — that dictionary is a ready-made i18n resource, port it wholesale.

## Implementation notes & known gaps

1. **Do not build the black navigator bar** at the top of the prototype — it is a mockup-only screen switcher.
2. **The prototype is static.** No drag, no real simulation, no file I/O. Every value shown is representative sample data from one consistent project (`555-astable-reg.ebp`) — reuse it as your dev fixture so screenshots stay comparable.
3. **Layer colour swatches** in screen 9 are unfilled; use the palette given in that section.
4. **Instrument windows** are absolutely positioned in the mock. In production they need real window management: drag, resize, z-order, dock/undock, and multi-monitor.
5. **Scale reality check.** The mocks show 22 parts and 14 nets. The product claims libraries of thousands of parts and must handle boards with hundreds of components — plan virtualised lists for the library grid/tree, and a retained-mode canvas renderer for the editors.
6. **AI auto-place** is shown mid-run at 68%. Specify real behaviour before building: which goals are hard constraints vs soft costs, whether placement is interruptible, whether the user can pin parts before a run, and how results are diffed/undone.
7. **Bilingual layout risk.** Thai strings run 15–40% longer than English in this UI. Panels are fixed-width; verify no truncation in Thai at every label, especially the 76px property labels and 66px layer-property labels.
8. **Accessibility.** The retro aesthetic gives several low-contrast pairs (`#7f97ab` on `#12161b` passes; `#4a6a80` on `#e4ebf1` and `#6f8ba1` on `#0d1418` do not). Provide a high-contrast theme variant alongside the seven cosmetic ones, and ensure every control is keyboard-reachable — this is a professional tool used all day.

---

## ภาคผนวก ก · สรุปภาษาไทย

**คอนเซ็ปต์โปรแกรม** — ElectroBench ME คือโปรแกรมทดลองวงจรอิเล็กทรอนิกส์ระดับมืออาชีพ ทำงานครบวงจรในไฟล์เดียว: ออกแบบผังวงจร → วางบนบอร์ดทดลอง 2D → กด Play ปล่อยไฟเข้าไปเพื่อจำลองการทำงานด้วยเครื่องมือวัดเสมือนจริง → ทำลายปริ๊น PCB พร้อม AI จัดวางอุปกรณ์และเดินลายอัตโนมัติ กลุ่มผู้ใช้คือช่างซ่อมมืออาชีพ วิศวกรออกแบบ R&D อาจารย์ นักศึกษา ปวช./ปวส. และ Maker จริงจัง — ไม่ใช่โปรแกรมสำหรับเด็กหัดเล่น

**สิ่งที่อยู่ในแพ็กเกจนี้**
- `design/ElectroBench ME.dc.html` — ไฟล์ออกแบบ HTML เปิดในเบราว์เซอร์ได้เลย ใช้เป็น**ต้นแบบอ้างอิง** ไม่ใช่โค้ดสำหรับนำไปใช้จริง
- `screens/*.png` — ภาพนิ่งครบทั้ง 10 หน้าจอ + ตัวอย่างธีมทั้ง 7 แบบ
- `README.md` — สเปกละเอียด: ขนาดทุกพิกเซล ค่าสีทุกตัว ฟอนต์ ข้อความทั้งไทยและอังกฤษ พฤติกรรมการใช้งาน และ design token ทั้งหมด

**สิ่งที่ Claude Code ต้องทำ** — สร้างงานนี้ขึ้นใหม่ในโค้ดเบสจริงตามสเปกในไฟล์นี้ ไม่ใช่ก๊อป HTML ไปใช้ตรง ๆ แนะนำ Tauri + React + TypeScript, ngspice/Xyce แบบ WASM สำหรับซิมูเลชัน และวาดพื้นที่ทำงานด้วย canvas (ไม่ใช่ DOM ต่อชิ้น เพราะวงจรจริงมีชิ้นงานหลายพัน)

**สิ่งที่ยังต้องเติม** — ไอคอนอุปกรณ์จริง (ในต้นแบบยังเป็นตัวอักษรแทน), สีในช่องเลือกสีเลเยอร์, การจัดการหน้าต่างเครื่องมือวัดจริง (ลาก/ย่อขยาย/ปักหมุด), และการกำหนดพฤติกรรม AI จัดวางให้ชัดเจนก่อนลงมือทำ

**ห้ามทำ** — แถบดำด้านบนที่มีปุ่มเลข 1–10 และปุ่มสลับธีม/ภาษา เป็นแค่ตัวช่วยดูต้นแบบเท่านั้น ไม่ต้องสร้างในโปรแกรมจริง
