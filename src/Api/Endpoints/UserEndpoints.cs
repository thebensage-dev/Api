using System.Security.Claims;
using Api.Application.Common;
using Api.Application.Users;
using Api.Application.Users.Dtos;
using Api.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// The <c>/users</c> routes. Each handler only reads the request, calls <see cref="IUserService"/>
/// and turns its result into an HTTP response.
/// </summary>
public static class UserEndpoints
{
    public static RouteGroupBuilder MapUsers(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/users").WithTags("Users");

        users.MapGet("/", List)
            .WithName("ListUsers")
            .WithSummary("List users")
            .WithDescription(
                "Every word of `q` must match, ignoring case and accents. Without `sort`, a search " +
                "puts the best matches first (a name word starting with the term, then the term " +
                "elsewhere in the name, then in any other field) and the list is otherwise newest first.");

        users.MapGet("/summary", Summary)
            .WithName("GetUsersSummary")
            .WithSummary("Get summary")
            .WithDescription("Totals for the whole directory.");

        users.MapGet("/options", Options)
            .WithName("GetUsersOptions")
            .WithSummary("Get options")
            .WithDescription("Allowed countries, interests, age ranges and sort orders.");

        users.MapGet("/audit-log", AuditLog)
            .WithName("GetUsersAuditLog")
            .WithSummary("Get audit log")
            .WithDescription("Newest first: who added, edited or deleted whom.")
            .RequireAuthorization(AuthSetup.EditorPolicy);

        users.MapGet("/{id:int}", Get)
            .WithName("GetUser")
            .WithSummary("Get user")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/", Create)
            .WithName("CreateUser")
            .WithSummary("Add user")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthSetup.EditorPolicy);

        users.MapPut("/{id:int}", Update)
            .WithName("UpdateUser")
            .WithSummary("Update user")
            .WithDescription("Replaces all of a user's details.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthSetup.EditorPolicy);

        users.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUser")
            .WithSummary("Delete user")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthSetup.EditorPolicy);

        return users;
    }

    /// <param name="users">The users use cases.</param>
    /// <param name="q">Search words.</param>
    /// <param name="country">ISO 3166-1 alpha-2 code, e.g. GB.</param>
    /// <param name="age">An age range: 18-24, 25-34, 35-44, 45-54 or 55+.</param>
    /// <param name="interests">Interests the user must all have (repeat the parameter).</param>
    /// <param name="sort">id-desc, id-asc, name-asc, name-desc, age-asc, age-desc, country-asc or country-desc.</param>
    /// <param name="page">Page number, from 1.</param>
    /// <param name="size">Users per page, 1 to 100.</param>
    /// <param name="ct">Cancelled when the caller goes away.</param>
    private static async Task<Ok<UserPage>> List(
        IUserService users,
        string? q,
        string? country,
        string? age,
        [FromQuery] string[]? interests,
        string? sort,
        int? page,
        int? size,
        CancellationToken ct)
    {
        var filters = new UserFilters(
            q,
            country,
            age,
            interests ?? [],
            sort,
            page ?? 1,
            size ?? UserSearchRules.DefaultPageSize);
        return TypedResults.Ok(await users.ListAsync(filters, ct));
    }

    private static async Task<Ok<DirectorySummary>> Summary(IUserService users, CancellationToken ct) =>
        TypedResults.Ok(await users.GetSummaryAsync(ct));

    private static Ok<UserOptions> Options(IUserService users) => TypedResults.Ok(users.GetOptions());

    /// <param name="users">The users use cases.</param>
    /// <param name="limit">How many to return, 1 to 200.</param>
    /// <param name="ct">Cancelled when the caller goes away.</param>
    private static async Task<Ok<IReadOnlyList<UserAuditEntryDto>>> AuditLog(
        IUserService users, int? limit, CancellationToken ct) =>
        TypedResults.Ok(await users.ListAuditLogAsync(limit, ct));

    private static async Task<Results<Ok<UserDto>, ProblemHttpResult>> Get(
        int id, IUserService users, CancellationToken ct)
    {
        var result = await users.GetAsync(id, ct);
        return result.Failure is { } failure ? failure.ToProblem() : TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Created<UserDto>, ValidationProblem, ProblemHttpResult>> Create(
        UserInput input, IUserService users, ClaimsPrincipal user, CancellationToken ct)
    {
        var result = await users.CreateAsync(input, user.ToEditor(), ct);
        return result.Failure switch
        {
            { Kind: FailureKind.Validation } failure => failure.ToValidationProblem(),
            { } failure => failure.ToProblem(),
            null => TypedResults.Created($"/users/{result.Value.Id}", result.Value),
        };
    }

    private static async Task<Results<Ok<UserDto>, ValidationProblem, ProblemHttpResult>> Update(
        int id, UserInput input, IUserService users, ClaimsPrincipal user, CancellationToken ct)
    {
        var result = await users.UpdateAsync(id, input, user.ToEditor(), ct);
        return result.Failure switch
        {
            { Kind: FailureKind.Validation } failure => failure.ToValidationProblem(),
            { } failure => failure.ToProblem(),
            null => TypedResults.Ok(result.Value),
        };
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id, IUserService users, ClaimsPrincipal user, CancellationToken ct)
    {
        var result = await users.DeleteAsync(id, user.ToEditor(), ct);
        return result.Failure is { } failure ? failure.ToProblem() : TypedResults.NoContent();
    }
}
