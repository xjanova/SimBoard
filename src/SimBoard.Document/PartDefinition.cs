namespace SimBoard.Document;

/// <summary>
/// What a pin does electrically. This is not decoration: it decides what may legally
/// connect to what (ERC), whether a net has a driver, and how the part is emitted into
/// a netlist.
/// </summary>
public enum PinKind
{
    /// <summary>No direction — resistor, capacitor, inductor ends.</summary>
    Passive,
    Input,
    Output,
    Bidirectional,
    /// <summary>Can only pull low; needs a pull-up on the net. I²C lines, open-collector.</summary>
    OpenDrain,
    /// <summary>Supply input, e.g. VCC. Two of these on one net is fine.</summary>
    Power,
    Ground,
    /// <summary>Continuous signal, not a logic level.</summary>
    Analog,
    /// <summary>Manufacturer says leave it unconnected.</summary>
    NotConnected,
}

/// <summary>Which edge of the symbol body a pin leaves from.</summary>
public enum PinSide { Left, Right, Top, Bottom }

/// <summary>
/// One pin of a part. <paramref name="Slot"/> is its position along its side in grid
/// steps from the top/left of the body, so symbol geometry stays declarative.
/// </summary>
public sealed record Pin(
    string Number,
    string Name,
    PinKind Kind,
    PinSide Side,
    int Slot,
    string? Description = null);

/// <summary>How a part turns into SPICE. Determines what the simulator can actually do with it.</summary>
public enum SpiceKind
{
    /// <summary>A primitive ngspice element: R, C, L, D, Q, M, V, I.</summary>
    Primitive,
    /// <summary>An .include'd .subckt — op-amps, regulators, the 555.</summary>
    Subcircuit,
    /// <summary>
    /// A digital or firmware-driven device SPICE cannot model from physics. We emit a
    /// behavioural stand-in (supply load, defined output levels, timed sources) so the
    /// analog circuit around it still solves correctly.
    /// </summary>
    Behavioural,
    /// <summary>Carries no electrical meaning of its own — net labels, ground symbols.</summary>
    None,
}

/// <summary>
/// The electrical envelope of a digital part. This is what "the program knows this
/// component" actually means for an MCU or a sensor: not a transistor model, but the
/// numbers a designer has to respect to build a board that works.
/// </summary>
public sealed record DigitalSpec(
    double VccMin,
    double VccMax,
    double VccTypical,
    /// <summary>Typical supply current in amps, at VccTypical.</summary>
    double Icc,
    /// <summary>Minimum input high / maximum input low, as a fraction of Vcc when null.</summary>
    double? Vih = null,
    double? Vil = null,
    /// <summary>Output drive at Voh/Vol, amps. Null for input-only parts.</summary>
    double? IoMax = null,
    Bus Bus = Bus.None,
    /// <summary>Fixed or strap-selectable bus address, when the part has one.</summary>
    string? BusAddress = null,
    /// <summary>
    /// True when the breakout board already carries bus pull-ups. Declared per part,
    /// never inferred from the package name — guessing it from a substring silences the
    /// pull-up rule on parts that genuinely need one.
    /// </summary>
    bool HasIntegratedPullups = false);

public enum Bus { None, I2C, Spi, Uart, OneWire, Pwm, Analog, Parallel }

/// <summary>How the symbol is drawn. Real symbols are shapes, not glyphs.</summary>
public enum SymbolShape
{
    /// <summary>IEC rectangle — resistor.</summary>
    Box,
    /// <summary>A rectangular body with pin labels — ICs, modules, sensors.</summary>
    IcBody,
    CapacitorNonPolar,
    CapacitorPolarised,
    Inductor,
    Diode,
    Led,
    Zener,
    BjtNpn,
    BjtPnp,
    MosfetN,
    MosfetP,
    VoltageSource,
    CurrentSource,
    Ground,
    Switch,
    Connector,
    Crystal,
    Speaker,
    Motor,
}

/// <summary>
/// A part the editor can place. One definition, many instances on a sheet.
///
/// The definition owns everything that is true of the type — pins, symbol, how to write
/// it into a netlist, its electrical envelope. An instance owns only what is true of
/// that placement: position, rotation, designator, and parameter values.
/// </summary>
public sealed record PartDefinition
{
    public required string Key { get; init; }

    /// <summary>Designator prefix: R, C, U, Q, D, J. The editor auto-numbers from this.</summary>
    public required string Prefix { get; init; }

    public required string Name { get; init; }
    public required string NameTh { get; init; }

    public required SymbolShape Symbol { get; init; }
    public required SpiceKind Spice { get; init; }
    public required IReadOnlyList<Pin> Pins { get; init; }

    /// <summary>Manufacturer part number, when the part is a specific device.</summary>
    public string? Mpn { get; init; }

    public string? Package { get; init; }

    /// <summary>
    /// The primary editable value and its unit — 10k, 100n, 5V. Null for parts that
    /// have no single value (an MCU, a connector).
    /// </summary>
    public string? DefaultValue { get; init; }

    public string? Unit { get; init; }

    /// <summary>SPICE model or subcircuit name, and the library it comes from.</summary>
    public string? SpiceModel { get; init; }
    public string? SpiceLibrary { get; init; }

    /// <summary>Present for digital and firmware-driven parts. Null for analog primitives.</summary>
    public DigitalSpec? Digital { get; init; }

    /// <summary>Body size in grid steps. Pins are placed on the edges of this box.</summary>
    public int BodyWidth { get; init; } = 2;
    public int BodyHeight { get; init; } = 2;

    /// <summary>Free-text notes shown in the properties panel — gotchas worth knowing.</summary>
    public string? NoteTh { get; init; }

    public Pin? PinByNumber(string number) =>
        Pins.FirstOrDefault(p => p.Number.Equals(number, StringComparison.OrdinalIgnoreCase));

    public Pin? PinByName(string name) =>
        Pins.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>True when the simulator can produce real numbers for this part.</summary>
    public bool IsSimulatable => Spice is SpiceKind.Primitive or SpiceKind.Subcircuit;

    public override string ToString() => $"{Key} ({Pins.Count} pins)";
}
