using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

/// <summary>
/// Single source of truth for the default categories, including the
/// fallback role. Changing values here requires a new EF migration
/// ("dotnet ef migrations add ..."), because the data is baked into the
/// model via HasData.
/// </summary>
public static class CategorySeeder
{
    public static readonly Category[] Categories =
    [
        new() { Id = 1, Name = "Einkommen", Icon = "💰", Color = "#22c55e" },
        new() { Id = 2, Name = "Miete", Icon = "🏠", Color = "#ef4444" },
        new() { Id = 3, Name = "Lebensmittel", Icon = "🛒", Color = "#f97316" },
        new() { Id = 4, Name = "Transport", Icon = "🚗", Color = "#3b82f6" },
        new() { Id = 5, Name = "Versicherungen", Icon = "🛡️", Color = "#8b5cf6" },
        new() { Id = 6, Name = "Freizeit", Icon = "🎮", Color = "#ec4899" },
        new() { Id = 7, Name = "Gesundheit", Icon = "💊", Color = "#14b8a6" },
        new()
        {
            Id = 8, Name = "Sonstiges", Icon = "📦", Color = "#6b7280",
            IsFallback = true
        },
    ];

    public static Category Fallback => Categories.Single(c => c.IsFallback);

    public static void Seed(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Category>().HasData(Categories);
}
