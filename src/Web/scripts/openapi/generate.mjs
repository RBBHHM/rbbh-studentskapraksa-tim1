import { spawn } from "node:child_process";
import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";

import openapiTS, { astToString } from "openapi-typescript";

const schemaUrl = "http://127.0.0.1:5080/swagger/v1/swagger.json";
const outputPath = resolve("src/lib/api/generated/api.ts");
const timeoutMs = 45_000;

async function schemaIsReady() {
  try {
    const response = await fetch(schemaUrl);
    return response.ok;
  } catch {
    return false;
  }
}

async function waitForSchema(server) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (server.exitCode !== null)
      throw new Error(`The temporary ASP.NET Core host exited with code ${server.exitCode}.`);
    if (await schemaIsReady()) return;
    await new Promise((resolveWait) => setTimeout(resolveWait, 250));
  }
  throw new Error(`OpenAPI was not available at ${schemaUrl} within ${timeoutMs / 1_000} seconds.`);
}

async function run(command, args) {
  await new Promise((resolveRun, rejectRun) => {
    const child = spawn(command, args, { stdio: "inherit", windowsHide: true });
    child.once("error", rejectRun);
    child.once("exit", (code) =>
      code === 0 ? resolveRun() : rejectRun(new Error(`${command} exited with code ${code}.`)),
    );
  });
}

let temporaryServer;
try {
  if (!(await schemaIsReady())) {
    process.stdout.write("Starting the temporary ASP.NET Core host…\n");
    await run("dotnet", ["build", "../../RelatedPartiesRegister/RelatedPartiesRegister.csproj", "--configuration", "Release", "--nologo", "--verbosity", "quiet"]);
    temporaryServer = spawn(
      "dotnet",
      ["../../RelatedPartiesRegister/bin/Release/net10.0/RelatedPartiesRegister.dll", "--urls", "http://127.0.0.1:5080"],
      {
        env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Development" },
        stdio: ["ignore", "pipe", "pipe"],
        windowsHide: true,
      },
    );
    await waitForSchema(temporaryServer);
  }

  const nodes = await openapiTS(new URL(schemaUrl));
  await mkdir(dirname(outputPath), { recursive: true });
  await writeFile(outputPath, astToString(nodes), "utf8");
  process.stdout.write(`Generated ${outputPath}\n`);
} finally {
  if (temporaryServer && temporaryServer.exitCode === null) {
    temporaryServer.kill();
    await new Promise((resolveExit) => {
      temporaryServer.once("exit", resolveExit);
      setTimeout(resolveExit, 3_000).unref();
    });
  }
}
