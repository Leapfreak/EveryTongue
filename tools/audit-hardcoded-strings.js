// Audit for hardcoded user-facing English strings in VB code.
//
// THE RULE (CLAUDE.md "All user-facing strings must be localised"): user-visible
// text goes through LanguagePackService GetString keys + locales/*.json — never
// string literals in code. This auditor makes the rule mechanical: it runs on
// every publish (warn-only) and can be run directly:  node tools/audit-hardcoded-strings.js
//
// What it flags (non-Designer .vb only — Designer literals are overridden by
// each form's ApplyLocale, and the column/text audits cover those separately):
//   - MessageBox.Show("literal"...  / MsgBox("literal"...
//   - <control>.Text = "literal"
//   - .HeaderText = "literal"
//   - SetToolTip(x, "literal") / .ToolTipText = "literal"
// Lines containing GetString(/_getString( are compliant; AppLogger/log/exception
// text is exempt by design (log text stays English).
//
// Allowlist: tools/hardcoded-strings.allow.json — every entry needs a reason.
// New violations (not allowlisted) exit 1 and must be fixed or consciously
// allowlisted, never ignored.
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const SCAN_DIRS = ['EveryTongue.Core', 'EveryTongue', 'EveryTongue.Lite'];
const ALLOW_PATH = path.join(__dirname, 'hardcoded-strings.allow.json');

const PATTERNS = [
  { kind: 'messagebox', re: /(?:MessageBox\.Show|MsgBox)\(\s*"([^"]*[A-Za-z]{2}[^"]*)"/ },
  { kind: 'text',       re: /\.Text\s*=\s*"([^"]*[A-Za-z]{2}[^"]*)"/ },
  { kind: 'headertext', re: /\.HeaderText\s*=\s*"([^"]*[A-Za-z]{2}[^"]*)"/ },
  { kind: 'tooltip',    re: /(?:SetToolTip\([^,]+,\s*|\.ToolTipText\s*=\s*)"([^"]*[A-Za-z]{2}[^"]*)"/ },
  // PipelineRunner progress: Report(step, "...") -> StatusMessage -> _lblStepStatus.Text
  // (user-visible; found 2026-07-30 when the sink was pointed out mid-refactor).
  { kind: 'reportstatus', re: /\bReport\([^"\r\n]+?,\s*"([^"]*[A-Za-z]{2}[^"]*)"/ },
];
// Compliant / out-of-scope lines.
const LINE_EXEMPT = /GetString\(|_getString\(|AppLogger\.|Throw New|Exception\(|LogEvents\.|Debug\./;

const allow = fs.existsSync(ALLOW_PATH) ? JSON.parse(fs.readFileSync(ALLOW_PATH, 'utf8')) : [];

function isAllowed(file, kind, literal) {
  const base = path.basename(file);
  return allow.some(a =>
    (a.file === '*' || a.file === base) &&
    (a.kinds.includes('*') || a.kinds.includes(kind)) &&
    (!a.match || literal.indexOf(a.match) >= 0));
}

function* vbFiles(dir) {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, e.name);
    if (e.isDirectory()) {
      if (e.name === 'bin' || e.name === 'obj') continue;
      yield* vbFiles(p);
    } else if (e.name.endsWith('.vb') && !e.name.endsWith('.Designer.vb')) {
      yield p;
    }
  }
}

const violations = [];
let allowedCount = 0;
for (const dir of SCAN_DIRS) {
  const full = path.join(ROOT, dir);
  if (!fs.existsSync(full)) continue;
  for (const file of vbFiles(full)) {
    const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((line, i) => {
      const code = line.trim();
      if (code.startsWith("'") || LINE_EXEMPT.test(code)) return;
      for (const { kind, re } of PATTERNS) {
        const m = re.exec(code);
        if (!m) continue;
        const rel = path.relative(ROOT, file).replace(/\\/g, '/');
        if (isAllowed(file, kind, m[1])) { allowedCount++; }
        else violations.push({ file: rel, line: i + 1, kind, literal: m[1] });
        break; // one finding per line is enough
      }
    });
  }
}

if (violations.length) {
  console.error(`HARDCODED-STRING AUDIT: ${violations.length} violation(s) (${allowedCount} allowlisted):`);
  for (const v of violations)
    console.error(`  ${v.file}:${v.line} [${v.kind}] "${v.literal}"`);
  console.error('Fix via GetString + locales/*.json, or add a REASONED entry to tools/hardcoded-strings.allow.json');
  process.exit(1);
}
console.log(`Hardcoded-string audit clean (${allowedCount} allowlisted).`);
