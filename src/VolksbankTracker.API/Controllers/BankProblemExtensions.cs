using Microsoft.AspNetCore.Mvc;

namespace VolksbankTracker.API.Controllers;

internal static class BankProblemExtensions
{
    public static ObjectResult BankError(this ControllerBase controller, string? detail) =>
        controller.Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Bank communication failed",
            detail: detail);
}
