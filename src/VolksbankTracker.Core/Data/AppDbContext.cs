using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSetting>().HasKey(s => s.Key);

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Hash).IsUnique(); // deduplication
            e.HasOne(t => t.Category).WithMany(c => c.Transactions).HasForeignKey(t => t.CategoryId).IsRequired(false);
        });

        CategorySeeder.Seed(modelBuilder);
    }
}

public class Transaction
{
    public int Id { get; set; }
    public string Hash { get; set; } = "";          // SHA256 of date+amount+purpose for dedup
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
    public string Keywords { get; set; } = "";
    /// <summary>Positive transactions are matched against income categories first.</summary>
    public bool IsIncome { get; set; }
    /// <summary>Transactions matching no keywords land here.</summary>
    public bool IsFallback { get; set; }
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
