// Shared helpers for the audit-*.js suite. Each auditor is standalone
// (node tools/audit-<name>.js), prints violations, and exits 1 when it finds
// any. audit-suite.js runs them all (or the publish tier).
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const VB_DIRS = ['EveryTongue.Core', 'EveryTongue', 'EveryTongue.Lite'];
const SKIP_DIRS = new Set(['bin', 'obj', 'node_modules', '__pycache__',
                           'python-embed', 'sat-libs', 'sat-cache']);

function* walkFiles(dir, ext) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (SKIP_DIRS.has(e.name)) continue;
      yield* walkFiles(p, ext);
    } else if (ext.some(x => e.name.endsWith(x))) {
      yield p;
    }
  }
}

function* vbFiles(includeDesigner) {
  for (const d of VB_DIRS) {
    const full = path.join(ROOT, d);
    if (!fs.existsSync(full)) continue;
    for (const f of walkFiles(full, ['.vb'])) {
      if (!includeDesigner && f.endsWith('.Designer.vb')) continue;
      yield f;
    }
  }
}

function rel(f) { return path.relative(ROOT, f).replace(/\\/g, '/'); }

// Scan file lines, skipping VB comment lines; cb(line, lineNo1based, relPath).
function scanVbLines(file, cb) {
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
  lines.forEach((line, i) => {
    if (line.trim().startsWith("'")) return;
    cb(line, i + 1, rel(file));
  });
}

// Standard result reporting: print violations and exit accordingly.
function finish(name, violations, note) {
  if (violations.length) {
    console.error(`${name}: ${violations.length} finding(s)${note ? ' — ' + note : ''}:`);
    for (const v of violations) console.error('  ' + v);
    process.exit(1);
  }
  console.log(`${name}: clean.`);
}

module.exports = { ROOT, VB_DIRS, walkFiles, vbFiles, rel, scanVbLines, finish };
