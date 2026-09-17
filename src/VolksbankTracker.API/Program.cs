using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using VolksbankTracker.API;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.Configure<FinTsConfig>(builder.Configuration.GetSection("FinTs"));

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=tracker.db"));

builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<ClassificationSettingsService>();
builder.Services.AddScoped<CategorizationService>();
builder.Services.AddScoped<FinTsSyncService>();
builder.Services.AddScoped<FinTsCredentialsService>();
builder.Services.AddScoped<AnomalyDetectionService>();

// Encrypts the FinTS credentials stored via PUT /api/settings/fints. Keep the key ring
// outside the database folder: losing it makes stored credentials unreadable.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("VolksbankTracker");
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
    if (OperatingSystem.IsWindows())
        dataProtection.ProtectKeysWithDpapi();
}

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    AddFixedWindow(RateLimitPolicies.Sync, permitLimit: 1, TimeSpan.FromSeconds(30));
    AddFixedWindow(RateLimitPolicies.Credentials, permitLimit: 3, TimeSpan.FromMinutes(10));

    void AddFixedWindow(string policy, int permitLimit, TimeSpan window) =>
        o.AddFixedWindowLimiter(policy, opt =>
        {
            opt.PermitLimit = permitLimit;
            opt.Window = window;
            opt.QueueLimit = 0;
        });
});

builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173", "http://localhost:3000")
     .AllowAnyMethod()
     .AllowAnyHeader()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    const string apiKeyScheme = "ApiKey";
    c.AddSecurityDefinition(apiKeyScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = ApiKeyMiddleware.HeaderName,
        Description = "API key. Only required if Api:Key is configured."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = apiKeyScheme
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var apiKey = app.Configuration["Api:Key"];
if (string.IsNullOrWhiteSpace(apiKey) && !app.Environment.IsDevelopment())
    throw new InvalidOperationException(
        "Api:Key must be configured outside Development (user-secrets or environment variable Api__Key).");

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await ctx.Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// /health is exempt: probes (Docker HEALTHCHECK, uptime monitors) cannot carry the key.
if (!string.IsNullOrWhiteSpace(apiKey))
    app.UseWhen(
        ctx => !ctx.Request.Path.StartsWithSegments("/health"),
        b => b.UseMiddleware<ApiKeyMiddleware>(apiKey));

// After the key check, so unauthenticated requests cannot burn the window budget.
app.UseRateLimiter();

// Default response writer: plain "Healthy"/"Unhealthy" with 200/503, no check details.
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
