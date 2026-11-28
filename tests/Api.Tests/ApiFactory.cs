using System.Net.Http.Headers;
using System.Security.Cryptography;
using Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Api.Tests.ApiFactory))]

namespace Api.Tests;

/// <summary>
/// The real app against a real PostgreSQL (a throwaway container, so Docker must be running),
/// with authentik replaced by a local signing key: tokens come from <see cref="Token"/>.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Authority = "https://id.test/application/o/";

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder("postgres:18.7-alpine").Build();
    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test" };

    public async ValueTask InitializeAsync() => await _db.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _db.GetConnectionString());
        builder.UseSetting("Auth:Authority", Authority);
        builder.UseSetting("SEED_DEMO_DATA", "false");
        builder.ConfigureTestServices(services =>
            services.ConfigureAll<JwtBearerOptions>(jwt =>
            {
                // The keys authentik would publish, without asking it.
                var config = new OpenIdConnectConfiguration();
                config.SigningKeys.Add(_key);
                jwt.Configuration = config;
            }));
    }

    /// <summary>An access token as authentik would issue it to <paramref name="client"/>.</summary>
    public string Token(
        string client = "api",
        string? issuer = null,
        string? audience = null,
        DateTime? expires = null,
        SecurityKey? key = null,
        string[]? groups = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? $"{Authority}{client}/",
            Audience = audience ?? client,
            NotBefore = (expires ?? DateTime.UtcNow.AddMinutes(5)).AddHours(-1),
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                ["sub"] = "test-user",
                ["name"] = "Test Editor",
                ["azp"] = client,
                // An editor for this client unless the test says otherwise.
                ["groups"] = groups ?? [$"{client}-editors"],
            },
            SigningCredentials = new SigningCredentials(key ?? _key, SecurityAlgorithms.RsaSha256),
        });

    public HttpClient SignedIn(string client = "api")
    {
        var http = CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(client));
        return http;
    }

    /// <summary>Empties the tables and adds the 12 demo users again.</summary>
    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("TRUNCATE users, user_audit_log RESTART IDENTITY");
        await DemoData.SeedAsync(db, TimeProvider.System);
    }
}

/// <summary>Every test class starts from the 12 demo users. Classes run one after another.</summary>
public abstract class ApiTest(ApiFactory api) : IAsyncLifetime
{
    protected ApiFactory Api { get; } = api;
    protected HttpClient Http { get; } = api.CreateClient();
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Api.ResetAsync();

    public ValueTask DisposeAsync()
    {
        Http.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
