using System.Security.Claims;
using Api.Application.Common;

namespace Api.Auth;

public static class EditorClaims
{
    /// <summary>Who is making a change, from the token's claims (kept under their JWT names).</summary>
    public static Editor ToEditor(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? "unknown";
        var name = user.FindFirstValue("name") ?? user.FindFirstValue("preferred_username")
            ?? user.FindFirstValue("email") ?? subject;
        var client = user.FindFirstValue("azp") ?? user.FindFirstValue("aud") ?? "unknown";
        return new Editor(subject, name, client);
    }
}
