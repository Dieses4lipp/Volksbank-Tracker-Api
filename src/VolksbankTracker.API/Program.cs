using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler =
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

app.MapControllers();

app.Run();
