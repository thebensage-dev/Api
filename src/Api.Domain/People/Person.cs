using Api.Domain.Common;

namespace Api.Domain.People;

/// <summary>A person in the directory, as stored.</summary>
public sealed class Person
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    /// <summary>Lower case and unique.</summary>
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public int Age { get; set; }
    /// <summary>ISO 3166-1 alpha-2 code, such as "GB".</summary>
    public required string Country { get; set; }
    public string? City { get; set; }
    public string? JobTitle { get; set; }
    public string? Bio { get; set; }
    public List<string> Interests { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Written on every save from the fields above, so search and sorting run in SQL.
    /// <summary>The full name, normalised (lower case, no accents).</summary>
    public string SearchName { get; set; } = "";
    /// <summary>Every other searchable field, normalised: email, job, city, country name, bio, interests.</summary>
    public string SearchText { get; set; } = "";
    /// <summary>The country's English name, for sorting.</summary>
    public string CountryName { get; set; } = "";

    /// <summary>Stamps a new person as created (and last updated) at <paramref name="at"/>.</summary>
    public void MarkCreated(DateTimeOffset at)
    {
        CreatedAt = UpdatedAt = at;
        RefreshSearchFields();
    }

    /// <summary>Replaces every editable field with those of <paramref name="details"/>.</summary>
    public void UpdateDetails(Person details, DateTimeOffset at)
    {
        FullName = details.FullName;
        Email = details.Email;
        Phone = details.Phone;
        Age = details.Age;
        Country = details.Country;
        City = details.City;
        JobTitle = details.JobTitle;
        Bio = details.Bio;
        Interests = details.Interests;
        UpdatedAt = at;
        RefreshSearchFields();
    }

    /// <summary>Rewrites the derived search columns. Call it whenever a searchable field changes.</summary>
    public void RefreshSearchFields()
    {
        CountryName = Countries.NameOf(Country);
        SearchName = Text.Normalise(FullName);
        SearchText = Text.Normalise(string.Join(' ',
            new[] { Email, JobTitle, City, CountryName, Bio, string.Join(' ', Interests) }
                .Where(s => !string.IsNullOrEmpty(s))));
    }
}
