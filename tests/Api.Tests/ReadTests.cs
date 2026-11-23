using System.Net;
using System.Net.Http.Json;
using Api.Application.People.Dtos;

namespace Api.Tests;

public sealed class ReadTests(ApiFactory api) : ApiTest(api)
{
    private async Task<PersonPage> List(string query = "") =>
        (await Http.GetFromJsonAsync<PersonPage>($"/people{query}", Ct))!;

    private static string[] Names(PersonPage page) => [.. page.Items.Select(p => p.FullName)];

    [Fact]
    public async Task Lists_newest_first_twelve_per_page()
    {
        var page = await List();
        Assert.Equal(12, page.Total);
        Assert.Equal("Olivia Brown", page.Items[0].FullName);
        Assert.Equal((1, 1, 1, 12), (page.Page, page.PageCount, page.From, page.To));
    }

    [Theory]
    [InlineData("siobhan", "Siobhán Murphy")]
    [InlineData("SOFÍA", "Sofia Martínez")]
    [InlineData("são", "Mateus Silva")]
    public async Task Search_ignores_case_and_accents(string q, string expected)
    {
        var page = await List($"?q={Uri.EscapeDataString(q)}");
        Assert.Equal([expected], Names(page));
    }

    [Fact]
    public async Task Every_search_word_must_match()
    {
        var page = await List("?q=coding%20gb");
        Assert.Equal(["Noah Patel", "Amara Okafor"], Names(page).Order().Reverse());
        Assert.Equal(["coding", "gb"], page.Terms);
    }

    [Fact]
    public async Task Best_matches_come_first()
    {
        // "art" is inside Martínez's name (2) and only an interest for the others (1).
        var page = await List("?q=art");
        Assert.Equal("Sofia Martínez", page.Items[0].FullName);
        Assert.Equal(4, page.Total);

        // A name word starting with the term (3) beats the term inside a name (2).
        var li = await List("?q=li");
        Assert.Equal("Liam Chen", li.Items[0].FullName);
    }

    [Fact]
    public async Task Filters_by_country_age_and_all_interests()
    {
        Assert.Equal(["Noah Patel", "Amara Okafor"], Names(await List("?country=gb")));
        Assert.Equal(["Olivia Brown"], Names(await List("?age=55%2B")));
        Assert.Equal(["Grace Hopper", "Noah Patel"], Names(await List("?interests=Coding&interests=Science")));
    }

    [Theory]
    [InlineData("name-asc", "Amara Okafor")]
    [InlineData("name-desc", "Yuki Tanaka")]
    [InlineData("age-asc", "Mateus Silva")]
    [InlineData("age-desc", "Olivia Brown")]
    [InlineData("country-asc", "Olivia Brown")]
    [InlineData("country-desc", "Grace Hopper")]
    [InlineData("id-asc", "Amara Okafor")]
    public async Task Sorts(string sort, string first)
    {
        Assert.Equal(first, (await List($"?sort={sort}")).Items[0].FullName);
    }

    [Fact]
    public async Task Pages_and_clamps_the_page_number()
    {
        var last = await List("?size=5&page=3");
        Assert.Equal((3, 3, 11, 12, 2), (last.Page, last.PageCount, last.From, last.To, last.Items.Count));

        var beyond = await List("?size=5&page=99");
        Assert.Equal(3, beyond.Page);

        var none = await List("?q=nobody-matches-this");
        Assert.Equal((0, 1, 0, 0), (none.Total, none.PageCount, none.From, none.To));
    }

    [Fact]
    public async Task Summarises_the_directory()
    {
        var summary = (await Http.GetFromJsonAsync<DirectorySummary>("/people/summary", Ct))!;
        Assert.Equal(12, summary.People);
        Assert.Equal(36, summary.AverageAge);
        Assert.Equal(new CountryCount("AU", "Australia", 1), summary.Countries[0]);
        Assert.Equal(new CountryCount("GB", "United Kingdom", 2), summary.Countries.Single(c => c.Code == "GB"));
        Assert.Equal(new InterestCount("Books", 4), summary.Interests[0]);
    }

    [Fact]
    public async Task Gets_one_person_or_404()
    {
        var person = await Http.GetFromJsonAsync<PersonDto>("/people/1", Ct);
        Assert.Equal("Amara Okafor", person!.FullName);
        Assert.Equal(["Coding", "Music", "Volunteering"], person.Interests);

        var missing = await Http.GetAsync("/people/999", Ct);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("application/problem+json", missing.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Lists_the_allowed_values()
    {
        var options = (await Http.GetFromJsonAsync<PeopleOptions>("/people/options", Ct))!;
        Assert.Contains(new CountryOption("GB", "United Kingdom"), options.Countries);
        Assert.Equal(16, options.Interests.Count);
        Assert.Equal(5, options.MaxInterests);
        Assert.Equal("55+", options.AgeRanges[^1].Value);
    }

    [Fact]
    public async Task Reports_health_and_serves_the_docs()
    {
        Assert.Equal("Healthy", await Http.GetStringAsync("/health", Ct));
        var doc = await Http.GetStringAsync("/openapi/v1.json", Ct);
        Assert.Contains("\"authorizationCode\"", doc, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await Http.GetAsync("/docs/", Ct)).StatusCode);
    }

    [Theory]
    [InlineData("/docs/")]
    [InlineData("/people")]
    [InlineData("/health")]
    [InlineData("/no-such-page")]
    public async Task Every_response_asks_search_engines_not_to_index(string path)
    {
        var response = await Http.GetAsync(path, Ct);
        Assert.Equal("noindex, nofollow", string.Join(", ", response.Headers.GetValues("X-Robots-Tag")));
    }
}
