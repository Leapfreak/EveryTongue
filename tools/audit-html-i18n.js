// audit-html-i18n.js — EXACT tier: wwwroot HTML display text must be localizable.
//
// Inline English in the HTML is FINE as the designed locale-failure fallback —
// but only when something can replace it. An element (or its title/placeholder/
// alt attribute) counts as covered when:
//   a. it carries data-i18n / data-i18n-ph / data-i18n-title / data-i18n-alt, or
//   b. its id (or an ancestor's id, for <option>) is targeted by a t()-based
//      assignment in the paired JS file (getElementById('id')… t( / $("id")… t(),
//      or document.title = t( for the <title> tag), or
//   c. it is listed in tools/html-i18n.allow.json with a written reason
//      (brand names, acronyms, pre-locale bootstrap text).
// Anything else is permanent English for every user — a finding.
'use strict';
const fs = require('fs');
const path = require('path');

const ROOT = path.join(__dirname, '..');
const WWW = path.join(ROOT, 'EveryTongue.Core', 'wwwroot');
const PAIRS = [
  ['index.html', 'js/app.js'],
  ['lobby.html', 'js/lobby.js'],
  ['admin.html', 'js/admin.js'],
];
const ALLOW_PATH = path.join(__dirname, 'html-i18n.allow.json');
const allow = fs.existsSync(ALLOW_PATH) ? JSON.parse(fs.readFileSync(ALLOW_PATH, 'utf8')) : [];
function isAllowed(file, text) {
  return allow.some(a => a.file === file && a.text === text && a.reason);
}

// Words = 2+ consecutive letters; skips emoji/entities/"A+"/numbers.
function hasWords(s) {
  return /[A-Za-z]{2,}/.test(s.replace(/&[a-z]+;|&#\d+;/g, ''));
}

const findings = [];
for (const [htmlFile, jsFile] of PAIRS) {
  const hp = path.join(WWW, htmlFile);
  if (!fs.existsSync(hp)) continue;
  const html = fs.readFileSync(hp, 'utf8');
  const js = fs.readFileSync(path.join(WWW, jsFile), 'utf8');

  // ids the JS localizes with t(): getElementById('x')/$("x") on a line that
  // calls t(). One variable hop is resolved too: `var el=getElementById('x')`
  // followed anywhere by `el.textContent=t(` etc. (statusEl, btnSpeak).
  const localizedIds = new Set();
  const varToId = new Map();
  for (const m of js.matchAll(/(?:var|const|let)\s+([\w$]+)\s*=\s*document\.getElementById\(\s*['"]([\w-]+)['"]\s*\)/g)) {
    varToId.set(m[1], m[2]);
  }
  for (const line of js.split(/\r?\n/)) {
    if (!/\bt\(/.test(line)) continue;
    for (const m of line.matchAll(/getElementById\(\s*['"]([\w-]+)['"]\s*\)/g)) localizedIds.add(m[1]);
    for (const m of line.matchAll(/\$\(\s*['"]([\w-]+)['"]\s*\)/g)) localizedIds.add(m[1]);
    for (const m of line.matchAll(/([\w$]+)\.(?:textContent|innerHTML|title|placeholder|alt|options)\b/g)) {
      if (varToId.has(m[1])) localizedIds.add(varToId.get(m[1]));
    }
  }
  const titleLocalized = /document\.title\s*=\s*t\(/.test(js);

  const lines = html.split(/\r?\n/);
  lines.forEach((line, i) => {
    const lineNo = i + 1;
    // 1. Display attributes: title= / placeholder= / alt=
    for (const m of line.matchAll(/<[^>]*?\s(title|placeholder|alt)\s*=\s*"([^"]+)"[^>]*>/g)) {
      const [tag, attr, val] = m;
      if (!hasWords(val)) continue;
      const di = { title: 'data-i18n-title', placeholder: 'data-i18n-ph', alt: 'data-i18n-alt' }[attr];
      if (tag.includes(di)) continue;
      const idm = tag.match(/\sid\s*=\s*"([\w-]+)"/);
      if (idm && localizedIds.has(idm[1])) continue;
      if (isAllowed(htmlFile, val)) continue;
      findings.push(`${htmlFile}:${lineNo} | ${attr}="${val}" | no ${di}, no t() assignment on its id, not allowlisted`);
    }
    // 2. <title> tag
    const tm = line.match(/<title>([^<]+)<\/title>/);
    if (tm && hasWords(tm[1]) && !titleLocalized && !isAllowed(htmlFile, tm[1])) {
      findings.push(`${htmlFile}:${lineNo} | <title>${tm[1]}</title> | ${jsFile} never sets document.title = t(...)`);
    }
    // 3. Direct element text for display tags
    for (const m of line.matchAll(/<(button|label|option|h[1-6]|th|legend|span|a|div|p)\b([^>]*)>([^<]+)/g)) {
      const [, tagName, attrs, text] = m;
      const trimmed = text.trim();
      if (!hasWords(trimmed)) continue;
      if (/data-i18n\s*=/.test(attrs)) continue;
      // <option value="Arial">Arial</option>: text === value means the text
      // IS the identifier (font families) — not prose.
      if (tagName === 'option') {
        const vm = attrs.match(/\svalue\s*=\s*"([^"]*)"/);
        if (vm && vm[1] === trimmed) continue;
      }
      const idm = attrs.match(/\sid\s*=\s*"([\w-]+)"/);
      if (idm && localizedIds.has(idm[1])) continue;
      if (isAllowed(htmlFile, trimmed)) continue;
      findings.push(`${htmlFile}:${lineNo} | "${trimmed}" | no data-i18n, no t() assignment on its id, not allowlisted`);
    }
    // 4. <option> without data-i18n whose parent <select id> is localized —
    //    handled via allowlist or select-id coverage: options inherit the
    //    nearest preceding <select id="x"> on earlier lines.
  });

  // Options: strip findings whose line sits inside a <select id=X>…</select>
  // where X is a localized id (option texts set via .options[n].textContent).
  const selRanges = [];
  const selRe = /<select\b[^>]*\sid\s*=\s*"([\w-]+)"[^>]*>/g;
  let sm;
  while ((sm = selRe.exec(html)) !== null) {
    const endIdx = html.indexOf('</select>', sm.index);
    if (endIdx < 0) continue;
    if (!localizedIds.has(sm[1])) continue;
    const startLine = html.slice(0, sm.index).split(/\r?\n/).length;
    const endLine = html.slice(0, endIdx).split(/\r?\n/).length;
    selRanges.push([htmlFile, startLine, endLine]);
  }
  for (let i = findings.length - 1; i >= 0; i--) {
    const fm = findings[i].match(/^([^:]+):(\d+)/);
    if (!fm) continue;
    if (selRanges.some(([f, s, e]) => f === fm[1] && +fm[2] >= s && +fm[2] <= e)) findings.splice(i, 1);
  }
}

if (findings.length) {
  console.error(`audit-html-i18n: ${findings.length} finding(s)`);
  for (const f of findings) console.error('  ' + f);
  console.error('Fix via data-i18n*/t() wiring, or add a REASONED entry to tools/html-i18n.allow.json');
  process.exit(1);
}
console.log('audit-html-i18n: clean');
