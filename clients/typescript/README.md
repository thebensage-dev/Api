# @thebensage-dev/api-client

A typed client for TheBensage API. It's generated from the API's OpenAPI document (`openapi/openapi.json` in this repo), so every path, parameter, body and response type comes straight from the API. It's about 6 kB of runtime ([openapi-fetch](https://openapi-ts.dev/openapi-fetch/)) with no other dependencies.

## Install

It's on GitHub Packages, like `@thebensage-dev/ui`. A project's `.npmrc` (gitignored) needs a token with `read:packages`:

```
@thebensage-dev:registry=https://npm.pkg.github.com
//npm.pkg.github.com/:_authToken=ghp_your_token
```

```powershell
npm install @thebensage-dev/api-client
```

## Use

```ts
import { createApiClient } from "@thebensage-dev/api-client";

const api = createApiClient({
  baseUrl: process.env.API_URL!,
  // The signed-in user's authentik access token, or null when signed out.
  getToken: () => session.accessToken,
});

const { data, error, response } = await api.GET("/people/{id}", { params: { path: { id: 7 } } });
if (data) console.log(data.fullName); // typed as Person
if (response.status === 404) console.log(error?.title);

const created = await api.POST("/people", { body: { fullName: "Ada King" /* … */ } });
if (created.response.status === 400) console.log(created.error?.errors); // messages per field
```

Calls never throw on an HTTP error: check `response.status` (or `data` / `error`). Any `fetch` option, such as Next's `cache: "no-store"`, can go in `createApiClient` or a single call.

## Sign-in and security

- **The package never signs anyone in or holds a secret.** Each app does its own authentik sign-in (UserDirectory uses openid-client) and hands the user's access token to `getToken`. That keeps client secrets and refresh tokens in the app, on its server.
- **The token only goes where it's needed.** The client reads which operations need a sign-in from the OpenAPI document (writes and `GET /people/audit-log`) and adds `Authorization: Bearer …` to those alone. Public reads never carry it, and `getToken` isn't even called for them. `requiresSignIn(method, path)` exposes the same check.
- **The API still decides.** It checks the issuer, audience, lifetime and signature of every token, and accepts only the authentik apps listed in its `Auth:Clients`. A new app that sends tokens needs its client id added there.
- **Use it on a server** (Server Components, Server Actions, route handlers) when it sends tokens, so the token never reaches the browser. A read-only client (no `getToken`) is fine anywhere.

## When the API changes

1. Build the API (`dotnet build` in the repo root): that rewrites `openapi/openapi.json`.
2. Here: `npm run generate`, which rewrites `src/schema.ts` and `src/secured.ts`.
3. Bump `version` in `package.json` (a breaking API change is a new major) and commit all of it.

On `main`, the "API client" workflow checks that the committed files match the API, then publishes the version if it isn't on GitHub Packages yet.

## Scripts

| Script                   | What                                                        |
| ------------------------ | ----------------------------------------------------------- |
| `npm run generate`       | Regenerates the types from `openapi/openapi.json`.          |
| `npm run generate:check` | Fails when the generated files are out of date (CI).        |
| `npm test`               | The client's tests (Node's test runner).                    |
| `npm run check`          | All of the above, then `typecheck` and `build`, as CI does. |
