// ============================================================================
// DbContextAdditions.cs
//
// This is NOT a standalone file to drop in — it shows what to add to your
// EXISTING DbContext class. Copy the DbSet properties and the OnModelCreating
// snippet into your real DbContext (whatever it's called in your project),
// then delete this file.
// ============================================================================

// TODO: adjust the using to match where you put the Models from this folder.
using PrecastEstimator.Api.Models;
using Microsoft.EntityFrameworkCore;

public partial class YourExistingDbContext // TODO: replace with your real DbContext class name
{
    // ---- Add these three DbSet properties to your DbContext ----
    public DbSet<CostSetting> CostSettings => Set<CostSetting>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ElementGroup> ElementGroups => Set<ElementGroup>();

    // ---- Add this inside your existing OnModelCreating(ModelBuilder modelBuilder) ----
    partial void ConfigurePrecastEstimatorModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CostSetting>(e =>
        {
            e.ToTable("CostSetting");
            e.HasKey(x => x.SettingKey);
            e.Property(x => x.SettingKey).HasMaxLength(100);
            e.Property(x => x.SettingValue).HasColumnType("decimal(18,4)");
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("Project");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(100);
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
        });

        modelBuilder.Entity<ElementGroup>(e =>
        {
            e.ToTable("ElementGroup");
            e.HasKey(x => x.Id);
            e.Property(x => x.GroupId).HasMaxLength(200).IsRequired();
            e.Property(x => x.ElementType).HasMaxLength(20).IsRequired();
            e.Property(x => x.PricePerM3).HasColumnType("decimal(18,4)");
            e.Property(x => x.Total).HasColumnType("decimal(18,2)");
            e.Property(x => x.DataJson).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(256);
            e.Property(x => x.ModifiedBy).HasMaxLength(256);
            e.HasOne(x => x.Project)
                .WithMany(p => p.ElementGroups)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // NOTE: `partial void ConfigurePrecastEstimatorModels(...)` above only works if your
    // OnModelCreating already declares and calls a matching partial method, or if you
    // just inline the three modelBuilder.Entity<>(...) blocks directly into your
    // existing OnModelCreating instead of using a partial method. The partial-method
    // split is here only so this file doesn't require you to paste over your whole
    // OnModelCreating body.
}
