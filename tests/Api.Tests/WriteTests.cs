using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Api.Application.People.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Api.Tests;

public sealed class WriteTests(ApiFactory api) : ApiTest(api)
{
    private static PersonInput Valid(string email = "new.person@example.com") => new()
    {
        FullName = "  Zoë O'Neil-Smith ",
        Email = $" {email.ToUpperInvariant()} ",
        Phone = "",
        Age = 30,
        Country = "gb",
        City = "York",
        JobTitle = "",
        Bio = null,
        Interests = ["Music", "Art"],
    };

    [Fact]
    public async Task Writes_need_a_token()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.PostAsJsonAsync("/people", Valid(), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.PutAsJsonAsync("/people/1", Valid(), Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.DeleteAsync("/people/1", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.GetAsync("/people/audit-log", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("wrong-audience")]
    [InlineData("unknown-issuer")]
    [InlineData("expired")]
    [InlineData("other-key")]
    [InlineData("not-a-jwt")]
    public async Task Refuses_bad_tokens(string kind)
    {
        var token = kind switch
        {
            "wrong-audience" => Api.Token(audience: "grafana"),
            "unknown-issuer" => Api.Token(issuer: "https://id.test/application/o/grafana/", audience: "grafana"),
            "expired" => Api.Token(expires: DateTime.UtcNow.AddMinutes(-5)),
            "other-key" => Api.Token(key: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test" }),
            _ => "abc.def.ghi",
        };
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/people/1");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Http.SendAsync(request, Ct)).StatusCode);
    }

    [Theory]
    [InlineData("api")]
    [InlineData("user-directory", "api-editors")]
    [InlineData("api", "user-directory-editors", "portfolio-members")]
    public async Task Refuses_signed_in_people_who_are_not_editors_for_that_app(string client, params string[] groups)
    {
        // authentik lets any account sign in to the apps; only the editor group may write.
        var token = Api.Token(client, groups: groups);
        foreach (var request in new[]
        {
            new HttpRequestMessage(HttpMethod.Post, "/people") { Content = JsonContent.Create(Valid()) },
            new HttpRequestMessage(HttpMethod.Put, "/people/1") { Content = JsonContent.Create(Valid()) },
            new HttpRequestMessage(HttpMethod.Delete, "/people/1"),
            new HttpRequestMessage(HttpMethod.Get, "/people/audit-log"),
        })
        {
            using (request)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                Assert.Equal(HttpStatusCode.Forbidden, (await Http.SendAsync(request, Ct)).StatusCode);
            }
        }
    }

    [Fact]
    public async Task Lets_authentik_admins_write_through_any_client()
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, "/people/1");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", Api.Token("user-directory", groups: ["authentik Admins"]));
        Assert.Equal(HttpStatusCode.NoContent, (await Http.SendAsync(request, Ct)).StatusCode);
    }

    [Theory]
    [InlineData("api")]
    [InlineData("user-directory")]
    public async Task Creates_a_person_cleaned_up_and_logs_who(string client)
    {
        using var http = Api.SignedIn(client);
        var response = await http.PostAsJsonAsync("/people", Valid(), Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var person = (await response.Content.ReadFromJsonAsync<PersonDto>(Ct))!;
        Assert.Equal($"/people/{person.Id}", response.Headers.Location?.ToString());
        Assert.Equal("Zoë O'Neil-Smith", person.FullName);
        Assert.Equal("new.person@example.com", person.Email);
        Assert.Equal("GB", person.Country);
        Assert.Null(person.Phone);
        Assert.Null(person.JobTitle);
        Assert.Equal(["Music", "Art"], person.Interests);

        // Searchable straight away, accents and hyphens ignored.
        var found = (await Http.GetFromJsonAsync<PersonPage>("/people?q=zoe%20smith", Ct))!;
        Assert.Equal([person.Id], found.Items.Select(p => p.Id));

        var log = (await http.GetFromJsonAsync<List<PersonAuditEntryDto>>("/people/audit-log", Ct))!;
        Assert.Equal(("created", person.Id, "Test Editor", client), (log[0].Action, log[0].PersonId, log[0].ByName, log[0].Client));
    }

    [Fact]
    public async Task Returns_every_field_error()
    {
        using var http = Api.SignedIn();
        var response = await http.PostAsJsonAsync("/people", new PersonInput
        {
            FullName = "X",
            Email = "not-an-email",
            Phone = "12",
            Age = 17,
            Country = "ZZ",
            Bio = new string('a', 301),
            Interests = ["Art", "Books", "Coding", "Cooking", "Fashion", "Fitness"],
        }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct))!;
        Assert.Equal("Please fix the highlighted fields.", problem.Title);
        Assert.Equal(
            new Dictionary<string, string>
            {
                ["fullName"] = "Name is required (at least 2 characters)",
                ["email"] = "Enter a valid email address, like name@example.com",
                ["phone"] = "Enter a valid phone number, like +44 7700 900123",
                ["age"] = "Must be 18 or over",
                ["country"] = "Choose a country",
                ["bio"] = "Bio must be 300 characters or fewer",
                ["interests"] = "Pick up to 5 interests",
            },
            problem.Errors.ToDictionary(e => e.Key, e => Assert.Single(e.Value)));

        var empty = await http.PostAsJsonAsync("/people", new PersonInput(), Ct);
        var errors = (await empty.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct))!.Errors;
        Assert.Equal(["Age is required"], errors["age"]);
        Assert.Equal(["Pick at least one interest"], errors["interests"]);
        Assert.Equal(["Email is required"], errors["email"]);
    }

    [Fact]
    public async Task A_taken_email_is_a_409_on_the_email_field()
    {
        using var http = Api.SignedIn();
        var create = await http.PostAsJsonAsync("/people", Valid("amara.okafor@example.com"), Ct);
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
        var problem = (await create.Content.ReadFromJsonAsync<ValidationProblemDetails>(Ct))!;
        Assert.Equal(["Another user already has this email"], problem.Errors["email"]);

        var update = await http.PutAsJsonAsync("/people/2", Valid("amara.okafor@example.com"), Ct);
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
    }

    [Fact]
    public async Task Updates_a_person()
    {
        using var http = Api.SignedIn();
        var before = (await Http.GetFromJsonAsync<PersonDto>("/people/1", Ct))!;
        var response = await http.PutAsJsonAsync("/people/1", Valid("amara.okafor@example.com") with { FullName = "Amara Okafor-Reid" }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = (await response.Content.ReadFromJsonAsync<PersonDto>(Ct))!;
        Assert.Equal("Amara Okafor-Reid", after.FullName);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.True(after.UpdatedAt > before.UpdatedAt);
        Assert.Equal("Amara Okafor-Reid", (await Http.GetFromJsonAsync<PersonDto>("/people/1", Ct))!.FullName);

        Assert.Equal(HttpStatusCode.NotFound, (await http.PutAsJsonAsync("/people/999", Valid(), Ct)).StatusCode);
    }

    [Fact]
    public async Task Deletes_a_person()
    {
        using var http = Api.SignedIn();
        Assert.Equal(HttpStatusCode.NoContent, (await http.DeleteAsync("/people/1", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Http.GetAsync("/people/1", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.DeleteAsync("/people/1", Ct)).StatusCode);

        var log = (await http.GetFromJsonAsync<List<PersonAuditEntryDto>>("/people/audit-log", Ct))!;
        Assert.Equal(("deleted", 1), (log[0].Action, log[0].PersonId));
    }
}
