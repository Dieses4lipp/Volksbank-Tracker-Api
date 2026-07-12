using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddProblemDetails();
builder.Services.Configure<FinTsConfig>(builder.Configuration.GetSection("FinTs"));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=tracker.db"));

builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<ClassificationSettingsService>();
builder.Services.AddScoped<CategorizationService>();
builder.Services.AddScoped<FinTsSyncService>();
builder.Services.AddScoped<AnomalyDetectionService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:3000")
     .AllowAnyMethod()
     .AllowAnyHeader()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await ctx.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

var txGroup = app.MapGroup("/api/transactions");

txGroup.MapGet("/", async (AppDbContext db, int page = 1, int pageSize = 50,
    int? categoryId = null, string? search = null, string? type = null) =>
{
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 200);

    var query = db.Transactions.Include(t => t.Category).AsQueryable();

    if (categoryId.HasValue)
        query = query.Where(t => t.CategoryId == categoryId);

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(t =>
            t.Purpose.Contains(search) ||
            t.CreditorName.Contains(search) ||
            t.DebtorName.Contains(search));

    if (type == "income")
        query = query.Where(t => t.Amount > 0);
    else if (type == "expense")
        query = query.Where(t => t.Amount < 0);

    var total = await query.CountAsync();
    var items = await query
        .OrderByDescending(t => t.BookingDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new { total, page, pageSize, items });
});

txGroup.MapPatch("/{id:int}/category", async (int id, [Microsoft.AspNetCore.Mvc.FromBody] UpdateCategoryRequest body, AppDbContext db) =>
{
    var t = await db.Transactions.FindAsync(id);
    if (t is null) return Results.NotFound();
    t.CategoryId = body.CategoryId;
    await db.SaveChangesAsync();
    return Results.Ok(t);
});




var statsGroup = app.MapGroup("/api/stats");

statsGroup.MapGet("/dashboard", async (StatisticsService stats) =>
    Results.Ok(await stats.GetDashboardStatsAsync()));

statsGroup.MapGet("/monthly", async (StatisticsService stats, int months = 24) =>
    Results.Ok(await stats.GetMonthlyBreakdownAsync(Math.Clamp(months, 1, 120))));

statsGroup.MapGet("/anomalies", async (AnomalyDetectionService svc, int months = 12, double threshold = 2.5) =>
    Results.Ok(await svc.DetectAsync(Math.Clamp(months, 1, 120), Math.Clamp(threshold, 0.5, 10))));

app.MapGet("/api/categories", async (AppDbContext db) =>
    Results.Ok(await db.Categories.ToListAsync()));

app.MapPut("/api/categories/{id:int}", async (int id, Category updated, AppDbContext db) =>
{
    var cat = await db.Categories.FindAsync(id);
    if (cat is null) return Results.NotFound();
    cat.Name = updated.Name;
    cat.Keywords = updated.Keywords;
    cat.Color = updated.Color;
    await db.SaveChangesAsync();
    return Results.Ok(cat);
});

var syncGroup = app.MapGroup("/api/sync");

static FinTsConfig RequireFinTsConfig(IOptions<FinTsConfig> options)
{
    var cfg = options.Value;
    if (string.IsNullOrWhiteSpace(cfg.BankUrl) ||
        string.IsNullOrWhiteSpace(cfg.BlZ) ||
        string.IsNullOrWhiteSpace(cfg.Iban))
        throw new InvalidOperationException(
            "FinTs configuration is incomplete (BankUrl, Blz, Iban required). Set them via user-secrets.");
    return cfg;
}

syncGroup.MapPost("/", async (IOptions<FinTsConfig> options, FinTsSyncService svc, SyncRequest? req) =>
{
    var result = await svc.SyncAsync(RequireFinTsConfig(options), req?.FromDate);
    return Results.Ok(result);
});

syncGroup.MapGet("/balance", async (IOptions<FinTsConfig> options, FinTsSyncService svc) =>
{
    var result = await svc.GetBalanceAsync(RequireFinTsConfig(options));
    return Results.Ok(result);
});

syncGroup.MapGet("/logs", async (AppDbContext db) =>
    Results.Ok(await db.SyncLogs.OrderByDescending(l => l.StartedAt).Take(20).ToListAsync()));

var settingsGroup = app.MapGroup("/api/settings");

settingsGroup.MapGet("/classification", async (ClassificationSettingsService svc) =>
    Results.Ok(await svc.GetAsync()));

settingsGroup.MapPut("/classification", async (ClassificationSettings body, ClassificationSettingsService svc) =>
    Results.Ok(await svc.SaveAsync(body)));

app.MapPost("/api/categories/recategorize", async (CategorizationService categorization) =>
{
    var recategorized = await categorization.RecategorizeAllAsync();
    return Results.Ok(new { recategorized });
});

app.Run();

public record SyncRequest(DateTime? FromDate);
public record UpdateCategoryRequest(int CategoryId);