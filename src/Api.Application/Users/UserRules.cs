using System.Text.RegularExpressions;
using Api.Application.Users.Dtos;
using Api.Domain.Users;
using static Api.Domain.Users.UserLimits;

namespace Api.Application.Users;

/// <summary>
/// The validation rules, with the same limits and messages as the User Directory's form, so
/// errors can be shown next to its fields as they are.
/// </summary>
public static partial class UserRules
{
    [GeneratedRegex(@"^[\p{L}\p{M}][\p{L}\p{M}'’ .-]*$")]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"^\+?[\d\s().-]+$")]
    private static partial Regex PhonePattern();

    // The same pattern as zod's email check.
    [GeneratedRegex(@"^(?!\.)(?!.*\.\.)([A-Za-z0-9_'+\-\.]*)[A-Za-z0-9_+-]@([A-Za-z0-9][A-Za-z0-9\-]*\.)+[A-Za-z]{2,}$")]
    private static partial Regex EmailPattern();

    /// <summary>A valid, cleaned-up user, or the errors per field (camelCase keys).</summary>
    public static (User? User, Dictionary<string, string[]> Errors) Validate(UserInput input)
    {
        var errors = new Dictionary<string, string[]>();
        void Fail(string field, string message) => errors[field] = [message];

        var fullName = (input.FullName ?? "").Trim();
        if (fullName.Length < 2) Fail("fullName", "Name is required (at least 2 characters)");
        else if (fullName.Length > NameMax) Fail("fullName", $"Name must be {NameMax} characters or fewer");
        else if (!NamePattern().IsMatch(fullName)) Fail("fullName", "Use letters, spaces, hyphens or apostrophes only");

        var email = (input.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length == 0) Fail("email", "Email is required");
        else if (email.Length > EmailMax) Fail("email", "Email is too long");
        else if (!EmailPattern().IsMatch(email)) Fail("email", "Enter a valid email address, like name@example.com");

        var phone = (input.Phone ?? "").Trim();
        if (phone.Length > 0)
        {
            var digits = phone.Count(char.IsAsciiDigit);
            if (!PhonePattern().IsMatch(phone) || digits < 7 || digits > 15)
                Fail("phone", "Enter a valid phone number, like +44 7700 900123");
        }

        if (input.Age is not { } age) Fail("age", "Age is required");
        else if (age < MinAge) Fail("age", $"Must be {MinAge} or over");
        else if (age > MaxAge) Fail("age", $"Must be {MaxAge} or under");

        var country = (input.Country ?? "").Trim().ToUpperInvariant();
        if (!Countries.Names.ContainsKey(country)) Fail("country", "Choose a country");

        var city = Optional(input.City, CityMax, "City", "city");
        var jobTitle = Optional(input.JobTitle, JobTitleMax, "Job title", "jobTitle");
        var bio = Optional(input.Bio, BioMax, "Bio", "bio");

        var interests = (input.Interests ?? []).Distinct(StringComparer.Ordinal).ToList();
        if (interests.Count == 0) Fail("interests", "Pick at least one interest");
        else if (interests.Any(i => !Interests.Allowed.Contains(i))) Fail("interests", "Unknown interest");
        else if (interests.Count > Interests.Max) Fail("interests", $"Pick up to {Interests.Max} interests");

        if (errors.Count > 0) return (null, errors);

        return (new User
        {
            FullName = fullName,
            Email = email,
            Phone = phone.Length > 0 ? phone : null,
            Age = input.Age!.Value,
            Country = country,
            City = city,
            JobTitle = jobTitle,
            Bio = bio,
            Interests = interests,
        }, errors);

        string? Optional(string? value, int max, string label, string field)
        {
            var text = (value ?? "").Trim();
            if (text.Length > max) Fail(field, $"{label} must be {max} characters or fewer");
            return text.Length > 0 ? text : null;
        }
    }
}
