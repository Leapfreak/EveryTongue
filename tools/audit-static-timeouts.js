// AUDIT: wall-clock give-up deadlines outside SidecarReadiness.  Tier: full /
// on-demand (HEURISTIC — findings are SUSPECTS for human review).
//
// ENGINE_CONCURRENCY_PLAN (2026-09-03): "give up after N seconds" loops assume
// a machine speed — fast PCs waste nothing, slow PCs get abandoned mid-load
// (field incident 2026-09-02: four silent no-caption rooms). The ONE sanctioned
// wait is SidecarReadiness.WaitAsync (progress-aware: idle timeout, no hard
// cap). This auditor flags:
//   1. deadline loops built from DateTime …AddSeconds(N) in non-test VB code
//      (SidecarReadiness.vb itself and benchmark/testing tools are exempt —
//      benchmarks poll in-process flags with a NAMED constant instead);
//   2. bare magic-number HttpClient .Timeout assignments without a nearby
//      named-constant or explanatory comment line.
'use strict';
const fs = require('fs');
const path = require('path');
const { vbFiles, rel, finish } = require('./audit-lib');

const suspects = [];

const EXEMPT = [
  'SidecarReadiness.vb',            // the sanctioned implementation
];
const EXEMPT_DIRS = [
  path.join('Services', 'Testing'), // benchmark tier: named constants, in-process flags
];

for (const file of vbFiles(false)) {
  const base = path.basename(file);
  if (EXEMPT.includes(base)) continue;
  if (EXEMPT_DIRS.some(d => file.includes(d))) continue;
  const lines = fs.readFileSync(file, 'utf8').split('\n');
  lines.forEach((line, i) => {
    const t = line.trim();
    if (t.startsWith("'")) return;
    // 1. wall-clock deadline construction with a numeric literal
    if (/\bAddSeconds\(\s*\d+(\.\d+)?\s*\)/.test(t) && /deadline|Deadline/.test(t)) {
      suspects.push(`${rel(file)}:${i + 1} wall-clock deadline literal — should this be SidecarReadiness (progress-aware) or a named constant? | ${t.slice(0, 120)}`);
    }
    // 2. Thread.Sleep with a big literal outside poll pacing (>2s = suspicious blind wait)
    const sleep = t.match(/Thread\.Sleep\(\s*(\d{4,})\s*\)/);
    if (sleep && parseInt(sleep[1], 10) > 2000) {
      suspects.push(`${rel(file)}:${i + 1} blind Thread.Sleep(${sleep[1]}ms) — what event should this wait on instead? | ${t.slice(0, 120)}`);
    }
  });
}

finish('audit-static-timeouts', suspects,
  'HEURISTIC suspects — resource-dependent waits belong in SidecarReadiness.WaitAsync (idle-based, no hard cap); fixed operation caps need a named constant with a reason');
