import assert from "node:assert/strict";
import { test } from "node:test";
import { createApiClient, requiresSignIn } from "../src/index.ts";

/** A fetch that records each request and answers 200 with `{}`. */
function recorder() {
  const requests: Request[] = [];
  const fetch = async (input: Request) => {
    requests.push(input);
    return new Response("{}", { headers: { "Content-Type": "application/json" } });
  };
  return { requests, fetch: fetch as typeof globalThis.fetch };
}

test("knows which operations need a sign-in", () => {
  assert.equal(requiresSignIn("post", "/people"), true);
  assert.equal(requiresSignIn("PUT", "/people/{id}"), true);
  assert.equal(requiresSignIn("DELETE", "/people/{id}"), true);
  assert.equal(requiresSignIn("GET", "/people/audit-log"), true);
  assert.equal(requiresSignIn("GET", "/people"), false);
  assert.equal(requiresSignIn("GET", "/people/{id}"), false);
});

test("sends the token only with operations that need it", async () => {
  const { requests, fetch } = recorder();
  let asked = 0;
  const api = createApiClient({
    baseUrl: "https://api.test/",
    fetch,
    getToken: () => {
      asked++;
      return "token-1";
    },
  });

  await api.GET("/people/{id}", { params: { path: { id: 1 } } });
  await api.DELETE("/people/{id}", { params: { path: { id: 1 } } });

  assert.equal(requests[0]?.url, "https://api.test/people/1");
  assert.equal(requests[0]?.headers.get("Authorization"), null);
  assert.equal(requests[1]?.headers.get("Authorization"), "Bearer token-1");
  assert.equal(asked, 1, "public reads don't ask for a token");
});

test("signed out: no header, and the API answers 401", async () => {
  const { requests, fetch } = recorder();
  const api = createApiClient({ baseUrl: "https://api.test", fetch, getToken: async () => null });

  await api.GET("/people/audit-log");

  assert.equal(requests[0]?.headers.get("Authorization"), null);
});

test("without getToken it is a read-only client", async () => {
  const { requests, fetch } = recorder();
  const api = createApiClient({ baseUrl: "https://api.test", fetch });

  await api.GET("/people", { params: { query: { q: "amara", interests: ["Coding", "Music"] } } });

  assert.equal(requests[0]?.url, "https://api.test/people?q=amara&interests=Coding&interests=Music");
});
