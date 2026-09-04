# Integración con backend existente (SQL Server + Entra ID)

Estos archivos **no son un proyecto independiente**: son piezas para copiar dentro de tu API .NET existente (que ya usa Entity Framework Core y ya valida Entra ID). No sé el nombre real de tu `DbContext`, tu namespace, ni el nombre de tus proyectos .csproj, así que cada archivo trae comentarios `// TODO:` donde tienes que ajustar algo a tu proyecto real.

## Qué resuelve

Hoy `index.html` guarda todo en `localStorage` del navegador:
- Tarifas de costos (`precast-costs`)
- Resumen de proyecto / elementos agregados (`precast-summary`)

Con esto, esos datos pasan a vivir en SQL Server, compartidos entre todos los usuarios que entren con su cuenta de Entra ID.

## Pasos de integración

1. **Base de datos**: ejecuta `schema.sql` contra tu base SQL Server. Crea las tablas `CostSetting`, `Project`, `ElementGroup` y deja sembradas (`seed`) las tarifas actuales de `DEFAULT_COSTS`. Es seguro volver a ejecutarlo (usa `IF NOT EXISTS` / `MERGE`).

2. **Modelos** (`Models/CostSetting.cs`, `Models/Project.cs`, `Models/ElementGroup.cs`): cópialos a la carpeta de modelos/entidades de tu proyecto. Ajusta el `namespace` al tuyo.

3. **DbContext** (`DbContextAdditions.cs`): **no es un archivo para pegar tal cual** — muestra qué añadir a tu `DbContext` real:
   - Los tres `DbSet<>` (`CostSettings`, `Projects`, `ElementGroups`).
   - La configuración de `OnModelCreating` para las tres tablas (tipos `decimal`, longitudes de `NVARCHAR`, la relación `Project 1—N ElementGroup` con `OnDelete(Cascade)`).

   Después de copiarlo, genera y aplica una migración de EF Core:
   ```
   dotnet ef migrations add AddPrecastEstimatorSharedTables
   dotnet ef database update
   ```
   (Si prefieres administrar el esquema solo con `schema.sql` y no con migraciones de EF, usa `modelBuilder.Entity<>().ToTable(t => t.ExcludeFromMigrations())` en las tres entidades para que EF no intente recrear tablas que ya creaste a mano.)

4. **DTOs** (`Dtos/CostSettingDto.cs`, `Dtos/ProjectDto.cs`, `Dtos/ElementGroupDto.cs`): cópialos igual, ajustando namespace. `ElementGroupDto.Data`/`CreateOrUpdateElementGroupDto.Data` son `JsonElement` (no `string`) para que el JSON viaje sin escapar en el body de la request/response.

5. **Controllers** (`Controllers/CostSettingsController.cs`, `Controllers/ProjectsController.cs`, `Controllers/ElementGroupsController.cs`): cópialos a tu carpeta de controllers.
   - Reemplaza `YourExistingDbContext` por el nombre real de tu `DbContext` en los tres archivos (inyectado por constructor, como ya harán tus otros controllers).
   - Reemplaza `User.Identity?.Name` por el claim que uses hoy para identificar al usuario de Entra ID que llama a la API (por ejemplo `User.FindFirstValue("preferred_username")` o `User.FindFirstValue(ClaimTypes.Email)`), igual que ya haces en tus controllers existentes.
   - Revisa que `[Authorize]` (sin parámetros) sea correcto para tu configuración. Si usas una policy con nombre específico para tu esquema de Entra ID (por ejemplo `[Authorize(Policy = "RequireAuthenticatedUser")]`), cámbialo ahí.

6. **Nada que cambiar en `Program.cs`/`Startup.cs`** para autenticación — ya tienes Entra ID configurado. Si tu configuración de CORS restringe orígenes, asegúrate de permitir el origen donde sirves `index.html`.

## Endpoints resultantes

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/cost-settings` | Trae todas las tarifas (`{ rates: { "Steel Bars": 3.2, ... } }`) |
| PUT | `/api/cost-settings` | Reemplaza/actualiza el set completo de tarifas |
| GET | `/api/projects` | Lista de proyectos/tenders |
| POST | `/api/projects` | Crea un proyecto/tender |
| GET/PUT/DELETE | `/api/projects/{id}` | Leer/editar/borrar un proyecto |
| GET | `/api/projects/{projectId}/element-groups` | Elementos (Walls/Columns) agregados a ese proyecto |
| POST | `/api/projects/{projectId}/element-groups` | Agrega un elemento al resumen |
| PUT | `/api/projects/{projectId}/element-groups/{id}` | Edita un elemento existente |
| PATCH | `/api/projects/{projectId}/element-groups/{id}/group-id` | Solo renombra el "Group" (edición inline en la tabla resumen) |
| DELETE | `/api/projects/{projectId}/element-groups/{id}` | Elimina un elemento |
| DELETE | `/api/projects/{projectId}/element-groups` | Vacía el resumen completo del proyecto ("Clear Summary") |

Todo protegido con `[Authorize]` — un token de Entra ID válido es obligatorio para cualquiera de estos.

## Concurrencia

Se implementó "último guardado gana" (sin optimistic concurrency): si dos personas guardan casi al mismo tiempo, el último `PUT`/`PATCH` sobreescribe al anterior sin aviso. Si más adelante quieres detectar conflictos, se puede agregar una columna `ROWVERSION` a `ElementGroup`/`CostSetting` y validar `If-Match` en los `PUT`.

## Frontend (`index.html`)

El archivo `index.html` en la raíz del repo ya fue adaptado para llamar a estos endpoints en vez de `localStorage` (ver bloque `APP_CONFIG` cerca del inicio del `<script>` principal — ahí debes completar tu `apiBaseUrl`, `clientId` y `authority`/`tenantId` de Entra ID, y el `apiScope` que tu API expone). Revisa esa sección antes de desplegar.
