using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Hash).IsUnique(); // deduplication
            e.HasOne(t => t.Category).WithMany(c => c.Transactions).HasForeignKey(t => t.CategoryId).IsRequired(false);
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Einkommen",      Icon = "💰", Color = "#22c55e" },
            new Category { Id = 2, Name = "Miete",          Icon = "🏠", Color = "#ef4444" },
            new Category { Id = 3, Name = "Lebensmittel",   Icon = "🛒", Color = "#f97316" },
            new Category { Id = 4, Name = "Transport",      Icon = "🚗", Color = "#3b82f6" },
            new Category { Id = 5, Name = "Versicherungen", Icon = "🛡️", Color = "#8b5cf6" },
            new Category { Id = 6, Name = "Freizeit",       Icon = "🎮", Color = "#ec4899" },
            new Category { Id = 7, Name = "Gesundheit",     Icon = "💊", Color = "#14b8a6" },
            new Category { Id = 8, Name = "Sonstiges",      Icon = "📦", Color = "#6b7280" }
        );
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
}

public class SyncLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TransactionsFetched { get; set; }
    public int TransactionsNew { get; set; }
    public string Status { get; set; } = "running"; // running | success | failed
    public string? Error { get; set; }
}
