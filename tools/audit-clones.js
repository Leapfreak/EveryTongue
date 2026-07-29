// AUDIT: duplicated code (copy-paste clones).  Tier: full / on-demand
// (HEURISTIC — findings are candidates for the "extract when duplicating
// across 2+ classes" rule, not automatic violations).
//
// Sliding-window clone detector: normalize each source line (trim, collapse
// whitespace, drop blanks/comments), hash every window of WINDOW consecutive
// normalized lines, and report windows that appear in 2+ places. Overlapping
// windows are merged into blocks and the biggest blocks reported first.
// Designer files are excluded (generated layout code repeats by nature).
'use strict';
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const { ROOT, walkFiles, rel } = require('./audit-lib');

const WINDOW = 12;          // consecutive substantial lines that must match
const MIN_BLOCK_CHARS = 300; // ignore matches of trivial boilerplate
const MAX_REPORT = 25;

const SOURCES = [
  ['EveryTongue.Core', ['.vb']],
  ['EveryTongue', ['.vb']],
  ['EveryTongue.Lite', ['.vb']],
  ['live-server', ['.py']],
  ['translate-server', ['.py']],
  ['mms-tts-server', ['.py']],
  ['qe-server', ['.py']],
  [path.join('EveryTongue.Core', 'wwwroot', 'js'), ['.js']],
];

function normalize(file) {
  const out = [];
  fs.readFileSync(file, 'utf8').split(/\r?\n/).forEach((line, i) => {
    const t = line.trim().replace(/\s+/g, ' ');
    if (!t || t.startsWith("'") || t.startsWith('//') || t.startsWith('#') || t.startsWith('*') || t.startsWith('/*')) return;
    // Import headers repeat across files by nature — not clones.
    if (/^(Imports |import |from .+ import )/.test(t)) return;
    out.push({ text: t, lineNo: i + 1 });
  });
  return out;
}

const windows = new Map();  // hash -> [{file, startLine, endLine, chars}]
for (const [dir, exts] of SOURCES) {
  const full = path.join(ROOT, dir);
  if (!fs.existsSync(full)) continue;
  for (const file of walkFiles(full, exts)) {
    if (file.endsWith('.Designer.vb')) continue;
    const lines = normalize(file);
    for (let i = 0; i + WINDOW <= lines.length; i++) {
      const slice = lines.slice(i, i + WINDOW);
      const joined = slice.map(l => l.text).join('\n');
      if (joined.length < MIN_BLOCK_CHARS) continue;
      const h = crypto.createHash('md5').update(joined).digest('hex');
      if (!windows.has(h)) windows.set(h, []);
      windows.get(h).push({ file: rel(file), startLine: slice[0].lineNo, endLine: slice[WINDOW - 1].lineNo, chars: joined.length });
    }
  }
}

// Keep hashes seen in 2+ distinct locations; merge overlapping/adjacent
// windows per (fileA,fileB) pair into one block report.
const dupes = [];
for (const locs of windows.values()) {
  const distinct = locs.filter((l, i) => locs.findIndex(o => o.file === l.file && Math.abs(o.startLine - l.startLine) < WINDOW) === i);
  if (distinct.length >= 2) dupes.push(distinct);
}
// Merge: group by the sorted location-set key rounded to blocks.
const blocks = new Map();
for (const set of dupes) {
  const key = set.map(l => l.file + ':' + Math.floor(l.startLine / WINDOW)).sort().join('|');
  const prev = blocks.get(key);
  if (!prev || set[0].chars > prev[0].chars) blocks.set(key, set);
}

const report = [...blocks.values()]
  .sort((a, b) => b[0].chars - a[0].chars)
  .slice(0, MAX_REPORT);

if (report.length) {
  console.error(`audit-clones: ${report.length} duplicated block(s) (window=${WINDOW} lines, top ${MAX_REPORT}) — candidates for the extract-shared rule:`);
  for (const set of report) {
    console.error('  ~' + set[0].chars + ' chars duplicated at:');
    for (const l of set) console.error(`    ${l.file}:${l.startLine}-${l.endLine}`);
  }
  process.exit(1);
}
console.log('audit-clones: clean.');
