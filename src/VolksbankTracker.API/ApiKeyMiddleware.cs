using System.Security.Cryptography;
using System.Text;

namespace VolksbankTracker.API;

/// <summary>
/// Requires the configured API key (config "Api:Key") in the X-Api-Key header
/// for every request. Registered only when a key is configured; in Production
/// startup fails without one (see Program.cs).
/// </summary>
public class ApiKeyMiddleware(RequestDelegate next, string apiKey)
{
    public const string HeaderName = "X-Api-Key";

    private readonly byte[] _keyBytes = Encoding.UTF8.GetBytes(apiKey);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var provided) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(provided.ToString()), _keyBytes))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = $"Missing or invalid {HeaderName} header."
            });
            return;
        }

        await next(context);
    }
}
