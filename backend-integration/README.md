# Integrating with your existing backend (SQL Server + Entra ID)

These files are **not a standalone project** — they're pieces to copy into your existing .NET API (which already uses Entity Framework Core and already validates Entra ID). I don't know the real name of your `DbContext`, your namespace, or your `.csproj` project names, so every file carries `// TODO:` comments wherever something needs adjusting to match your real project.

## What this solves

Today `index.html` stores everything in the browser's `localStorage`:
- Cost rates (`precast-costs`)
- Project summary / added elements (`precast-summary`)

With this, that data moves into SQL Server, shared across everyone who signs in with their Entra ID account.

## Integration steps

1. **Database**: run `schema.sql` against your SQL Server database. It creates the `CostSetting`, `Project`, and `ElementGroup` tables and seeds the current `DEFAULT_COSTS` rates. Safe to re-run (uses `IF NOT EXISTS` / `MERGE`).

2. **Models** (`Models/CostSetting.cs`, `Models/Project.cs`, `Models/ElementGroup.cs`): copy these into your project's models/entities folder. Adjust the `namespace` to match yours.

3. **DbContext** (`DbContextAdditions.cs`): **not a file to paste as-is** — it shows what to add to your real `DbContext`:
   - The three `DbSet<>` properties (`CostSettings`, `Projects`, `ElementGroups`).
   - The `OnModelCreating` configuration for the three tables (`decimal` types, `NVARCHAR` lengths, the `Project` 1—N `ElementGroup` relationship with `OnDelete(Cascade)`).

   After copying it, generate and apply an EF Core migration:
   ```
   dotnet ef migrations add AddPrecastEstimatorSharedTables
   dotnet ef database update
   ```
   (If you'd rather manage the schema only via `schema.sql` and not EF migrations, use `modelBuilder.Entity<>().ToTable(t => t.ExcludeFromMigrations())` on the three entities so EF doesn't try to recreate tables you already created by hand.)

4. **DTOs** (`Dtos/CostSettingDto.cs`, `Dtos/ProjectDto.cs`, `Dtos/ElementGroupDto.cs`): copy these too, adjusting the namespace. `ElementGroupDto.Data`/`CreateOrUpdateElementGroupDto.Data` are `JsonElement` (not `string`) so the JSON travels unescaped in the request/response body.

5. **Controllers** (`Controllers/CostSettingsController.cs`, `Controllers/ProjectsController.cs`, `Controllers/ElementGroupsController.cs`): copy these into your controllers folder.
   - Replace `YourExistingDbContext` with the real name of your `DbContext` in all three files (constructor-injected, same as your other controllers).
   - Replace `User.Identity?.Name` with whichever claim you currently use to identify the signed-in Entra ID user calling the API (for example `User.FindFirstValue("preferred_username")` or `User.FindFirstValue(ClaimTypes.Email)`), matching what your existing controllers already do.
   - Check that plain `[Authorize]` is correct for your setup. If you use a named policy for your Entra ID scheme (e.g. `[Authorize(Policy = "RequireAuthenticatedUser")]`), change it there.

6. **Nothing to change in `Program.cs`/`Startup.cs`** for authentication — you already have Entra ID configured. If your CORS configuration restricts origins, make sure it allows the origin `index.html` is served from.

## Resulting endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/cost-settings` | Fetches all rates (`{ rates: { "Steel Bars": 3.2, ... } }`) |
| PUT | `/api/cost-settings` | Replaces/updates the full rate set |
| GET | `/api/projects` | List of projects/tenders |
| POST | `/api/projects` | Creates a project/tender |
| GET/PUT/DELETE | `/api/projects/{id}` | Read/edit/delete a project |
| GET | `/api/projects/{projectId}/element-groups` | Elements (Walls/Columns) added to that project |
| POST | `/api/projects/{projectId}/element-groups` | Adds an element to the summary |
| PUT | `/api/projects/{projectId}/element-groups/{id}` | Edits an existing element |
| PATCH | `/api/projects/{projectId}/element-groups/{id}/group-id` | Renames only the "Group" (inline edit in the summary table) |
| DELETE | `/api/projects/{projectId}/element-groups/{id}` | Deletes an element |
| DELETE | `/api/projects/{projectId}/element-groups` | Clears the whole project summary ("Clear Summary") |

All protected with `[Authorize]` — a valid Entra ID token is required for any of these.

## Concurrency

This implements "last write wins" (no optimistic concurrency): if two people save at nearly the same time, the last `PUT`/`PATCH` overwrites the previous one without warning. If you later want to detect conflicts, a `ROWVERSION` column could be added to `ElementGroup`/`CostSetting` and checked via `If-Match` on the `PUT`s.

## Frontend (`index.html`)

`index.html` at the repo root has already been adapted to call these endpoints instead of `localStorage` (see the `APP_CONFIG` block near the top of the main `<script>` — that's where you fill in your `apiBaseUrl`, Entra ID `clientId`, `authority`/tenant ID, and the `apiScope` your API exposes). Check that section before deploying.
