using System.Security.Cryptography;
using System.Text;

namespace VolksbankTracker.API;

/// <summary>
/// Requires the configured API key (config "Api:Key") in the X-Api-Key header
/// for every request. 
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
            await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = $"Missing or invalid {HeaderName} header."
            }, options: null, contentType: "application/problem+json",
               cancellationToken: context.RequestAborted);
            return;
        }

        await next(context);
    }
}
