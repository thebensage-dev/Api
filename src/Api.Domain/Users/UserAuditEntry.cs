namespace Api.Domain.Users;

/// <summary>One line of the audit log: who added, edited or deleted a user, through which app, and when.</summary>
public sealed class UserAuditEntry
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Deleted = "deleted";

    public long Id { get; set; }
    /// <summary>Not a foreign key, so the log outlives a deleted user.</summary>
    public int UserId { get; set; }
    /// <summary><see cref="Created"/>, <see cref="Updated"/> or <see cref="Deleted"/>.</summary>
    public required string Action { get; set; }
    public DateTimeOffset At { get; set; }
    /// <summary>authentik's id for the user (the token's "sub").</summary>
    public required string BySubject { get; set; }
    public required string ByName { get; set; }
    /// <summary>The OIDC client the token was issued to, such as "api" or "user-directory".</summary>
    public required string Client { get; set; }
}
