// TODO: adjust namespaces/usings to match your existing project.
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrecastEstimator.Api.Dtos;
using PrecastEstimator.Api.Models;

namespace PrecastEstimator.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize] // TODO: confirm this matches how your other controllers require an Entra ID token.
public class ProjectsController : ControllerBase
{
    private readonly YourExistingDbContext _db; // TODO: replace with your real DbContext type.

    public ProjectsController(YourExistingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll(CancellationToken ct)
    {
        var projects = await _db.Projects.AsNoTracking()
            .OrderByDescending(p => p.ModifiedAtUtc)
            .Select(p => ToDto(p))
            .ToListAsync(ct);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken ct)
    {
        var project = await _db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();
        return Ok(ToDto(project));
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateOrUpdateProjectDto dto, CancellationToken ct)
    {
        var who = User.Identity?.Name ?? "unknown"; // TODO: use whatever claim your app uses to identify the signed-in Entra ID user.
        var now = DateTime.UtcNow;

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Code = dto.Code,
            ClientName = dto.ClientName,
            Status = dto.Status,
            CreatedBy = who,
            CreatedAtUtc = now,
            ModifiedBy = who,
            ModifiedAtUtc = now,
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, ToDto(project));
    }

    /// <summary>Last write wins — no conflict detection.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateOrUpdateProjectDto dto, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();

        project.Name = dto.Name;
        project.Code = dto.Code;
        project.ClientName = dto.ClientName;
        project.Status = dto.Status;
        project.ModifiedBy = User.Identity?.Name ?? "unknown"; // TODO: see note above.
        project.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();

        _db.Projects.Remove(project); // cascades to ElementGroup rows (see schema.sql FK).
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ProjectDto ToDto(Project p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Code = p.Code,
        ClientName = p.ClientName,
        Status = p.Status,
        CreatedAtUtc = p.CreatedAtUtc,
        ModifiedAtUtc = p.ModifiedAtUtc,
    };
}
