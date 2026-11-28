namespace Api.Domain.Users;

/// <summary>
/// The size limits of a user's fields. Validation (<c>UserRules</c>) and the database columns
/// both use them; they match the User Directory's form.
/// </summary>
public static class UserLimits
{
    public const int NameMax = 80;
    public const int EmailMax = 254;
    public const int PhoneMax = 32;
    public const int JobTitleMax = 80;
    public const int CityMax = 80;
    public const int BioMax = 300;
    public const int MinAge = 18;
    public const int MaxAge = 120;
}
