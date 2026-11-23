using Api.Application.Common;

namespace Api.Application.People;

/// <summary>The ways a people use case can fail, with the words the caller sees.</summary>
public static class PersonErrors
{
    public static readonly Failure NotFound = new(FailureKind.NotFound, "That person doesn't exist.");

    /// <summary>Shaped like a validation error, so the email field can show it.</summary>
    public static readonly Failure DuplicateEmail = new(
        FailureKind.Conflict,
        "That email is already in use.",
        new Dictionary<string, string[]> { ["email"] = ["Another user already has this email"] });

    public static Failure Invalid(IReadOnlyDictionary<string, string[]> fields) =>
        new(FailureKind.Validation, "Please fix the highlighted fields.", fields);
}
