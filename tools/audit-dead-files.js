// REPORT: dead FILES — whole source files nothing references.  Tier: report
// (never fails; the output is a review list, NOT a delete list — dynamic
// loading, reflection, DI and entry points can all hide legitimate uses).
//
// What it checks:
//  - VB: a file none of whose declared top-level types (Class/Module/
//    Structure/Interface/Enum) are mentioned in any OTHER .vb file.
//    Designer files and entry points are skipped; partial classes are safe
//    (the shared type name is referenced from the sibling file's users).
//  - Python sidecars: a module nothing imports (engines/* are auto-registered
//    by the engines package scanning its own directory — treated as alive).
//  - wwwroot JS: a file no HTML page or VB-served path mentions.
//
// tools/*.js is deliberately OUT OF SCOPE: every auditor and helper there is
// standalone by design (they'd all be flagged — including this file).
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, walkFiles, vbFiles, rel } = require('./audit-lib');

const findings = [];

// ── VB ──────────────────────────────────────────────────────────────────────
const ENTRY = /(^|[\\/])(Program|LiteProgram|ApplicationEvents)\.vb$/;
const TYPE_DECL = /^\s*(?:(?:Public|Friend|Private|Partial|NotInheritable|MustInherit)\s+)*(?:Class|Module|Structure|Interface|Enum)\s+([A-Za-z_]\w+)/;
const files = [...vbFiles(true)];
const fileTypes = new Map();   // file -> [type names]
const textOf = new Map();
for (const f of files) {
  const text = fs.readFileSync(f, 'utf8');
  textOf.set(f, text);
  const types = [];
  for (const line of text.split(/\r?\n/)) {
    const m = TYPE_DECL.exec(line);
    if (m) types.push(m[1]);
  }
  fileTypes.set(f, types);
}
for (const f of files) {
  if (f.endsWith('.Designer.vb') || ENTRY.test(f)) continue;
  const types = fileTypes.get(f);
  if (!types.length) continue;
  const referenced = types.some(t => {
    const re = new RegExp('\\b' + t + '\\b');
    // A partial sibling (e.g. FormMain.Shell.vb) declaring the same type does
    // not count as a "use" — but any OTHER file mentioning the type does.
    return files.some(o => o !== f && !fileTypes.get(o).includes(types[0]) && re.test(textOf.get(o)));
  });
  if (!referenced) findings.push(`VB: ${rel(f)} — types [${types.join(', ')}] referenced by no other file`);
}

// ── Python sidecars ─────────────────────────────────────────────────────────
const PY_DIRS = ['live-server', 'translate-server', 'mms-tts-server', 'qe-server'];
const pyFiles = [];
for (const d of PY_DIRS) {
  const full = path.join(ROOT, d);
  if (fs.existsSync(full)) pyFiles.push(...walkFiles(full, ['.py']));
}
const pyAll = pyFiles.map(f => fs.readFileSync(f, 'utf8')).join('\n');
for (const f of pyFiles) {
  const base = path.basename(f, '.py');
  if (base === 'server' || base === '__init__') continue;          // entry points / packages
  if (/[\\/]engines[\\/]/.test(f)) continue;                        // auto-registered by directory scan
  // Covers absolute (import x, from pkg.x import), relative (from .x import),
  // and submodule (from vad.x import) forms.
  const imported = new RegExp('(import|from)\\s+\\.*([\\w.]+\\.)?' + base + '\\b').test(pyAll);
  // Standalone generator scripts are invoked from VB or docs — check those too.
  const vbMention = files.some(o => textOf.get(o).includes(base + '.py'));
  if (!imported && !vbMention) findings.push(`PY: ${rel(f)} — never imported and never invoked from VB`);
}

// ── wwwroot JS ──────────────────────────────────────────────────────────────
const wwwroot = path.join(ROOT, 'EveryTongue.Core', 'wwwroot');
if (fs.existsSync(wwwroot)) {
  const jsFiles = [...walkFiles(path.join(wwwroot, 'js'), ['.js'])];
  const htmlAll = [...walkFiles(wwwroot, ['.html'])].map(f => fs.readFileSync(f, 'utf8')).join('\n')
    + [...walkFiles(path.join(wwwroot, 'js'), ['.js'])].map(f => fs.readFileSync(f, 'utf8')).join('\n');
  const vbAll = files.map(f => textOf.get(f)).join('\n');
  for (const f of jsFiles) {
    const base = path.basename(f);
    if (!htmlAll.includes(base) && !vbAll.includes(base)) {
      findings.push(`JS: ${rel(f)} — referenced by no HTML page, script, or VB-served path`);
    }
  }
}

if (findings.length) {
  console.log(`audit-dead-files: ${findings.length} candidate(s) — REVIEW list, not a delete list (reflection/dynamic loading can hide uses):`);
  for (const f of findings) console.log('  ' + f);
} else {
  console.log('audit-dead-files: no dead-file candidates.');
}
process.exit(0);   // report tier — never fails
