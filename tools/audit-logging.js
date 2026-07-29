// AUDIT: features must write useful logs.  Tier: full / on-demand (HEURISTIC).
//
// "Useful" is judgment, but the ABSENCE of logging has detectable signatures.
// Three checks (the project principle is "build with logging baked in" — a
// field machine's log is the only debugger we have on Sundays):
//
//  1. Silent VB Catch blocks: a Catch that neither logs (AppLogger/_log),
//     rethrows, nor carries a comment justifying the silence. Swallowed
//     exceptions are how features die invisibly in the field. THE RULE:
//     silence must be justified IN PLACE — add a comment saying why ignoring
//     is correct, or log it.
//  2. Python except blocks with the same disease (no logger./logging./raise/
//     comment) in the sidecars.
//  3. Feature areas with NO logging at all: substantial .vb files under
//     Services/Controllers/Pipeline/Server that never reference AppLogger —
//     undebuggable from the field.
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, walkFiles, vbFiles, rel, finish } = require('./audit-lib');

const suspects = [];

// ── 1. Silent VB Catch blocks ───────────────────────────────────────────────
const VB_LOG_TOKENS = /AppLogger\.|\b_log\s*\(|\b_debugLog\s*\(|\bLogToUnified\s*\(|\bWriteLog\b|\bThrow\b|\bSLOG\b|\bLog\s*\(\s*\$?"|\.Log(Warning|Error|Information|Debug|Critical|Trace)\b|\bRaiseEvent\s+\w*(Error|Status|Failed)/;
for (const file of vbFiles(false)) {
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
  // Stack of open Try contexts; each Catch segment is evaluated on close.
  const stack = [];
  let seg = null; // { file, lineNo, hasEvidence, depth }
  const closeSeg = () => {
    if (seg && !seg.hasEvidence) {
      suspects.push(`${rel(file)}:${seg.lineNo} silent Catch — no log, no rethrow, no justifying comment`);
    }
    seg = null;
  };
  lines.forEach((raw, i) => {
    const line = raw.trim();
    // Single-line `Try : x : Catch : End Try` is the self-documenting
    // best-effort idiom — accepted without a comment (decision 2026-07-29).
    // It must not touch the stack or it desyncs the depth tracking.
    if (/^Try\b/.test(line) && /\bEnd Try\b/.test(line)) return;
    if (/^Try\b/.test(line)) { stack.push(true); return; }
    if (/^Catch\b/.test(line) && stack.length) {
      if (seg && seg.depth === stack.length) closeSeg();
      seg = { lineNo: i + 1, hasEvidence: false, depth: stack.length };
      // Evidence may sit on the Catch line itself (single-line handlers).
      if (VB_LOG_TOKENS.test(line) || line.includes("'")) seg.hasEvidence = true;
      return;
    }
    if (/^Finally\b/.test(line) && seg && seg.depth === stack.length) { closeSeg(); return; }
    if (/^End Try\b/.test(line)) {
      if (seg && seg.depth === stack.length) closeSeg();
      stack.pop();
      return;
    }
    if (seg && seg.depth === stack.length) {
      if (VB_LOG_TOKENS.test(line) || line.startsWith("'") || raw.includes("' ")) seg.hasEvidence = true;
    }
  });
}

// ── 2. Silent python except blocks ──────────────────────────────────────────
const PY_LOG_TOKENS = /logger\.|logging\.|\braise\b|print\(/;
for (const dir of ['live-server', 'translate-server', 'mms-tts-server', 'qe-server']) {
  const full = path.join(ROOT, dir);
  if (!fs.existsSync(full)) continue;
  for (const file of walkFiles(full, ['.py'])) {
    const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((raw, i) => {
      const m = /^(\s*)except\b([^:]*):\s*(.*)$/.exec(raw);
      if (!m) return;
      // A TYPED narrow except (queue.Empty, (TypeError, ValueError), OSError…)
      // with a fallback is self-documenting — the danger class is bare
      // `except:` / broad `except Exception` (policy 2026-07-29).
      const types = m[2].trim();
      if (types && !/\b(Exception|BaseException)\b/.test(types)) return;
      const indent = m[1].length;
      let hasEvidence = PY_LOG_TOKENS.test(m[3]) || m[3].includes('#');
      // Walk the indented block.
      for (let j = i + 1; j < lines.length && !hasEvidence; j++) {
        const l = lines[j];
        if (l.trim() === '') continue;
        const li = l.length - l.trimStart().length;
        if (li <= indent) break;
        if (PY_LOG_TOKENS.test(l) || l.trim().startsWith('#') || l.includes('  #')) hasEvidence = true;
      }
      if (!hasEvidence) suspects.push(`${rel(file)}:${i + 1} silent except — no log, no raise, no justifying comment`);
    });
  }
}

// ── 3. Unlogged feature areas ───────────────────────────────────────────────
const AREA_DIRS = ['Services', 'Controllers', 'Pipeline', 'Server'];
const MIN_LINES = 150;
// Reviewed 2026-07-29: files whose errors flow to a logging caller by design.
const AREA_ALLOW = {
  'UsfmConverter.vb': 'pure transform — errors returned in the issues list; the Download Manager logs installs',
  'IntegrityChecker.vb': 'its OUTPUT is the report — Program.vb logs it at startup',
  'PriorityWorkQueue.vb': 'data structure — callers own the logging',
  'CloudStreamingSttBackend.vb': 'thin adapter — LiveStreamRunner + sidecar own the logging',
  'FasterWhisperBackend.vb': 'thin adapter — LiveStreamRunner + sidecar own the logging',
  'SpeechmaticsConfig.vb': 'declarative config block',
  'SpeechmaticsClauseAccumulator.vb': 'data structure — the clause coordinator logs fragments/locks',
  'ResourceMonitor.vb': 'benchmark collector — results land in the report',
};
for (const file of vbFiles(false)) {
  const r = rel(file);
  if (!AREA_DIRS.some(d => r.includes('/' + d + '/'))) continue;
  if (AREA_ALLOW[path.basename(file)]) continue;
  const text = fs.readFileSync(file, 'utf8');
  const lineCount = text.split('\n').length;
  if (lineCount < MIN_LINES) continue;
  if (!/AppLogger\.|\b_log\s*\(/.test(text)) {
    suspects.push(`${r} (${lineCount} lines) — feature-sized file with NO logging at all`);
  }
}

finish('audit-logging', suspects,
  'HEURISTIC — log it, rethrow it, or comment WHY silence is correct; feature files need at least error-path logging');
