// Generates src/schema.ts (the API's types) and src/secured.ts (the operations that need a
// sign-in) from the OpenAPI document the API's build writes to openapi/openapi.json.
// `--check` only compares, and fails when the committed files are out of date (CI runs it).
import { readFileSync, writeFileSync } from "node:fs";
import openapiTS, { astToString } from "openapi-typescript";

const check = process.argv.includes("--check");
const documentUrl = new URL("../../../openapi/openapi.json", import.meta.url);
const document = JSON.parse(readFileSync(documentUrl, "utf8"));

const header =
  "// Generated from openapi/openapi.json by `npm run generate`. Don't edit: rebuild the API, then\n" +
  "// run the script again.\n\n";

const schema = header + astToString(await openapiTS(document, { alphabetize: true }));

const methods = ["get", "put", "post", "delete", "patch"];
const secured = Object.entries(document.paths)
  .flatMap(([path, item]) =>
    methods
      .filter((method) => item[method]?.security?.length)
      .map((method) => `${method.toUpperCase()} ${path}`),
  )
  .sort();
const securedTs =
  header +
  "/** `METHOD /path` of every operation that needs an authentik access token. */\n" +
  `export const securedOperations: ReadonlySet<string> = new Set([\n${secured
    .map((op) => `  "${op}",\n`)
    .join("")}]);\n`;

const outputs = { "src/schema.ts": schema, "src/secured.ts": securedTs };
let stale = false;
for (const [file, content] of Object.entries(outputs)) {
  const url = new URL(`../${file}`, import.meta.url);
  if (check) {
    let current = "";
    try {
      current = readFileSync(url, "utf8");
    } catch {}
    if (current !== content) {
      console.error(`${file} is out of date: run \`npm run generate\` and commit it.`);
      stale = true;
    }
  } else {
    writeFileSync(url, content);
    console.log(`Wrote ${file}`);
  }
}
if (stale) process.exit(1);
