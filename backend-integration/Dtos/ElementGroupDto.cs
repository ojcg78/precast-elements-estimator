// TODO: adjust the namespace to match your existing project.
using System.Text.Json;

namespace PrecastEstimator.Api.Dtos;

/// <summary>
/// Wire shape for element groups. Data is typed as JsonElement (not string)
/// so it round-trips as real, unescaped JSON in the request/response body —
/// the client sends/receives the same object shape it already builds in
/// index.html (costPerUnit, qtyPerUnit, unitRates, qtyUnits, raw,
/// rawSections, rawCustomElements, rawEoItems, rawConsumables, inputs, rates),
/// and the API just stores it as-is in ElementGroup.DataJson.
/// </summary>
public class ElementGroupDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string GroupId { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public decimal? PricePerM3 { get; set; }
    public decimal? Total { get; set; }
    public JsonElement Data { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
}

public class CreateOrUpdateElementGroupDto
{
    public string GroupId { get; set; } = string.Empty;
    public string ElementType { get; set; } = string.Empty;
    public decimal? PricePerM3 { get; set; }
    public decimal? Total { get; set; }
    public JsonElement Data { get; set; }
}
