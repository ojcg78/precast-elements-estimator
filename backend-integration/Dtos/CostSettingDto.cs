// TODO: adjust the namespace to match your existing project.
namespace PrecastEstimator.Api.Dtos;

/// <summary>
/// Wire shape for GET/PUT /api/cost-settings. The client's costDict is a flat
/// { "Steel Bars": 3.2, ... } object, so the whole payload is just that map.
/// </summary>
public class CostSettingsDto
{
    public Dictionary<string, decimal> Rates { get; set; } = new();
}
