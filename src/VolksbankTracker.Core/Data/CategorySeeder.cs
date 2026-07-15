using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

/// <summary>
/// Single source of truth for the default categories, including their
/// matching keywords and roles (income / fallback). Changing values here
/// requires a new EF migration ("dotnet ef migrations add ..."), because the
/// data is baked into the model via HasData.
/// </summary>
public static class CategorySeeder
{
    public static readonly Category[] Categories =
    [
        new()
        {
            Id = 1, Name = "Einkommen", Icon = "💰", Color = "#22c55e",
            IsIncome = true,
            Keywords = "gehalt|lohn|entgelt|gutschrift arbeitgeber"
        },
        new()
        {
            Id = 2, Name = "Miete", Icon = "🏠", Color = "#ef4444",
            Keywords = "miete|nebenkosten"
        },
        new()
        {
            Id = 3, Name = "Lebensmittel", Icon = "🛒", Color = "#f97316",
            Keywords = "rewe|aldi|lidl|edeka|netto|kaufland|nah und gut|nah + gut|marktkauf|penny"
        },
        new()
        {
            Id = 4, Name = "Transport", Icon = "🚗", Color = "#3b82f6",
            Keywords = "tank|aral|shell|db bahn|deutsche bahn|vgn|öpnv|parken"
        },
        new()
        {
            Id = 5, Name = "Versicherungen", Icon = "🛡️", Color = "#8b5cf6",
            Keywords = "versicherung|allianz|huk|aok|tkk|barmer|gkv"
        },
        new()
        {
            Id = 6, Name = "Freizeit", Icon = "🎮", Color = "#ec4899",
            Keywords = "netflix|spotify|steam|amazon prime|disney|kino|restaurant|lieferando"
        },
        new()
        {
            Id = 7, Name = "Gesundheit", Icon = "💊", Color = "#14b8a6",
            Keywords = "apotheke|arzt|zahnarzt|xtra|fitnessstudio"
        },
        new()
        {
            Id = 8, Name = "Sonstiges", Icon = "📦", Color = "#6b7280",
            IsFallback = true,
            Keywords = ""
        },
    ];

    public static void Seed(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Category>().HasData(Categories);
}
