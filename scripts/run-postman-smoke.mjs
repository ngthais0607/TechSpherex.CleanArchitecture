import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { createRequire } from 'node:module';
import { randomBytes } from 'node:crypto';

if (!process.argv.includes('--disposable-test')) throw new Error('Pass --disposable-test to run against the local smoke Docker project only.');
const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const work = resolve(root, '.superpowers/sdd/2026-10-16-week4-api-reports-handover');
const smoke = resolve(work, 'snp_week4_smoke');
const settings = Object.fromEntries(readFileSync(resolve(smoke, 'smoke.env'), 'utf8').split(/\r?\n/)
  .filter(line => line && !line.startsWith('#')).map(line => { const index = line.indexOf('='); return [line.slice(0,index),line.slice(index+1)]; }));
const require = createRequire(import.meta.url);
const newman = require(resolve(work,'postman-runner/node_modules/newman'));
const collection = JSON.parse(readFileSync(resolve(root,'docs/week-04/postman/SnpDepotManagement.postman_collection.json'),'utf8'));
const environment = JSON.parse(readFileSync(resolve(root,'docs/week-04/postman/SnpDepotManagement.local.postman_environment.json'),'utf8'));
const overrides = {
  baseUrl: `http://127.0.0.1:${Number(settings.API_PORT)}`,
  adminUserName: settings.Admin__UserName,
  adminPassword: settings.Admin__Password,
  testUserPassword: 'Postman9!' + randomBytes(24).toString('base64')
};
for (const variable of environment.values) if (Object.hasOwn(overrides, variable.key)) variable.value = overrides[variable.key];
// No secret environment or raw response report is written to disk or sent to Postman Cloud.
newman.run({ collection, environment, reporters: ['cli'], timeoutRequest: 30000, timeoutScript: 10000,
  reporter: { cli: { noConsole: true } } }, (error, summary) => {
  const result = {
    executedAtUtc: new Date().toISOString(), runner: 'Newman '+require(resolve(work,'postman-runner/node_modules/newman/package.json')).version,
    collectionFormat: 'v2.1', baseUrl: overrides.baseUrl,
    stats: summary?.run?.stats,
    failures: (summary?.run?.failures ?? []).map(f => ({ name: f.source?.name, at: f.at, assertion: f.error?.test, message: f.error?.message })),
    error: error ? String(error.message) : null
  };
  writeFileSync(resolve(work,'postman-results.json'),JSON.stringify(result,null,2)+'\n');
  if (error || result.failures.length) process.exitCode = 1;
});
