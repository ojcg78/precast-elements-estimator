// TODO: adjust the namespace to match your existing project.
namespace PrecastEstimator.Api.Models;

/// <summary>
/// One rate in the shared, global cost dictionary (maps 1:1 to a key in the
/// client's DEFAULT_COSTS object, e.g. "Steel Bars", "Concrete 50 MPa").
/// </summary>
public class CostSetting
{
    public string SettingKey { get; set; } = string.Empty;
    public decimal SettingValue { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
}
