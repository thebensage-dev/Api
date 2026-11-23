namespace Api.Application.Common;

/// <summary>Who is making a change: the signed-in user and the app their token was issued to.</summary>
/// <param name="Subject">authentik's id for the user (the token's "sub").</param>
/// <param name="Name">Their display name.</param>
/// <param name="Client">The OIDC client the token was issued to, such as "api" or "user-directory".</param>
public sealed record Editor(string Subject, string Name, string Client);
