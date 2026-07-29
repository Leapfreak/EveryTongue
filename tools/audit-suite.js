// AUDIT SUITE RUNNER.
//
//   node tools/audit-suite.js            -> run EVERYTHING (exact + heuristic)
//   node tools/audit-suite.js --publish  -> exact zero-noise tier only
//                                           (wired into every publish, warn-only)
//
// Tiers:
//   publish — exact rules with zero false positives; any finding is a must-fix.
//   full    — adds the heuristic auditors whose findings are SUSPECTS needing
//             human review (run on demand / before releases).
//
// Adding an auditor: create tools/audit-<name>.js (use audit-lib.js helpers,
// exit 1 on findings), then register it here with a tier + one-line purpose.
'use strict';
const { spawnSync } = require('child_process');
const path = require('path');

const AUDITS = [
  { script: 'audit-hardcoded-strings.js', tier: 'publish', desc: 'user-facing English literals bypassing LanguagePackService' },
  { script: 'audit-locale-parity.js',     tier: 'publish', desc: 'locale files in sync + every referenced key exists' },
  { script: 'audit-es5-appjs.js',         tier: 'publish', desc: 'app.js stays ES5 (old phone browsers)' },
  { script: 'audit-api-footgun.js',       tier: 'publish', desc: 'VB minimal-API async lambda silent-200 footgun' },
  { script: 'audit-code-bans.js',         tier: 'publish', desc: 'Debug.WriteLine / List(Of Object) bans' },
  { script: 'audit-language-lists.js',    tier: 'full',    desc: 'HEURISTIC: inline language-code lists (canonical = language-codes.json)' },
  { script: 'audit-pipe-drain.js',        tier: 'full',    desc: 'HEURISTIC: pipe redirect + WaitForExit without draining both pipes' },
];

const publishOnly = process.argv.includes('--publish');
const toRun = AUDITS.filter(a => !publishOnly || a.tier === 'publish');

let failed = 0;
for (const a of toRun) {
  const r = spawnSync(process.execPath, [path.join(__dirname, a.script)], { encoding: 'utf8' });
  const out = (r.stdout + r.stderr).trim();
  if (r.status === 0) {
    console.log(`PASS  ${a.script}`);
  } else {
    failed++;
    console.log(`FAIL  ${a.script} — ${a.desc}`);
    console.log(out.split('\n').map(l => '      ' + l).join('\n'));
  }
}
console.log(`\naudit-suite (${publishOnly ? 'publish tier' : 'full'}): ${toRun.length - failed}/${toRun.length} clean`);
process.exit(failed ? 1 : 0);
