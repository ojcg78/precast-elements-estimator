// TODO: adjust the namespace to match your existing project.
namespace PrecastEstimator.Api.Models;

/// <summary>
/// A Project/Tender. Replaces the projName/projCode pair that used to be the
/// only "project" concept the app had (stored nowhere, just two text fields).
/// </summary>
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ClientName { get; set; }
    public string? Status { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime ModifiedAtUtc { get; set; }

    public List<ElementGroup> ElementGroups { get; set; } = new();
}
