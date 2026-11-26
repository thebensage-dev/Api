using System.Security.Claims;
using Api.Application.Common;
using Api.Application.People;
using Api.Application.People.Dtos;
using Api.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// The <c>/people</c> routes. Each handler only reads the request, calls <see cref="IPeopleService"/>
/// and turns its result into an HTTP response.
/// </summary>
public static class PeopleEndpoints
{
    public static RouteGroupBuilder MapPeople(this IEndpointRouteBuilder app)
    {
        var people = app.MapGroup("/people").WithTags("People");

        people.MapGet("/", List)
            .WithName("ListPeople")
            .WithSummary("List people")
            .WithDescription(
                "Every word of `q` must match, ignoring case and accents. Without `sort`, a search " +
                "puts the best matches first (a name word starting with the term, then the term " +
                "elsewhere in the name, then in any other field) and the list is otherwise newest first.");

        people.MapGet("/summary", Summary)
            .WithName("GetPeopleSummary")
            .WithSummary("Get summary")
            .WithDescription("Totals for the whole directory.");

        people.MapGet("/options", Options)
            .WithName("GetPeopleOptions")
            .WithSummary("Get options")
            .WithDescription("Allowed countries, interests, age ranges and sort orders.");

        people.MapGet("/audit-log", AuditLog)
            .WithName("GetPeopleAuditLog")
            .WithSummary("Get audit log")
            .WithDescription("Newest first: who added, edited or deleted whom.")
            .RequireAuthorization(AuthSetup.EditorPolicy);

        people.MapGet("/{id:int}", Get)
            .WithName("GetPerson")
            .WithSummary("Get person")
            .ProducesProblem(StatusCodes.Status404NotFound);

        people.MapPost("/", Create)
            .WithName("CreatePerson")
            .WithSummary("Add person")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthSetup.EditorPolicy);

        people.MapPut("/{id:int}", Update)
            .WithName("UpdatePerson")
            .WithSummary("Update person")
            .WithDescription("Replaces all of a person's details.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(AuthSetup.EditorPolicy);

        people.MapDelete("/{id:int}", Delete)
            .WithName("DeletePerson")
            .WithSummary("Delete person")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(AuthSetup.EditorPolicy);

        return people;
    }

    /// <param name="people">The people use cases.</param>
    /// <param name="q">Search words.</param>
    /// <param name="country">ISO 3166-1 alpha-2 code, e.g. GB.</param>
    /// <param name="age">An age range: 18-24, 25-34, 35-44, 45-54 or 55+.</param>
    /// <param name="interests">Interests the person must all have (repeat the parameter).</param>
    /// <param name="sort">id-desc, id-asc, name-asc, name-desc, age-asc, age-desc, country-asc or country-desc.</param>
    /// <param name="page">Page number, from 1.</param>
    /// <param name="size">People per page, 1 to 100.</param>
    /// <param name="ct">Cancelled when the caller goes away.</param>
    private static async Task<Ok<PersonPage>> List(
        IPeopleService people,
        string? q,
        string? country,
        string? age,
        [FromQuery] string[]? interests,
        string? sort,
        int? page,
        int? size,
        CancellationToken ct)
    {
        var filters = new PeopleFilters(
            q,
            country,
            age,
            interests ?? [],
            sort,
            page ?? 1,
            size ?? PeopleSearch.DefaultPageSize);
        return TypedResults.Ok(await people.ListAsync(filters, ct));
    }

    private static async Task<Ok<DirectorySummary>> Summary(IPeopleService people, CancellationToken ct) =>
        TypedResults.Ok(await people.GetSummaryAsync(ct));

    private static Ok<PeopleOptions> Options(IPeopleService people) => TypedResults.Ok(people.GetOptions());

    /// <param name="people">The people use cases.</param>
    /// <param name="limit">How many to return, 1 to 200.</param>
    /// <param name="ct">Cancelled when the caller goes away.</param>
    private static async Task<Ok<IReadOnlyList<PersonAuditEntryDto>>> AuditLog(
        IPeopleService people, int? limit, CancellationToken ct) =>
        TypedResults.Ok(await people.ListAuditLogAsync(limit, ct));

    private static async Task<Results<Ok<PersonDto>, ProblemHttpResult>> Get(
        int id, IPeopleService people, CancellationToken ct)
    {
        var result = await people.GetAsync(id, ct);
        return result.Failure is { } failure ? failure.ToProblem() : TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Created<PersonDto>, ValidationProblem, ProblemHttpResult>> Create(
        PersonInput input, IPeopleService people, ClaimsPrincipal user, CancellationToken ct)
    {
        var result = await people.CreateAsync(input, user.ToEditor(), ct);
        return result.Failure switch
        {
            { Kind: FailureKind.Validation } failure => failure.ToValidationProblem(),
            { } failure => failure.ToProblem(),
            null => TypedResults.Created($"/people/{result.Value.Id}", result.Value),
        };
    }

    private static async Task<Results<Ok<PersonDto>, ValidationProblem, ProblemHttpResult>> Update(
        int id, PersonInput input, IPeopleService people, ClaimsPrincipal user, CancellationToken ct)
    {
        var result = await people.UpdateAsync(id, input, user.ToEditor(), ct);
        return result.Failure switch
        {
            { Kind: FailureKind.Validation } failure => failure.ToValidationProblem(),
            { } failure => failure.ToProblem(),
            null => TypedResults.Ok(result.Value),
        };
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id, IPeopleService people, ClaimsPrincipal user, CancellationToken ct)
    {
        var result = await people.DeleteAsync(id, user.ToEditor(), ct);
        return result.Failure is { } failure ? failure.ToProblem() : TypedResults.NoContent();
    }
}
