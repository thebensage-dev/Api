using System.Collections.Frozen;

namespace Api.Domain.People;

/// <summary>The interests a person can pick: 1 to <see cref="Max"/> of them.</summary>
public static class Interests
{
    public const int Max = 5;

    public static readonly string[] All =
    [
        "Art", "Books", "Coding", "Cooking", "Fashion", "Fitness", "Gaming", "Gardening",
        "Hiking", "Movies", "Music", "Photography", "Science", "Sports", "Travel", "Volunteering",
    ];

    public static readonly FrozenSet<string> Allowed = All.ToFrozenSet(StringComparer.Ordinal);
}
