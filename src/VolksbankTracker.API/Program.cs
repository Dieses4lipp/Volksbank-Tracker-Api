using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
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

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");

// Only the endpoints that open a real FinTS session are limited: a retry loop
// against the bank risks throttling or an account lockout. The concurrency
// limit prevents two parallel syncs sharing one set of credentials.
// /api/sync/logs reads local SQLite and stays unlimited.
var syncLimits = builder.Configuration.GetSection("RateLimiting:Sync");
var permitLimit = syncLimits.GetValue("PermitLimit", 5);
var window = TimeSpan.FromMinutes(syncLimits.GetValue("WindowMinutes", 5.0));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    o.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            IsBankCall(ctx)
                ? RateLimitPartition.GetFixedWindowLimiter("sync", _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = 0
                    })
                : RateLimitPartition.GetNoLimiter<string>("unlimited")),
        PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
            IsBankCall(ctx)
                ? RateLimitPartition.GetConcurrencyLimiter("sync", _ =>
                    new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = 0 })
                : RateLimitPartition.GetNoLimiter<string>("unlimited")));

    o.OnRejected = async (ctx, ct) =>
    {
        // The fixed-window limiter supplies Retry-After; the concurrency limiter does not.
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);

        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        // Pass the content type to WriteAsJsonAsync — setting Response.ContentType
        // beforehand does not survive, it overwrites the header with application/json.
        await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Too many requests",
            Status = StatusCodes.Status429TooManyRequests,
            Detail = "Sync endpoints are rate limited to protect the bank connection."
        }, options: null, contentType: "application/problem+json", cancellationToken: ct);
    };
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
