using Api.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Endpoints;

/// <summary>Turns a use case's <see cref="Failure"/> into problem details.</summary>
public static class FailureResults
{
    /// <summary>400 with the errors per field.</summary>
    public static ValidationProblem ToValidationProblem(this Failure failure) =>
        TypedResults.ValidationProblem(failure.Fields ?? new Dictionary<string, string[]>(), title: failure.Title);

    /// <summary>
    /// 404 or 409. A 409 with field errors is shaped like a validation error, so a form can
    /// show it on the field (a taken email).
    /// </summary>
    public static ProblemHttpResult ToProblem(this Failure failure)
    {
        var status = failure.Kind switch
        {
            FailureKind.NotFound => StatusCodes.Status404NotFound,
            FailureKind.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return failure.Fields is { } fields
            ? TypedResults.Problem(new HttpValidationProblemDetails(fields.ToDictionary())
            {
                Title = failure.Title,
                Status = status,
            })
            : TypedResults.Problem(title: failure.Title, statusCode: status);
    }
}
