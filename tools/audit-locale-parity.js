// AUDIT: locale-file parity + referenced-key existence.  Tier: publish (exact).
//
// Catches two recurring localisation failures:
//  1. A key added to locales/en.json but not translated into the other locale
//     files (or an orphan key in ca/es that en doesn't have — usually a typo).
//     Locale files are discovered dynamically — the language list is never
//     hardcoded (project rule).
//  2. Code referencing a key that doesn't exist in en.json — GetString("Typo")
//     silently shows the raw key name to users. Scans GetString/_getString/
//     LabelKey literal usages in all VB. (Dynamically-built key names can't be
//     audited; unused keys are NOT flagged for the same reason.)
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, vbFiles, finish } = require('./audit-lib');

const violations = [];
const locDir = path.join(ROOT, 'locales');
const files = fs.readdirSync(locDir).filter(f => f.endsWith('.json'));
const packs = {};
for (const f of files) packs[f] = JSON.parse(fs.readFileSync(path.join(locDir, f), 'utf8'));

const enKeys = Object.keys(packs['en.json']);
const enSet = new Set(enKeys);
for (const f of files) {
  if (f === 'en.json') continue;
  const s = new Set(Object.keys(packs[f]));
  for (const k of enKeys) if (!s.has(k)) violations.push(`locales/${f}: missing key "${k}" (exists in en.json)`);
  for (const k of Object.keys(packs[f])) if (!enSet.has(k)) violations.push(`locales/${f}: orphan key "${k}" (not in en.json)`);
}

const re = /(?:GetString|_getString)\(\s*"([A-Za-z0-9_.]+)"|LabelKey\s*=\s*"([A-Za-z0-9_.]+)"/g;
const reported = new Set();
for (const file of vbFiles(false)) {
  const text = fs.readFileSync(file, 'utf8');
  let m;
  while ((m = re.exec(text))) {
    const key = m[1] || m[2];
    if (!enSet.has(key) && !reported.has(key)) {
      reported.add(key);
      violations.push(`referenced key "${key}" not found in locales/en.json (${path.relative(ROOT, file).replace(/\\/g, '/')})`);
    }
  }
}

finish('audit-locale-parity', violations,
  'add missing translations / fix the key name');
