using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Api.Auth;

public static class AuthSetup
{
    /// <summary>The scheme the endpoints use; it hands each token to the scheme of its issuer.</summary>
    public const string Scheme = "authentik";

    /// <summary>
    /// The policy every write uses: a valid token from an accepted client whose <c>groups</c> claim
    /// holds that client's editor group or the admin group. Signed in without it: 403.
    /// </summary>
    public const string EditorPolicy = "editor";

    public static string SchemeFor(string client) => $"authentik:{client}";

    public static IServiceCollection AddAuthentikAuth(this IServiceCollection services, IConfiguration config)
    {
        var options = config.GetSection(AuthOptions.Section).Get<AuthOptions>() ?? new AuthOptions();
        services.Configure<AuthOptions>(config.GetSection(AuthOptions.Section));

        var builder = services.AddAuthentication(Scheme);
        var clients = options.Enabled ? options.Clients : ["none"];

        foreach (var client in clients)
        {
            builder.AddJwtBearer(SchemeFor(client), jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = client,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
                if (!options.Enabled)
                {
                    // Sign-in is off: no keys, so every token fails and writes get 401.
                    jwt.TokenValidationParameters.ValidIssuer = "sign-in-is-off";
                    jwt.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration();
                    return;
                }
                jwt.Authority = options.IssuerOf(client);
                jwt.TokenValidationParameters.ValidIssuer = options.IssuerOf(client);
                jwt.RequireHttpsMetadata = jwt.Authority.StartsWith("https://", StringComparison.Ordinal);
                if (options.InternalUrl.Length > 0)
                {
                    jwt.BackchannelHttpHandler = new InternalUrlHandler(new Uri(jwt.Authority), new Uri(options.InternalUrl));
                }
            });
        }

        builder.AddPolicyScheme(Scheme, "authentik", policy =>
        {
            policy.ForwardDefaultSelector = context => SchemeFor(PickClient(context, options, clients));
        });

        services.AddAuthorizationBuilder()
            .AddPolicy(EditorPolicy, p => p
                .AddAuthenticationSchemes(Scheme)
                .RequireAuthenticatedUser()
                .RequireAssertion(context => IsEditor(context.User, options)));

        return services;
    }

    /// <summary>In the issuing client's editor group, or an authentik admin.</summary>
    public static bool IsEditor(ClaimsPrincipal user, AuthOptions options)
    {
        var client = user.FindFirst("azp")?.Value ?? user.FindFirst("aud")?.Value;
        if (client is null) return false;
        var allowed = new[] { AuthOptions.EditorGroupOf(client), options.AdminGroup };
        return user.FindAll("groups").Any(group => allowed.Contains(group.Value, StringComparer.Ordinal));
    }

    /// <summary>The accepted client whose issuer signed this request's token (checked again by its scheme).</summary>
    private static string PickClient(HttpContext context, AuthOptions options, List<string> clients)
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (options.Enabled && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var handler = new JsonWebTokenHandler();
            var raw = header["Bearer ".Length..].Trim();
            if (handler.CanReadToken(raw))
            {
                try
                {
                    var issuer = handler.ReadJsonWebToken(raw).Issuer;
                    var match = clients.FirstOrDefault(c => options.IssuerOf(c) == issuer);
                    if (match is not null) return match;
                }
                catch (ArgumentException)
                {
                    // Not a readable JWT: the default scheme rejects it with 401.
                }
            }
        }
        return clients[0];
    }

    /// <summary>
    /// Sends the server-to-server calls (discovery and signing keys) for the public authentik
    /// address to <see cref="AuthOptions.InternalUrl"/> instead, with the public host in
    /// X-Forwarded-Host: authentik builds its issuer from the request's host.
    /// </summary>
    private sealed class InternalUrlHandler(Uri publicUrl, Uri internalUrl) : DelegatingHandler(new HttpClientHandler())
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.RequestUri is { } uri &&
                string.Equals(uri.Authority, publicUrl.Authority, StringComparison.OrdinalIgnoreCase))
            {
                request.RequestUri = new UriBuilder(uri)
                {
                    Scheme = internalUrl.Scheme,
                    Host = internalUrl.Host,
                    Port = internalUrl.Port,
                }.Uri;
                request.Headers.TryAddWithoutValidation("X-Forwarded-Host", publicUrl.Authority);
                request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", publicUrl.Scheme);
            }
            return base.SendAsync(request, ct);
        }
    }
}
