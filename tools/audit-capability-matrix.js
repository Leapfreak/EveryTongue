// REPORT: engine capability matrix.  Tier: report (never fails — its output
// IS the product).
//
// The "features not even applied" problem (SaT existed for Speechmatics but
// not whisper for a month; the EOU tuner likewise): each capability was built
// inside one engine and nobody could SEE the gap. This report derives an
// engines × capabilities table from code markers so empty cells are visible
// at a glance. An empty cell is a QUESTION, not automatically a bug — some
// capabilities don't apply (e.g. a self-endpointing cloud engine has no local
// VAD thresholds to tune) — but every question deserves an answer once.
//
// Markers are regex probes against each engine's source. When you add a
// capability to ANY engine, add a column here — that's what makes the next
// gap visible.
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT } = require('./audit-lib');

function probe(file, re) {
  if (!fs.existsSync(file)) return false;
  return re.test(fs.readFileSync(file, 'utf8'));
}

// ── Python live-server engines (streaming STT) ──────────────────────────────
const ENG_DIR = path.join(ROOT, 'live-server', 'engines');
const pyEngines = fs.readdirSync(ENG_DIR)
  .filter(f => f.endsWith('.py') && !['__init__.py', 'common.py'].includes(f));

const PY_CAPS = [
  { name: 'web-mic feed', re: /def\s+feed\s*\(/ },
  { name: 'vocab layers', re: /additional_vocab|service_vocab/ },
  { name: 'pace tuner',   re: /PaceTuner/ },
  { name: 'one-shot /transcribe', re: /def\s+_transcribe_|register_transcribe/ },
  { name: 'lang-change reconnect', re: /update_config/ },
];

// The whisper family's pipeline lives in vad/, not engines/ — include it as a
// pseudo-engine row so whisper gaps show up in the same table.
const vadFiles = ['pipeline.py', 'state_machine.py'].map(f => path.join(ROOT, 'live-server', 'vad', f));
function probeAny(files, re) { return files.some(f => probe(f, re)); }

// ── VB STT backends ─────────────────────────────────────────────────────────
const VB_STT_DIR = path.join(ROOT, 'EveryTongue.Core', 'Services', 'Stt');
const vbBackends = fs.readdirSync(VB_STT_DIR).filter(f => f.endsWith('Backend.vb'));
const VB_CAPS = [
  { name: 'ISegmentingSttBackend', re: /ISegmentingSttBackend/ },
  { name: 'SatHold forward',       re: /\.SatHold\s*=/ },
  { name: 'EouAutoTune forward',   re: /\.EouAutoTune\s*=/ },
  { name: 'AudioSource forward',   re: /\.AudioSource\s*=/ },
];

function pad(s, n) { return (s + ' '.repeat(n)).slice(0, Math.max(n, s.length)); }

function table(title, rows, caps) {
  console.log('\n' + title);
  const w = Math.max(...rows.map(r => r.name.length)) + 2;
  console.log(pad('', w) + caps.map(c => pad(c.name, c.name.length + 2)).join(''));
  for (const r of rows) {
    console.log(pad(r.name, w) + caps.map((c, i) => pad(r.cells[i] ? 'Y' : '-', c.name.length + 2)).join(''));
  }
}

table('PYTHON LIVE-SERVER ENGINES × CAPABILITIES',
  pyEngines.map(f => ({
    name: f.replace('.py', ''),
    cells: PY_CAPS.map(c => probe(path.join(ENG_DIR, f), c.re)),
  })).concat([{
    name: 'whisper (vad/)',
    cells: PY_CAPS.map(c => probeAny(vadFiles, c.re)),
  }]),
  PY_CAPS);

table('VB STT BACKENDS × CAPABILITIES',
  vbBackends.map(f => ({
    name: f.replace('.vb', ''),
    cells: VB_CAPS.map(c => probe(path.join(VB_STT_DIR, f), c.re)),
  })),
  VB_CAPS);

console.log('\nEmpty cells are QUESTIONS: does the capability apply to that engine, and if yes, why is it missing?');
process.exit(0);
