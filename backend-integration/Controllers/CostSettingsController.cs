// TODO: adjust namespaces/usings to match your existing project.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrecastEstimator.Api.Dtos;
using PrecastEstimator.Api.Models;

namespace PrecastEstimator.Api.Controllers;

/// <summary>
/// Global, shared cost rates (concrete, steel, mesh, labour, etc.).
/// One row per rate key; the client works with the whole set as a flat
/// dictionary, so GET/PUT operate on the full set rather than per-key.
/// </summary>
[ApiController]
[Route("api/cost-settings")]
[Authorize] // TODO: confirm this matches how your other controllers require an Entra ID token (policy/scheme name, if any).
public class CostSettingsController : ControllerBase
{
    private readonly YourExistingDbContext _db; // TODO: replace with your real DbContext type.

    public CostSettingsController(YourExistingDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<CostSettingsDto>> Get(CancellationToken ct)
    {
        var rates = await _db.CostSettings.AsNoTracking()
            .ToDictionaryAsync(c => c.SettingKey, c => c.SettingValue, ct);
        return Ok(new CostSettingsDto { Rates = rates });
    }

    /// <summary>
    /// Replaces the full rate set. Last write wins — no conflict detection,
    /// matching the app's current (localStorage) behaviour.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Put([FromBody] CostSettingsDto dto, CancellationToken ct)
    {
        var who = User.Identity?.Name ?? "unknown"; // TODO: use whatever claim your app uses to identify the signed-in Entra ID user.
        var now = DateTime.UtcNow;

        var existing = await _db.CostSettings.ToDictionaryAsync(c => c.SettingKey, ct);

        foreach (var (key, value) in dto.Rates)
        {
            if (existing.TryGetValue(key, out var row))
            {
                row.SettingValue = value;
                row.ModifiedBy = who;
                row.ModifiedAtUtc = now;
            }
            else
            {
                _db.CostSettings.Add(new CostSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    ModifiedBy = who,
                    ModifiedAtUtc = now,
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
