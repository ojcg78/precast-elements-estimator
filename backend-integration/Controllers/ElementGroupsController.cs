// TODO: adjust namespaces/usings to match your existing project.
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrecastEstimator.Api.Dtos;
using PrecastEstimator.Api.Models;

namespace PrecastEstimator.Api.Controllers;

/// <summary>
/// Walls/Columns groups added to a Project's summary (the app's
/// "Project Summary" page). Nested under /api/projects/{projectId} because
/// every group belongs to exactly one project.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/element-groups")]
[Authorize] // TODO: confirm this matches how your other controllers require an Entra ID token.
public class ElementGroupsController : ControllerBase
{
    private readonly YourExistingDbContext _db; // TODO: replace with your real DbContext type.

    public ElementGroupsController(YourExistingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<ElementGroupDto>>> GetAll(Guid projectId, CancellationToken ct)
    {
        var groups = await _db.ElementGroups.AsNoTracking()
            .Where(g => g.ProjectId == projectId)
            .OrderBy(g => g.CreatedAtUtc)
            .ToListAsync(ct);

        return Ok(groups.Select(ToDto).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<ElementGroupDto>> Create(Guid projectId, [FromBody] CreateOrUpdateElementGroupDto dto, CancellationToken ct)
    {
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId, ct);
        if (!projectExists) return NotFound($"Project {projectId} not found.");

        var who = User.Identity?.Name ?? "unknown"; // TODO: use whatever claim your app uses to identify the signed-in Entra ID user.
        var now = DateTime.UtcNow;

        var group = new ElementGroup
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            GroupId = dto.GroupId,
            ElementType = dto.ElementType,
            PricePerM3 = dto.PricePerM3,
            Total = dto.Total,
            DataJson = dto.Data.GetRawText(),
            CreatedBy = who,
            CreatedAtUtc = now,
            ModifiedBy = who,
            ModifiedAtUtc = now,
        };

        _db.ElementGroups.Add(group);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetAll), new { projectId }, ToDto(group));
    }

    /// <summary>Last write wins — no conflict detection.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid projectId, Guid id, [FromBody] CreateOrUpdateElementGroupDto dto, CancellationToken ct)
    {
        var group = await _db.ElementGroups.FirstOrDefaultAsync(g => g.Id == id && g.ProjectId == projectId, ct);
        if (group is null) return NotFound();

        group.GroupId = dto.GroupId;
        group.ElementType = dto.ElementType;
        group.PricePerM3 = dto.PricePerM3;
        group.Total = dto.Total;
        group.DataJson = dto.Data.GetRawText();
        group.ModifiedBy = User.Identity?.Name ?? "unknown"; // TODO: see note above.
        group.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Partial update used by the summary table's inline "rename group" field
    /// (editGroupId in index.html), so renaming doesn't require resending the
    /// whole DataJson payload.
    /// </summary>
    [HttpPatch("{id:guid}/group-id")]
    public async Task<IActionResult> Rename(Guid projectId, Guid id, [FromBody] string newGroupId, CancellationToken ct)
    {
        var group = await _db.ElementGroups.FirstOrDefaultAsync(g => g.Id == id && g.ProjectId == projectId, ct);
        if (group is null) return NotFound();

        group.GroupId = newGroupId;
        group.ModifiedBy = User.Identity?.Name ?? "unknown"; // TODO: see note above.
        group.ModifiedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid id, CancellationToken ct)
    {
        var group = await _db.ElementGroups.FirstOrDefaultAsync(g => g.Id == id && g.ProjectId == projectId, ct);
        if (group is null) return NotFound();

        _db.ElementGroups.Remove(group);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>Backs the "Clear Summary" button — removes every group in one call.</summary>
    [HttpDelete]
    public async Task<IActionResult> DeleteAll(Guid projectId, CancellationToken ct)
    {
        var groups = _db.ElementGroups.Where(g => g.ProjectId == projectId);
        _db.ElementGroups.RemoveRange(groups);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static ElementGroupDto ToDto(ElementGroup g) => new()
    {
        Id = g.Id,
        ProjectId = g.ProjectId,
        GroupId = g.GroupId,
        ElementType = g.ElementType,
        PricePerM3 = g.PricePerM3,
        Total = g.Total,
        Data = JsonDocument.Parse(g.DataJson).RootElement.Clone(),
        CreatedAtUtc = g.CreatedAtUtc,
        ModifiedAtUtc = g.ModifiedAtUtc,
    };
}
