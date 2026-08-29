using GenomeTrack.Application.Response;
using Microsoft.AspNetCore.Mvc;

namespace GenomeTrack.API.Extensions;

/// <summary>
/// The single place a <see cref="Result"/> becomes a status code. Controllers call this instead
/// of choosing codes themselves, so "not found" means 404 everywhere and a new endpoint cannot
/// quietly answer 200 for a failure.
/// </summary>
public static class ApiResultExtensions
{
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result);

        if (result.IsNotFound)
            return new NotFoundObjectResult(result);

        if (result.IsForbidden)
            return new ObjectResult(result) { StatusCode = StatusCodes.Status403Forbidden };

        if (result.IsConflict)
            return new ConflictObjectResult(result);

        return new BadRequestObjectResult(result);
    }
}
