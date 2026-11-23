using Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Api.OpenApi;

/// <summary>
/// Describes authentik sign-in in the OpenAPI document (OAuth 2 authorization code with PKCE),
/// and marks the endpoints that need it, so the docs page can sign in and send the token.
/// </summary>
internal sealed class OpenApiAuth(IOptions<AuthOptions> auth) : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    public const string SchemeName = "authentik";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        var options = auth.Value;
        if (!options.Enabled) return Task.CompletedTask;

        var authority = options.Authority.TrimEnd('/');
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Sign in with authentik. Writes need it; reads don't.",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority}/authorize/"),
                    TokenUrl = new Uri($"{authority}/token/"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "Who you are",
                        ["profile"] = "Your name",
                        ["email"] = "Your email address",
                    },
                },
            },
        };
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var needsAuth = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();
        if (!needsAuth || !auth.Value.Enabled) return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = ["openid", "profile", "email"],
        });
        operation.Responses ??= [];
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No valid authentik token." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Signed in, but not an editor for this app (ask for access on the sign-in page)." });
        return Task.CompletedTask;
    }
}
