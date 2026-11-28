# API

A general-use REST API built with **ASP.NET Core on .NET 10** (the LTS), **Entity Framework Core** on **PostgreSQL**, documented with **OpenAPI** and an interactive **Scalar** page, and secured with **authentik** (the same sign-in as the other sites). It runs at `api.<domain>`.

Its first resource is **users**: everything the User Directory needs to list, search, add, edit and delete users, so that app can read and write through this API instead of its own database. More resources can be added beside it (see [Adding a resource](#adding-a-resource)).

How the repos fit together, and the local stack that runs them all, are in [Infrastructure's README](https://github.com/thebensage-dev/Infrastructure). The User Directory reads and writes its users through this API.

## What you get

- **Interactive docs** at `/docs/` (Scalar), from the OpenAPI document at `/openapi/v1.json`. `/` redirects there. Every endpoint can be tried from the page; **Authentication > authentik** signs in for the write endpoints.
- **Public reads, editors' writes.** Anyone can read. Adding, editing and deleting need an authentik access token (`Authorization: Bearer …`) from an editor: someone in that app's editor group (`api-editors`, `user-directory-editors`) or an authentik admin, from the token's `groups` claim. Signed in without it: 403. Every change goes in an audit log with who made it (`GET /users/audit-log`, signed in).
- **Search in SQL**, multi-word and accent-insensitive ("siobhan" finds Siobhán), ranked by best match, with filters, sorting and paging.
- **Errors as RFC 9457 problem details**: validation errors per field (400), unknown ids (404), a taken email on the email field (409), missing or bad tokens (401).
- **Health** at `/health`, which also checks the database. Docker's healthcheck and Grafana use it.
- Migrations are applied on every start; the 12 demo users are added on request (`SEED_DEMO_DATA=true`).

## Endpoints

| Method and path        | Auth   | What                                                                                                                       |
| ---------------------- | ------ | -------------------------------------------------------------------------------------------------------------------------- |
| `GET /users`          | public | Search and list. Query: `q`, `country` (`GB`), `age` (`18-24` … `55+`), `interests` (repeat it; all must match), `sort`, `page`, `size` (1–100, default 12). |
| `GET /users/summary`  | public | Totals: users, average age, users per country and per interest.                                                         |
| `GET /users/options`  | public | The allowed countries, interests, age ranges and sort orders.                                                             |
| `GET /users/{id}`     | public | One user, or 404.                                                                                                       |
| `POST /users`         | token  | Add a user: 201 with a `Location` header, 400 per field, 409 for a taken email.                                         |
| `PUT /users/{id}`     | token  | Replace a user's details: 200, 400, 404 or 409.                                                                          |
| `DELETE /users/{id}`  | token  | 204, or 404.                                                                                                              |
| `GET /users/audit-log` | token | The audit log, newest first (`limit`, up to 200): who added, edited or deleted whom, and through which app. |

The exact shapes are in the OpenAPI document. A user:

```json
{
  "id": 1,
  "fullName": "Amara Okafor",
  "email": "amara.okafor@example.com",
  "phone": "+44 7700 900123",
  "age": 29,
  "country": "GB",
  "city": "Manchester",
  "jobTitle": "Frontend developer",
  "bio": "Builds accessible design systems and runs a weekend coding club for teenagers.",
  "interests": ["Coding", "Music", "Volunteering"],
  "createdAt": "2026-07-25T10:12:39.227492+00:00",
  "updatedAt": "2026-07-25T10:12:39.227492+00:00"
}
```

**Search ranking** (with `q` and no `sort`): every word must match. Per word, a name word starting with it scores 3, the word elsewhere in the name 2, and the word in the email, job, city, country name or code, bio or interests 1. The highest total comes first, then by name. Without `q` the list is newest first.

**Validation** is the same as the User Directory's form, with the same messages: name 2–80 letters (spaces, hyphens, apostrophes and dots allowed), a valid email (stored lower case, unique), an optional phone of 7–15 digits, age 18–120, a known ISO country code, optional city and job title (80) and bio (300), and 1–5 interests from the fixed list. Optional text left empty is stored as `null`.

## Sign-in (authentik)

authentik gives each application its own issuer, `https://id.<domain>/application/o/<app>/`, and puts the app's client id in the token's audience. The API accepts tokens from the apps listed in `Auth:Clients` (`src/Api/appsettings.json`):

- **`api`**: this API's own authentik app, a public client with PKCE (no secret), used by the docs page. Infrastructure's blueprint creates it. Any authentik account may sign in (single sign-on across the sites); only `api-editors` and authentik admins may write.
- **`user-directory`**: the User Directory's app. That app forwards its signed-in user's access token, so its editors (`user-directory-editors`) can save through the API without a second sign-in.

authentik lets every account sign in to these apps, so the API decides who may write (`AuthSetup.IsEditor`): the token's `groups` claim must hold `<client>-editors` for the client that issued it (by convention, `AuthOptions.EditorGroupOf`), or `Auth:AdminGroup` (`authentik Admins`). Anyone else gets a 403. Users ask for a group on authentik's shared "Request access" page, and you approve by adding them to it.

Signing keys are fetched from authentik and cached. `Auth:Authority` (env `Auth__Authority`) is authentik's OAuth base, `https://id.<domain>/application/o/`; leave it empty and every write gets a 401. `Auth:InternalUrl` is only for the local Docker stack, where `id.localhost` isn't reachable from inside a container.

## Prerequisites

- **.NET 10 SDK** (`global.json` accepts any 10.0 feature band).
- **Docker Desktop**: the tests start a throwaway PostgreSQL with Testcontainers, and the easiest database for `dotnet run` is a container too.

## Getting started

```powershell
docker run -d --name api-db -p 5432:5432 -e POSTGRES_DB=api -e POSTGRES_USER=api -e POSTGRES_PASSWORD=local-only-not-a-secret-api-db -v api-db:/var/lib/postgresql postgres:18.7-alpine
dotnet run --project src/Api   # http://localhost:3002/docs/
```

`appsettings.Development.json` points at that database and adds the demo users. Sign-in is off in development unless you set `Auth__Authority` (for example to the local stack's `http://id.localhost:8080/application/o/`).

To run it with everything else (authentik included, so sign-in works), use `docker compose up --build -d` in `Infrastructure/local`: the API is at http://api.localhost:8080.

## Commands

| Command                                                         | What                                                                        |
| --------------------------------------------------------------- | --------------------------------------------------------------------------- |
| `dotnet build`                                                  | Build; every warning is an error.                                           |
| `dotnet format`                                                 | Format the code (`--verify-no-changes` checks, as CI does).                 |
| `dotnet test`                                                   | The tests, against a real PostgreSQL in Docker.                             |
| `dotnet tool restore`                                           | Installs `dotnet ef` (pinned in `.config/dotnet-tools.json`).               |
| `dotnet ef migrations add <Name> -p src/Api.Infrastructure -s src/Api -o Persistence/Migrations` | A migration after a model change. Commit it; it's applied on the next start. |

## Project structure

Clean architecture: four projects, each depending only on the ones below it.

```
src/
  Api/                      the web host (references Application and Infrastructure)
    Program.cs              wires the layers, middleware, health check, migrate-on-start
    Auth/                   authentik tokens (one scheme per accepted app), the token's user as an Editor
    Endpoints/              minimal API routes: read the request, call a service, map its Result to HTTP
    OpenApi/                the OpenAPI document, its security scheme and the Scalar docs page
  Api.Application/          use cases (references Domain only)
    Common/                 Result / Failure (validation, not found, conflict) and Editor (who changes things)
    Users/                 UserService, IUserRepository, UserRules (validation), search and sort types
      Dtos/                 what the API takes and returns
  Api.Domain/               entities and their rules (references nothing)
    Common/                 Text: search normalisation
    Users/                 User, UserAuditEntry, countries, interests and field limits
  Api.Infrastructure/       PostgreSQL through EF Core (references Application)
    Persistence/            AppDbContext, entity configurations, migrations, the demo users
      Repositories/         UserRepository and the search/sort queries it runs in SQL
tests/Api.Tests/            WebApplicationFactory + Testcontainers PostgreSQL, tokens signed with a local key
openapi/openapi.json        the contract: written by every build of src/Api, committed
clients/typescript/         @thebensage-dev/api-client, the typed Node client generated from it
```

A request goes endpoint → service → repository → database. Endpoints never touch `AppDbContext`, services never touch EF Core or HTTP, and the domain knows nothing of either.

### Adding a resource

1. **Domain:** the entity (and any fixed lists or limits) in `src/Api.Domain/<Resource>/`.
2. **Application:** in `src/Api.Application/<Resource>/` the DTOs, the validation rules, an `I<Resource>Repository` interface and a service (interface + class) that returns `Result`s; register the service in `Application/DependencyInjection.cs`.
3. **Infrastructure:** an `IEntityTypeConfiguration` in `Persistence/Configurations/` and a `DbSet` on `AppDbContext`, the repository in `Persistence/Repositories/` (registered in `Infrastructure/DependencyInjection.cs`), then a migration.
4. **Api:** a `Map…` extension in `Endpoints/` (a route group with tags) called from `Program.cs`, with `.RequireAuthorization(AuthSetup.EditorPolicy)` on every write.

Tests go in `tests/Api.Tests`.

## Clients

The OpenAPI document is the contract. Every build of `src/Api` writes it to `openapi/openapi.json` (Microsoft.Extensions.ApiDescription.Server; no database or authentik needed), and the clients are generated from it, so they can't drift from the API.

- **Node: `@thebensage-dev/api-client`** (`clients/typescript`, see its README). Typed paths, bodies and responses; you pass a `getToken()` callback and it sends the authentik token only to the operations that need it. Published to GitHub Packages by the "API client" workflow when its version changes. UserDirectory uses it.
- **NuGet, later:** generate a C# client from the same file with Kiota (`kiota generate -l CSharp -d openapi/openapi.json`) in `clients/dotnet`, and pack it the same way. Not needed until a .NET app calls the API.

After changing an endpoint or a DTO: build, run `npm run generate` in `clients/typescript`, bump its version, and commit `openapi/` with it.

## CI

The same pipeline as every repo (Infrastructure's README, "Deploying"). **Check** runs a locked restore, `dotnet format --verify-no-changes`, a Release build (warnings are errors), a check that the migrations match the model, the tests and `docker compose config`. The `API client` workflow publishes `@thebensage-dev/api-client` when its version changes. Dependabot keeps the .NET packages and images on 10.x and PostgreSQL on 18 (their next majors need the next LTS).

## Docker

The `Dockerfile` publishes the API on `mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra` (about 250 MB; the `-extra` variant has ICU, which accent-insensitive search needs). It runs as the image's non-root user on port 8080 and applies migrations on start. `compose.yaml` is the production stack: the API image plus `postgres:18.7-alpine` with the `api_db` volume.

## Deploying

Coolify app: **Docker Compose** named `api`, this repo, `compose.yaml` (the API and its PostgreSQL), domain of the `api` service `https://api.<domain>:8080`, auto deploy off. The pipeline sets `API_IMAGE_TAG` and derives every setting from `DOMAIN`; Coolify generates the database password (`SERVICE_PASSWORD_64_APIDB`). Set the repo variable `SEED_DEMO_DATA=true` for a deploy that should add the 12 demo users, then delete it. Back up the `api_db` volume, and never rename it.

## Licence

MIT, see [`LICENSE`](LICENSE). Copyright (c) 2026 TheBensage.
