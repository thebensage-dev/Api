import createClient, { type Client, type ClientOptions, type Middleware } from "openapi-fetch";
import type { components, paths } from "./schema.ts";
import { securedOperations } from "./secured.ts";

export type { components, paths };

type Schemas = components["schemas"];
export type Person = Schemas["PersonDto"];
export type PersonInput = Schemas["PersonInput"];
export type PersonPage = Schemas["PersonPage"];
export type DirectorySummary = Schemas["DirectorySummary"];
export type PeopleOptions = Schemas["PeopleOptions"];
export type PersonAuditEntry = Schemas["PersonAuditEntryDto"];
/** A 400 or 409: `errors` holds the messages per field (camelCase keys). */
export type ValidationProblem = Schemas["HttpValidationProblemDetails"];

/** The typed client: `api.GET("/people/{id}", { params: { path: { id } } })` returns `{ data, error, response }`. */
export type ApiClient = Client<paths>;

/**
 * Returns the signed-in user's authentik access token, or nothing when nobody is signed in.
 * Called on each request that needs one, so it can return a refreshed token.
 */
export type TokenSource = () => string | null | undefined | Promise<string | null | undefined>;

export interface ApiClientOptions extends Omit<ClientOptions, "baseUrl"> {
  /** The API's address, such as `https://api.example.com`. */
  baseUrl: string;
  /**
   * Where the token comes from. It's sent only to the operations that need a sign-in (writes and
   * the audit log), never with public reads. Leave it out for a read-only client.
   */
  getToken?: TokenSource;
}

/** Whether an operation needs an authentik access token, as the API's OpenAPI document says. */
export function requiresSignIn(method: string, schemaPath: string): boolean {
  return securedOperations.has(`${method.toUpperCase()} ${schemaPath}`);
}

/** A client for TheBensage API. */
export function createApiClient({ getToken, baseUrl, ...options }: ApiClientOptions): ApiClient {
  const client = createClient<paths>({ ...options, baseUrl: baseUrl.replace(/\/+$/, "") });
  if (getToken) client.use(bearerToken(getToken));
  return client;
}

function bearerToken(getToken: TokenSource): Middleware {
  return {
    async onRequest({ request, schemaPath }) {
      if (!requiresSignIn(request.method, schemaPath)) return undefined;
      const token = await getToken();
      if (token) request.headers.set("Authorization", `Bearer ${token}`);
      return request;
    },
  };
}
