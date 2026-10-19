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

public class Transaction
{
    public int Id { get; set; }
    public string Hash { get; set; } = "";          // SHA256 of date+amount+purpose+partner IBAN+EndToEndId for dedup
    public DateTime BookingDate { get; set; }
    public DateTime ValueDate { get; set; }
    public decimal Amount { get; set; }             // negative = expense, positive = income
    public string Currency { get; set; } = "EUR";
    public string Purpose { get; set; } = "";       // Verwendungszweck
    public string CreditorName { get; set; } = "";
    public string CreditorIban { get; set; } = "";
    public string DebtorName { get; set; } = "";
    public string DebtorIban { get; set; } = "";
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "#6b7280";
    public List<Transaction> Transactions { get; set; } = [];
    /// <summary>
    /// Transactions matching no merchant mapping land here.
    /// </summary>
    public bool IsFallback { get; set; }
    /// <summary>
    /// Transactions classified as savings (see ClassificationSettings) always land here.
    /// </summary>
    public bool IsSavings { get; set; }
    /// <summary>
    /// Incoming transactions in this category count as income in the statistics,
    /// in addition to the salary and cash-deposit rules in ClassificationSettings.
    /// </summary>
    public bool IsIncome { get; set; }
}

public class MerchantCategoryMap
{
    public int Id { get; set; }
    public string MatchKey { get; set; } = ""; // IBAN, or normalized name if no IBAN
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}

public class AppSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class SyncLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TransactionsFetched { get; set; }
    public int TransactionsNew { get; set; }
    public string Status { get; set; } = SyncStatus.Running; // see SyncStatus
    public string? Error { get; set; }
}
