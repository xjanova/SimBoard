namespace SimBoard.Document;

using static CatalogBuilder;

/// <summary>
/// อุปกรณ์พาสซีฟ.
///
/// One file per family so the catalogue can grow without every addition touching the
/// same place. Figures come from manufacturer datasheets; anything not yet checked
/// against one carries <see cref="Provenance.Unverified"/> and the UI says so before it
/// shows the number, because a wrong rating does not raise a dialog — it kills a board.
/// </summary>
public static class CatalogPassive
{
    public static IReadOnlyList<PartDefinition> Parts { get; } =
    [
    ];
}
