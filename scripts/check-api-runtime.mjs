import { readFileSync, existsSync } from "node:fs";
import { resolve, dirname, relative } from "node:path";
import { fileURLToPath } from "node:url";
import { createRequire } from "node:module";
const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const ts = createRequire(resolve(root, "frontend/package.json"))("typescript");
const seen = new Set(),
  failures = [];
function visit(path) {
  if (seen.has(path) || path.endsWith(".spec.ts")) return;
  seen.add(path);
  const label = relative(root, path).replaceAll("\\", "/"),
    source = readFileSync(path, "utf8");
  if (
    /\/demo\/|\/core\/session\/demo/.test(label) ||
    /DemoRepository|DemoSession|DepotSnapshot|snp-demo|localStorage/.test(
      source,
    )
  )
    failures.push(label + ": demo/business snapshot dependency");
  if (
    /sessionStorage/.test(source) &&
    !label.endsWith("/auth/token-storage.ts")
  )
    failures.push(label + ": session storage outside token storage");
  if (/\/api\/master-data|\/assign['"`]/.test(source))
    failures.push(label + ": deprecated API alias");
  const file = ts.createSourceFile(path, source, ts.ScriptTarget.Latest, true);
  function walk(node) {
    let spec;
    if (
      (ts.isImportDeclaration(node) || ts.isExportDeclaration(node)) &&
      node.moduleSpecifier &&
      !(ts.isImportDeclaration(node) && node.importClause?.isTypeOnly)
    )
      spec = node.moduleSpecifier;
    if (
      ts.isCallExpression(node) &&
      node.expression.kind === ts.SyntaxKind.ImportKeyword
    )
      spec = node.arguments[0];
    if (spec && ts.isStringLiteral(spec) && spec.text.startsWith(".")) {
      const base = resolve(dirname(path), spec.text),
        next = [base + ".ts", base + "/index.ts", base].find(
          (p) => existsSync(p) && p.endsWith(".ts"),
        );
      if (!next) failures.push(label + ": missing import " + spec.text);
      else visit(next);
    }
    ts.forEachChild(node, walk);
  }
  walk(file);
}
visit(resolve(root, "frontend/src/main.ts"));
if (failures.length) {
  console.error(failures.join("\n"));
  process.exitCode = 1;
} else
  console.log(
    "PASS: " +
      seen.size +
      " runtime TypeScript modules; no demo state, business browser storage or deprecated aliases.",
  );
