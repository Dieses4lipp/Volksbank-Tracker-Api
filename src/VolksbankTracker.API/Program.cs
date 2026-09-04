using System.Threading.RateLimiting;
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
builder.Services.AddScoped<AnomalyDetectionService>();

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("sync", opt =>
    {
        opt.PermitLimit = 1;
        opt.Window = TimeSpan.FromSeconds(30);
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
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
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
                    Id = "ApiKey"
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
app.UseRateLimiter();

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

// Matches only the endpoints that talk to the bank — not /api/sync/logs.
static bool IsBankCall(HttpContext ctx) =>
    ctx.Request.Path.Equals("/api/sync", StringComparison.OrdinalIgnoreCase) ||
    ctx.Request.Path.Equals("/api/sync/balance", StringComparison.OrdinalIgnoreCase);
