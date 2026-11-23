using Api.Application;
using Api.Auth;
using Api.Endpoints;
using Api.Infrastructure;
using Api.OpenApi;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);
builder.UseBuildTimeDocDefaults();

// The layers: use cases (Application), then PostgreSQL and the repositories (Infrastructure).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Numbers are numbers: "29" in quotes is refused, and the OpenAPI schema says integer.
builder.Services.ConfigureHttpJsonOptions(json =>
    json.SerializerOptions.NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict);

builder.Services.AddAuthentikAuth(builder.Configuration);
builder.Services.AddProblemDetails();
builder.Services.AddApiDocs();

// Anyone may call it from a browser. Tokens go in the Authorization header, never cookies.
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .WithHeaders("Authorization", "Content-Type")));

// Behind Coolify's proxy (or Caddy locally): trust its X-Forwarded-* headers, so links and the
// docs' server address use the public https URL. The container's port is never published.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapApiDocs();
app.MapHealthChecks("/health");

app.MapPeople();

// Bring the database up to date, then optionally add the demo people (not while the build writes
// the OpenAPI document: there is no database then).
var added = ApiDocs.IsBuildTime
    ? 0
    : await app.Services.PrepareDatabaseAsync(app.Configuration.GetValue<bool>("SEED_DEMO_DATA"));
if (added > 0) StartupLog.DemoDataAdded(app.Logger, added);

await app.RunAsync();

internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Demo data: added {Count} people.")]
    public static partial void DemoDataAdded(ILogger logger, int count);
}

/// <summary>The entry point, visible to the tests' WebApplicationFactory.</summary>
public partial class Program;
