using System.Reflection;
using Api.Auth;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace Api.OpenApi;

/// <summary>The OpenAPI document (<c>/openapi/v1.json</c>) and the Scalar docs page (<c>/docs/</c>).</summary>
public static class ApiDocs
{
    public const string Title = "TheBensage API";

    /// <summary>True while the build is writing the OpenAPI document, not serving requests.</summary>
    public static bool IsBuildTime { get; } = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

    /// <summary>
    /// When the build writes <c>openapi/openapi.json</c> (Microsoft.Extensions.ApiDescription.Server
    /// runs the app without starting it), give it placeholder settings: no database is needed, and
    /// sign-in is on so the document describes the authentik scheme. The authority is the same
    /// placeholder as in .env.example; the live document at <c>/openapi/v1.json</c> uses the real one.
    /// </summary>
    public static WebApplicationBuilder UseBuildTimeDocDefaults(this WebApplicationBuilder builder)
    {
        if (!IsBuildTime) return builder;

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = "Host=unused-at-build-time",
            ["Auth:Authority"] = "https://id.example.com/application/o/",
        });
        return builder;
    }

    public static IServiceCollection AddApiDocs(this IServiceCollection services) =>
        services.AddOpenApi(openApi =>
        {
            openApi.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = Title;
                document.Info.Description =
                    "A general-use REST API. Reading is open to everyone; adding, editing and deleting " +
                    "need an authentik sign-in (the Authentication button on this page).";
                return Task.CompletedTask;
            });
            openApi.AddDocumentTransformer<OpenApiAuth>();
            openApi.AddOperationTransformer<OpenApiAuth>();
        });

    public static WebApplication MapApiDocs(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/docs", (options, context) =>
        {
            var auth = context.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value;
            options.WithTitle(Title).AddPreferredSecuritySchemes(OpenApiAuth.SchemeName);
            // Just the docs: no Scalar telemetry, AI chat or hosted-service buttons.
            options.Telemetry = false;
            options.Agent = new ScalarAgentOptions { Disabled = true };
            options.ShowDeveloperTools = DeveloperToolsVisibility.Never;
            options.DotNetFlag = false;
            if (auth.Enabled)
            {
                var origin = $"{context.Request.Scheme}://{context.Request.Host}";
                options.AddAuthorizationCodeFlow(OpenApiAuth.SchemeName, flow => flow
                    .WithClientId(auth.DocsClient)
                    .WithPkce(Pkce.Sha256)
                    .WithRedirectUri($"{origin}/docs/")
                    .WithSelectedScopes(["openid", "profile", "email"]));
            }
        });
        app.MapGet("/", () => Results.Redirect("/docs/")).ExcludeFromDescription();
        return app;
    }
}
