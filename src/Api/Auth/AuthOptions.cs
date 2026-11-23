namespace Api.Auth;

/// <summary>
/// Sign-in through authentik. Reads are public; every write needs an access token issued by one
/// of <see cref="Clients"/>. authentik gives each application its own issuer,
/// <c>{Authority}{client}/</c>, and the token's audience is the client id. authentik lets any
/// account sign in to the apps (single sign-on), so the token's <c>groups</c> claim decides who
/// may write: <see cref="EditorGroupOf"/> for the client that issued it, or <see cref="AdminGroup"/>.
/// </summary>
public sealed class AuthOptions
{
    public const string Section = "Auth";

    /// <summary>
    /// authentik's OAuth base, ending in a slash, e.g. <c>https://id.example.com/application/o/</c>.
    /// Empty: sign-in is off and every write is refused with 401.
    /// </summary>
    public string Authority { get; set; } = "";

    /// <summary>
    /// The authentik applications (slug = client id) whose tokens are accepted: this API's own
    /// ("api", used by the docs' sign-in), and apps that forward their user's token, like the
    /// User Directory.
    /// </summary>
    public List<string> Clients { get; set; } = [];

    /// <summary>
    /// Optional. Where this server reaches authentik when the public address doesn't work from
    /// inside its container (the local Docker stack: <c>http://authentik-server:9000</c>).
    /// </summary>
    public string InternalUrl { get; set; } = "";

    /// <summary>The client the docs page (Scalar) signs in with: a public client with PKCE.</summary>
    public string DocsClient { get; set; } = "api";

    /// <summary>authentik's own admins, who may write through any client.</summary>
    public string AdminGroup { get; set; } = "authentik Admins";

    /// <summary>
    /// The group that may write with this client's tokens, by convention <c>{client}-editors</c>:
    /// <c>api-editors</c> for the docs page, <c>user-directory-editors</c> for the User Directory.
    /// Approving someone's request for that app in authentik adds them to it.
    /// </summary>
    public static string EditorGroupOf(string client) => $"{client}-editors";

    public bool Enabled => Authority.Length > 0 && Clients.Count > 0;

    public string IssuerOf(string client) => $"{Authority.TrimEnd('/')}/{client}/";
}
