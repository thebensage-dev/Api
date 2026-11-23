# API

A general-use REST API built with **ASP.NET Core on .NET 10** (the LTS), **Entity Framework Core** on **PostgreSQL**, documented with **OpenAPI** and an interactive **Scalar** page, and secured with **authentik** (the same sign-in as the other sites). It runs at `api.<domain>`.

Its first resource is **people**: everything the User Directory needs to list, search, add, edit and delete people, so that app can read and write through this API instead of its own database. More resources can be added beside it (see [Adding a resource](#adding-a-resource)).

## How it fits with the other repos

| Repo           | Role                                                                                                                                          |
| -------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| **Api** (this) | The API at `api.<domain>`, with its own PostgreSQL.                                                                                           |
| UserDirectory  | Calls the people endpoints from its server, and forwards the signed-in person's authentik token on writes.                                    |
| Portfolio      | The main site; it can showcase this API and link to its docs.                                                                                 |
| Infrastructure | authentik (the `api` app the docs page signs in with) and Grafana, which checks `/health` here. Its `local/compose.yaml` runs everything locally. |
| SharedUI       | Not used here: the API has no UI of its own beyond the Scalar docs.                                                                           |

All five are sibling folders under `Documents\GitHub`.

## What you get

- **Interactive docs** at `/docs/` (Scalar), from the OpenAPI document at `/openapi/v1.json`. `/` redirects there. Every endpoint can be tried from the page; **Authentication > authentik** signs in for the write endpoints.
- **Public reads, editors' writes.** Anyone can read. Adding, editing and deleting need an authentik access token (`Authorization: Bearer …`) from an editor: someone in that app's editor group (`api-editors`, `user-directory-editors`) or an authentik admin, from the token's `groups` claim. Signed in without it: 403. Every change goes in an audit log with who made it (`GET /people/audit-log`, signed in).
- **Search in SQL**, multi-word and accent-insensitive ("siobhan" finds Siobhán), ranked by best match, with filters, sorting and paging.
- **Errors as RFC 9457 problem details**: validation errors per field (400), unknown ids (404), a taken email on the email field (409), missing or bad tokens (401).
- **Health** at `/health`, which also checks the database. Docker's healthcheck and Grafana use it.
- Migrations are applied on every start; the 12 demo people are added on request (`SEED_DEMO_DATA=true`).

## Endpoints

| Method and path        | Auth   | What                                                                                                                       |
| ---------------------- | ------ | -------------------------------------------------------------------------------------------------------------------------- |
| `GET /people`          | public | Search and list. Query: `q`, `country` (`GB`), `age` (`18-24` … `55+`), `interests` (repeat it; all must match), `sort`, `page`, `size` (1–100, default 12). |
| `GET /people/summary`  | public | Totals: people, average age, people per country and per interest.                                                         |
| `GET /people/options`  | public | The allowed countries, interests, age ranges and sort orders.                                                             |
| `GET /people/{id}`     | public | One person, or 404.                                                                                                       |
| `POST /people`         | token  | Add a person: 201 with a `Location` header, 400 per field, 409 for a taken email.                                         |
| `PUT /people/{id}`     | token  | Replace a person's details: 200, 400, 404 or 409.                                                                          |
| `DELETE /people/{id}`  | token  | 204, or 404.                                                                                                              |
| `GET /people/audit-log` | token | The audit log, newest first (`limit`, up to 200): who added, edited or deleted whom, and through which app. |

The exact shapes are in the OpenAPI document. A person:

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

authentik lets every account sign in to these apps, so the API decides who may write (`AuthSetup.IsEditor`): the token's `groups` claim must hold `<client>-editors` for the client that issued it (by convention, `AuthOptions.EditorGroupOf`), or `Auth:AdminGroup` (`authentik Admins`). Anyone else gets a 403. People ask for a group on authentik's shared "Request access" page, and you approve by adding them to it.

Signing keys are fetched from authentik and cached. `Auth:Authority` (env `Auth__Authority`) is authentik's OAuth base, `https://id.<domain>/application/o/`; leave it empty and every write gets a 401. `Auth:InternalUrl` is only for the local Docker stack, where `id.localhost` isn't reachable from inside a container.

## Prerequisites

- **.NET 10 SDK** (`global.json` accepts any 10.0 feature band).
- **Docker Desktop**: the tests start a throwaway PostgreSQL with Testcontainers, and the easiest database for `dotnet run` is a container too.

## Getting started

```powershell
docker run -d --name api-db -p 5432:5432 -e POSTGRES_DB=api -e POSTGRES_USER=api -e POSTGRES_PASSWORD=local-only-not-a-secret-api-db -v api-db:/var/lib/postgresql postgres:18.7-alpine
dotnet run --project src/Api   # http://localhost:3002/docs/
```

`appsettings.Development.json` points at that database and adds the demo people. Sign-in is off in development unless you set `Auth__Authority` (for example to the local stack's `http://id.localhost:8080/application/o/`).

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
    People/                 PeopleService, IPeopleRepository, PersonRules (validation), search and sort types
      Dtos/                 what the API takes and returns
  Api.Domain/               entities and their rules (references nothing)
    Common/                 Text: search normalisation
    People/                 Person, PersonChange, countries, interests and field limits
  Api.Infrastructure/       PostgreSQL through EF Core (references Application)
    Persistence/            AppDbContext, entity configurations, migrations, the demo people
      Repositories/         PeopleRepository and the search/sort queries it runs in SQL
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

One workflow, `.github/workflows/ci.yml` ("CI"), with the same shape as the other repos:

| Job                   | Runs on                    | What it does                                                                                                                                                                 |
| --------------------- | -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Check**             | every PR and push          | Locked restore, `dotnet format --verify-no-changes`, a Release build (warnings are errors), a check that the migrations match the model, the tests, and `docker compose config`. |
| **Build image**       | push to `main`, manual run | Builds the Dockerfile and pushes `ghcr.io/thebensage-dev/api:sha-<commit>` (and `:main`), keeps the 10 newest, then checks whether Coolify is ready.                         |
| **Deploy to Coolify** | after your approval        | Only when Coolify is ready: writes the settings and image tag into the Coolify app `api` through Infrastructure's shared deploy step, deploys that commit, and waits.        |

`.github/dependabot.yml` opens weekly PRs for NuGet packages, the Dockerfile and compose images, and actions. The Microsoft and Npgsql packages and the .NET images stay on the 10.x line (their 11.x needs .NET 11, which isn't LTS), and PostgreSQL stays on 18: Dependabot ignores those majors. Move them together with the next LTS.

## Docker

The `Dockerfile` publishes the API on `mcr.microsoft.com/dotnet/aspnet:10.0-alpine-extra` (about 250 MB; the `-extra` variant has ICU, which accent-insensitive search needs). It runs as the image's non-root user on port 8080 and applies migrations on start. `compose.yaml` is the production stack: the API image plus `postgres:18.7-alpine` with the `api_db` volume.

## Deploying (Coolify)

Deploys go through the pipeline, never Coolify's auto-deploy. On every push to `main`: **check**, then **image** pushes `ghcr.io/thebensage-dev/api:sha-<commit>`, then **deploy** waits for your approval and runs Infrastructure's shared deploy action. Because the API and its database deploy together, the Coolify app is a **Docker Compose** app built from this repo's `compose.yaml` at the deployed commit, and the action sets `API_IMAGE_TAG` to that commit's image.

**GitHub values:** only `DOMAIN` (org variable) and `COOLIFY_TOKEN` (`production` environment secret), both set by the script in Infrastructure's README ("Every value for all five repos at once"). `AUTH_AUTHORITY` is derived from `DOMAIN`. Optional: `SEED_DEMO_DATA` (repo variable, `true` for the first deploy only), `COOLIFY_URL` and `COOLIFY_APP_UUID` (overrides). The database password is a Coolify magic variable (`SERVICE_PASSWORD_64_APIDB`), generated on the first deploy and never in GitHub.

**One-time setup in Coolify:** New resource > **Docker Compose**, repo `thebensage-dev/Api`, branch `main`, base directory `/`, compose file `compose.yaml`, named `api`. Set the domain of the `api` service to `https://api.example.com:8080`. Auto deploy off. If the image on ghcr.io is private, the server needs the one-time `docker login ghcr.io` (Infrastructure README, "Coolify can pull the app images").

Back up the `api_db` volume like authentik's. Never rename it: Coolify would start an empty one.

## Licence

MIT, see [`LICENSE`](LICENSE). Copyright (c) 2026 TheBensage.
