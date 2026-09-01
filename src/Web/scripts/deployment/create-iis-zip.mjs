import { mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { zipSync } from "fflate";

const source = resolve("dist");
const output = resolve("artifacts", "connected-parties-iis.zip");
const files = {};

function collect(directory) {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) collect(path);
    else files[relative(source, path).replaceAll("\\", "/")] = readFileSync(path);
  }
}

collect(source);
mkdirSync(dirname(output), { recursive: true });
writeFileSync(output, zipSync(files, { level: 9 }));
console.log(`IIS artifact: ${output}`);
