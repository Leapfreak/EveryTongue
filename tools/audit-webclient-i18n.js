// audit-webclient-i18n.js — EXACT tier: web-client locale-key parity.
//
// The web client localizes via inline fallback tables (T in app.js, LT in
// lobby.js/admin.js) overlaid by /api/locale (which serves en.json's web.*
// keys, prefix stripped). A key that is USED but missing from the fallback
// table renders as the raw key name when the locale fetch fails (field bug:
// t('feedbackTitle') → literal "feedbackTitle"). A key missing from en.json
// is NEVER localized for non-English users — permanent English. Both are
// mechanical, zero-false-positive checks:
//
//   1. every t('key') / fmt('key') use  → key in that file's fallback table
//   2. every data-i18n / data-i18n-ph / data-i18n-title in the HTML file
//      → key in the paired JS fallback table
//   3. every used key AND every fallback-table key → web.<key> in en.json
//      (table→en.json catches keys only used via dynamic construction)
//
// Dynamic keys built at runtime ('lbType'+x, 'err_'+code) can't be extracted
// from call sites — direction 3 still covers them because the constructed
// keys must sit in the fallback table.
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const WWW = path.join(ROOT, 'EveryTongue.Core', 'wwwroot');

// file pairs: [jsFile, tableVarName, htmlFiles[]]
const UNITS = [
  ['js/app.js',   'T',  ['index.html']],
  ['js/lobby.js', 'LT', ['lobby.html']],
  ['js/admin.js', 'LT', ['admin.html']],
];

function read(p) { return fs.readFileSync(path.join(WWW, p), 'utf8'); }

// Extract the keys of `var T={...}` / `const LT = {...}` via brace matching.
function tableKeys(src, varName, file) {
  const m = src.match(new RegExp('(?:var|const|let)\\s+' + varName + '\\s*=\\s*\\{'));
  if (!m) throw new Error(`${file}: fallback table ${varName} not found`);
  let i = m.index + m[0].length, depth = 1, body = '';
  while (i < src.length && depth > 0) {
    const c = src[i];
    if (c === '{') depth++;
    else if (c === '}') depth--;
    if (depth > 0) body += c;
    i++;
  }
  const keys = new Set();
  // keys at depth 0 of the object body: ident: or "ident": or 'ident':
  // (values are all string literals in these tables — strip them first so
  // colons inside text can't fabricate keys)
  const stripped = body.replace(/'(?:[^'\\]|\\.)*'/g, "''").replace(/"(?:[^"\\]|\\.)*"/g, '""');
  for (const km of stripped.matchAll(/(?:^|[,{])\s*['"]?([A-Za-z_][A-Za-z0-9_]*)['"]?\s*:/gm)) {
    keys.add(km[1]);
  }
  return keys;
}

function usedKeys(src) {
  const used = new Set();
  // [),] excludes dynamic constructions like t('err_'+code) — those are
  // covered by the table→en.json direction (constructed keys sit in the table).
  for (const m of src.matchAll(/\b(?:t|fmt)\(\s*['"]([A-Za-z_][A-Za-z0-9_]*)['"]\s*[),]/g)) used.add(m[1]);
  return used;
}

function htmlKeys(src) {
  const used = new Set();
  for (const m of src.matchAll(/data-i18n(?:-ph|-title)?\s*=\s*"([A-Za-z_][A-Za-z0-9_]*)"/g)) used.add(m[1]);
  return used;
}

const enJson = JSON.parse(fs.readFileSync(path.join(ROOT, 'locales', 'en.json'), 'utf8'));
const webKeys = new Set(Object.keys(enJson).filter(k => k.startsWith('web.')).map(k => k.slice(4)));

const findings = [];
for (const [jsFile, varName, htmls] of UNITS) {
  const src = read(jsFile);
  const table = tableKeys(src, varName, jsFile);
  const used = usedKeys(src);
  for (const h of htmls) {
    if (!fs.existsSync(path.join(WWW, h))) continue;
    for (const k of htmlKeys(read(h))) used.add(k);
  }
  for (const k of used) {
    if (!table.has(k)) findings.push(`${jsFile}: key '${k}' used but missing from ${varName} fallback table (raw key renders on locale-fetch failure)`);
    if (!webKeys.has(k)) findings.push(`${jsFile}: key '${k}' used but 'web.${k}' missing from locales/en.json (never localized)`);
  }
  for (const k of table) {
    if (!webKeys.has(k)) findings.push(`${jsFile}: ${varName} fallback key '${k}' has no 'web.${k}' in locales/en.json (never localized)`);
  }
}

if (findings.length) {
  console.error(`audit-webclient-i18n: ${findings.length} finding(s)`);
  for (const f of findings) console.error('  ' + f);
  process.exit(1);
}
console.log('audit-webclient-i18n: clean');
