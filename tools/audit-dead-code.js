// AUDIT: dead code.  Tier: full / on-demand (HEURISTIC — findings are
// SUSPECTS: VB has reflection, AddHandler-by-name, Designer wiring, DI…
// verify before deleting).
//
// Two checks with a decent signal/noise ratio:
//  1. Private Sub/Function/Property declared in a file but never referenced
//     anywhere else IN THAT FILE (Private = file is the whole world).
//     Declarations with a `Handles` clause are skipped (event-wired).
//  2. LogEvents constants never referenced outside LogEvents.vb — an event ID
//     nobody logs is either dead or a feature that lost its instrumentation.
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, vbFiles, rel, finish } = require('./audit-lib');

// Reviewed suspects that are intentionally kept (name -> reason).
const ALLOW = {
  // (none yet)
};

const suspects = [];

// 1. Unreferenced Private members. A Private member's world is its CLASS, and
// Partial classes span files (FormMain.vb + FormMain.Shell.vb,
// EndpointRegistration.*.vb, …) — so group files by declared class name and
// search the whole group, not just the declaring file.
const DECL = /^\s*Private\s+(?:Shared\s+)?(?:Async\s+)?(?:Iterator\s+)?(?:Sub|Function|ReadOnly\s+Property|Property)\s+([A-Za-z_]\w*)/;
const CLASS_DECL = /(?:Partial\s+)?(?:Public|Friend|Private)?\s*(?:NotInheritable\s+|MustInherit\s+)?(?:Class|Module)\s+([A-Za-z_]\w*)/;

const fileText = new Map();       // file -> text
const classFiles = new Map();     // class name -> [files]
for (const file of vbFiles(false)) {
  const text = fs.readFileSync(file, 'utf8');
  fileText.set(file, text);
  const seen = new Set();
  for (const line of text.split(/\r?\n/)) {
    const m = CLASS_DECL.exec(line.trim());
    if (m && !seen.has(m[1])) {
      seen.add(m[1]);
      if (!classFiles.has(m[1])) classFiles.set(m[1], []);
      classFiles.get(m[1]).push(file);
    }
  }
}
function groupText(file, text) {
  // Union of all files sharing any class declared in this file.
  const group = new Set([file]);
  for (const [cls, files] of classFiles) {
    if (files.includes(file)) files.forEach(f => group.add(f));
  }
  return [...group].map(f => fileText.get(f)).join('\n');
}

for (const [file, text] of fileText) {
  const scope = groupText(file, text);
  text.split(/\r?\n/).forEach((line, i) => {
    if (/\bHandles\s/.test(line)) return;
    const m = DECL.exec(line);
    if (!m) return;
    const name = m[1];
    if (ALLOW[name]) return;
    if (name === 'New' || name === 'Dispose' || name === 'Finalize') return;
    const uses = (scope.match(new RegExp('\\b' + name + '\\b', 'g')) || []).length;
    // 1 = the declaration itself. Property Get/Set blocks and named End
    // statements don't repeat the name in VB.
    if (uses <= 1) suspects.push(`${rel(file)}:${i + 1} Private ${name} — never referenced in its class (incl. partials)`);
  });
}

// 2. LogEvents constants nobody logs.
const logEventsPath = path.join(ROOT, 'EveryTongue.Core', 'Services', 'Infrastructure', 'LogEvents.vb');
if (fs.existsSync(logEventsPath)) {
  const consts = [];
  for (const m of fs.readFileSync(logEventsPath, 'utf8').matchAll(/Public\s+Const\s+([A-Z0-9_]+)\b/g)) consts.push(m[1]);
  const used = new Set();
  for (const file of vbFiles(true)) {
    if (path.basename(file) === 'LogEvents.vb') continue;
    const text = fs.readFileSync(file, 'utf8');
    for (const c of consts) if (!used.has(c) && text.includes('LogEvents.' + c)) used.add(c);
  }
  for (const c of consts) {
    if (!used.has(c) && !ALLOW[c]) suspects.push(`LogEvents.${c} — registered event ID never logged anywhere`);
  }
}

finish('audit-dead-code', suspects,
  'HEURISTIC suspects — verify (reflection/AddHandler/DI can hide uses), then delete or allowlist with a reason');
