// TODO: adjust the namespace to match your existing project.
namespace PrecastEstimator.Api.Models;

/// <summary>
/// One Walls or Columns group added to a Project's summary. DataJson holds the
/// full object the client already builds today (costPerUnit, qtyPerUnit,
/// unitRates, qtyUnits, raw, rawSections, rawCustomElements, rawEoItems,
/// rawConsumables, inputs, rates) so the client can restore it into the form
/// exactly like it restores from localStorage today (see restoreFields in
/// index.html). We deliberately do NOT normalize that structure into more
/// tables — it evolves with the calculator's own form fields, and a JSON
/// snapshot is the low-risk way to keep it working without a schema change
/// every time a field is added to the wall/column calculators.
/// </summary>
public class ElementGroup
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string GroupId { get; set; } = string.Empty;

    /// <summary>"Walls" or "Columns" — matches the client's g.type.</summary>
    public string ElementType { get; set; } = string.Empty;

    public decimal? PricePerM3 { get; set; }
    public decimal? Total { get; set; }

    /// <summary>Raw JSON snapshot of the client's group object.</summary>
    public string DataJson { get; set; } = "{}";

    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime ModifiedAtUtc { get; set; }

    public Project? Project { get; set; }
}
