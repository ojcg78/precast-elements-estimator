// TODO: adjust the namespace to match your existing project.
namespace PrecastEstimator.Api.Dtos;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ClientName { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ModifiedAtUtc { get; set; }
}

public class CreateOrUpdateProjectDto
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? ClientName { get; set; }
    public string? Status { get; set; }
}
