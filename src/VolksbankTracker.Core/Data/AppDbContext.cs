using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>Apps share one Postgres cluster, so each owns a schema. SQLite has no schemas.</summary>
    public const string PostgresSchema = "volksbank";

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MerchantCategoryMap> MerchantCategoryMaps => Set<MerchantCategoryMap>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isPostgres = Database.IsNpgsql();
        if (isPostgres)
            modelBuilder.HasDefaultSchema(PostgresSchema);

        modelBuilder.Entity<AppSetting>().HasKey(s => s.Key);

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Hash).IsUnique(); // deduplication
            e.HasOne(t => t.Category).WithMany(c => c.Transactions).HasForeignKey(t => t.CategoryId).IsRequired(false);

            foreach (var date in new[] { e.Property(t => t.BookingDate), e.Property(t => t.ValueDate) })
            {
                date.HasConversion(DateTimeKindConverters.Unspecified);
                if (isPostgres)
                    date.HasColumnType("timestamp without time zone");
            }
            e.Property(t => t.ImportedAt).HasConversion(DateTimeKindConverters.Utc);
        });

        modelBuilder.Entity<SyncLog>(e =>
        {
            e.Property(l => l.StartedAt).HasConversion(DateTimeKindConverters.Utc);
            e.Property(l => l.CompletedAt).HasConversion(DateTimeKindConverters.Utc);
        });

        modelBuilder.Entity<MerchantCategoryMap>(e =>
        {
            e.HasIndex(m => m.MatchKey).IsUnique();
            e.HasOne(m => m.Category).WithMany().HasForeignKey(m => m.CategoryId).OnDelete(DeleteBehavior.Cascade);
        });

        CategorySeeder.Seed(modelBuilder);
    }

    public async Task<string?> GetSettingAsync(string key) =>
        (await AppSettings.FindAsync(key))?.Value;

    public async Task SetSettingAsync(string key, string value)
    {
        var row = await AppSettings.FindAsync(key);
        if (row is null)
            AppSettings.Add(new AppSetting { Key = key, Value = value });
        else
            row.Value = value;

        await SaveChangesAsync();
    }

    public async Task RemoveSettingAsync(string key)
    {
        if (await AppSettings.FindAsync(key) is not { } row)
            return;

        AppSettings.Remove(row);
        await SaveChangesAsync();
    }
}
