// gen-number-words.js — generates the Bible_NumberWords full enumeration (1-200,
// every number spelled out as its complete spoken phrase) for each locale file,
// and line-inserts the three spoken-reference keys at their anchor positions:
//   Bible_SpokenBookNames / Bible_SpokenVerseWords  -> after Bible_SpokenChapterWords
//   Bible_NumberWords                               -> after Bible_Ordinals
// Idempotent: a key already present is left untouched (delete the line and re-run
// to regenerate). Regenerating and diffing IS the audit for list correctness.
// The detection layer folds accents and case at compare time, so lists carry
// canonical accented forms only.
'use strict';
const fs = require('fs');
const path = require('path');

// ── Spanish ─────────────────────────────────────────────────────────────
function esRest(n) { // 1..99 -> single spoken form
  const units = ['', 'uno', 'dos', 'tres', 'cuatro', 'cinco', 'seis', 'siete', 'ocho', 'nueve',
    'diez', 'once', 'doce', 'trece', 'catorce', 'quince', 'dieciséis', 'diecisiete', 'dieciocho', 'diecinueve'];
  const veinti = ['veinte', 'veintiuno', 'veintidós', 'veintitrés', 'veinticuatro', 'veinticinco',
    'veintiséis', 'veintisiete', 'veintiocho', 'veintinueve'];
  const tens = { 30: 'treinta', 40: 'cuarenta', 50: 'cincuenta', 60: 'sesenta', 70: 'setenta', 80: 'ochenta', 90: 'noventa' };
  if (n <= 19) return units[n];
  if (n <= 29) return veinti[n - 20];
  const t = Math.floor(n / 10) * 10, u = n % 10;
  return u === 0 ? tens[t] : `${tens[t]} y ${units[u]}`;
}
function genEs() {
  const out = [];
  for (let n = 1; n <= 99; n++) out.push([esRest(n), n]);
  out.push(['cien', 100]);
  for (let n = 101; n <= 199; n++) out.push([`ciento ${esRest(n - 100)}`, n]);
  out.push(['doscientos', 200]);
  return out;
}

// ── Catalan ─────────────────────────────────────────────────────────────
function caRest(n) { // 2..99 -> array of spoken forms (hyphenated canonical + spaced STT variant)
  const units = ['', 'un', 'dos', 'tres', 'quatre', 'cinc', 'sis', 'set', 'vuit', 'nou',
    'deu', 'onze', 'dotze', 'tretze', 'catorze', 'quinze', 'setze', 'disset', 'divuit', 'dinou'];
  const tens = { 20: 'vint', 30: 'trenta', 40: 'quaranta', 50: 'cinquanta', 60: 'seixanta', 70: 'setanta', 80: 'vuitanta', 90: 'noranta' };
  if (n <= 19) return n === 2 ? ['dos', 'dues'] : [units[n]];
  const t = Math.floor(n / 10) * 10, u = n % 10;
  if (u === 0) return [tens[t]];
  const uForms = u === 1 ? ['un', 'u'] : u === 2 ? ['dos', 'dues'] : [units[u]];
  const res = [];
  for (const uf of uForms) {
    if (t === 20) res.push(`vint-i-${uf}`, `vint i ${uf}`);
    else res.push(`${tens[t]}-${uf}`, `${tens[t]} ${uf}`);
  }
  return res;
}
function genCa() {
  // standalone 1 ("u"/"un") excluded: article collision — chapter/verse 1 in
  // Catalan arrives as a digit or via Bible_Ordinals ("primer")
  const out = [];
  for (let n = 2; n <= 99; n++) for (const f of caRest(n)) out.push([f, n]);
  out.push(['cent', 100]);
  for (let n = 101; n <= 199; n++) {
    const r = n - 100;
    const rests = r === 1 ? ['un', 'u'] : caRest(r);
    for (const f of rests) out.push([`cent ${f}`, n]);
  }
  out.push(['dos-cents', 200], ['dos cents', 200]);
  return out;
}

// ── English ─────────────────────────────────────────────────────────────
function enRest(n) { // 1..99 -> array of forms (hyphenated + spaced for compounds)
  const units = ['', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine',
    'ten', 'eleven', 'twelve', 'thirteen', 'fourteen', 'fifteen', 'sixteen', 'seventeen', 'eighteen', 'nineteen'];
  const tens = { 20: 'twenty', 30: 'thirty', 40: 'forty', 50: 'fifty', 60: 'sixty', 70: 'seventy', 80: 'eighty', 90: 'ninety' };
  if (n <= 19) return [units[n]];
  const t = Math.floor(n / 10) * 10, u = n % 10;
  if (u === 0) return [tens[t]];
  return [`${tens[t]}-${units[u]}`, `${tens[t]} ${units[u]}`];
}
function genEn() {
  const out = [];
  for (let n = 1; n <= 99; n++) for (const f of enRest(n)) out.push([f, n]);
  out.push(['hundred', 100], ['one hundred', 100], ['a hundred', 100]);
  for (let n = 101; n <= 199; n++) {
    const hyph = enRest(n - 100)[0];               // hyphenated/base form
    for (const f of enRest(n - 100)) out.push([`one hundred and ${f}`, n]);
    out.push([`a hundred and ${hyph}`, n], [`one hundred ${hyph}`, n], [`hundred and ${hyph}`, n]);
  }
  out.push(['two hundred', 200]);
  return out;
}

// ── Emit + insert ───────────────────────────────────────────────────────
const LOCALE_KEYS = {
  'en.json': {
    Bible_SpokenBookNames: 'psalm:230',
    Bible_SpokenVerseWords: 'verse,verses',
    Bible_NumberWords: genEn(),
  },
  'es.json': {
    Bible_SpokenBookNames: 'salmo:230',
    Bible_SpokenVerseWords: 'versículo,versículos',
    Bible_NumberWords: genEs(),
  },
  'ca.json': {
    Bible_SpokenBookNames: 'salm:230',
    Bible_SpokenVerseWords: 'verset,versets',
    Bible_NumberWords: genCa(),
  },
};

const localeDir = path.join(__dirname, '..', 'locales');
let changed = 0;
for (const [file, keys] of Object.entries(LOCALE_KEYS)) {
  const p = path.join(localeDir, file);
  let lines = fs.readFileSync(p, 'utf8').split('\n');
  const has = (k) => lines.some(l => l.includes(`"${k}"`));
  const insertAfter = (anchorKey, newLine) => {
    const i = lines.findIndex(l => l.includes(`"${anchorKey}"`));
    if (i < 0) throw new Error(`${file}: anchor ${anchorKey} not found`);
    lines.splice(i + 1, 0, newLine);
  };
  const numberLine = keys.Bible_NumberWords.map(([w, v]) => `${w}:${v}`).join(',');
  const inserts = [
    ['Bible_SpokenChapterWords', 'Bible_SpokenVerseWords', keys.Bible_SpokenVerseWords],
    ['Bible_SpokenChapterWords', 'Bible_SpokenBookNames', keys.Bible_SpokenBookNames],
    ['Bible_Ordinals', 'Bible_NumberWords', numberLine],
  ];
  for (const [anchor, key, value] of inserts) {
    if (has(key)) continue;
    insertAfter(anchor, `  "${key}": "${value}",`);
    changed++;
    console.log(`${file}: inserted ${key} (${value.length} chars, ${key === 'Bible_NumberWords' ? keys.Bible_NumberWords.length + ' entries' : 'ok'})`);
  }
  fs.writeFileSync(p, lines.join('\n'), 'utf8');
}
console.log(changed ? `done: ${changed} insertions` : 'done: nothing to do (all keys present)');
