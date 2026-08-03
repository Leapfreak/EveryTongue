// AUDIT: static language lists.  Tier: full / on-demand (HEURISTIC — findings
// are SUSPECTS for human review, not automatic violations).
//
// Project rule: language lists/maps are NEVER static — the canonical table is
// wwwroot/data/language-codes.json; VB goes through SttBackendRegistry.
// EffectiveLanguages, python engines through common.vendor_locale. This
// auditor flags source lines that look like inline language-code collections
// (3+ distinct quoted ISO 639-1 codes, or 2+ FLORES codes, on one line).
//
// Sanctioned homes are allowlisted below WITH the reason — everything else
// deserves a look.
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, walkFiles, rel, finish } = require('./audit-lib');

// Sanctioned files (basename -> reason).
const ALLOW = {
  'SttBackendRegistry.vb': 'Entry.SupportedLanguages is the ONE sanctioned home for engine-declared lists',
  'TranslationService.vb': 'WhisperToFloresLang canonical code map (language-codes.json is the web-side twin)',
};

// Sanctioned LINES (substring/regex -> reason). Reviewed 2026-07-29.
const ALLOW_LINES = [
  { re: /_supported As New HashSet|"gl", "hi"|"no", "pl"|"cmn", "cs"/,
    reason: 'SpeechmaticsTranslation vendor capability list (inline translation retired; revisit if resurrected)' },
  { re: /POPULAR_LANGS/,
    reason: 'curated UX ordering for the web language picker, not a capability list' },
  { re: /DEFAULT_AUTODETECT/,
    reason: 'Azure caps autodetect candidates (~10); a curated default choice, not derivable' },
  { re: /"(cyrillic|cjk|thai|georgian|armenian)"/,
    reason: 'script-family constants (linguistic facts, not vendor capabilities)' },
  { re: /"de", "en", "es", "fr", "it", "ja", "ko", "zh"/,
    reason: 'DeepL custom_instructions vendor constraint (_instructionTargets) — the API accepts instructions only for these target families; a vendor fact, not a language list' },
];

const ISO = new Set(('en es ca fr de it pt nl ru zh ja ko ar he pl tr uk ro cs el hi sv no da fi hu id vi th bg sk hr sr lt lv et sl').split(' '));
const QUOTED = /["']([a-z]{2,3}(?:_[A-Z][a-z]{3})?)["']/g;

const scanRoots = [
  ['EveryTongue.Core', ['.vb', '.js']],
  ['EveryTongue', ['.vb']],
  ['EveryTongue.Lite', ['.vb']],
  ['live-server', ['.py']],
  ['translate-server', ['.py']],
  ['mms-tts-server', ['.py']],
  ['qe-server', ['.py']],
];

const suspects = [];
for (const [dir, exts] of scanRoots) {
  const full = path.join(ROOT, dir);
  if (!fs.existsSync(full)) continue;
  for (const file of walkFiles(full, exts)) {
    const base = path.basename(file);
    if (ALLOW[base]) continue;
    const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
    lines.forEach((line, i) => {
      const t = line.trim();
      if (t.startsWith("'") || t.startsWith('//') || t.startsWith('#')
          || t.startsWith('/*') || t.startsWith('*')) return;
      if (ALLOW_LINES.some(a => a.re.test(line))) return;
      let m, iso = new Set(), flores = 0;
      QUOTED.lastIndex = 0;
      while ((m = QUOTED.exec(line))) {
        if (m[1].includes('_')) flores++;
        else if (ISO.has(m[1])) iso.add(m[1]);
      }
      if (iso.size >= 3 || flores >= 2) {
        suspects.push(`${rel(file)}:${i + 1} (${iso.size} ISO / ${flores} FLORES codes) ${t.slice(0, 90)}`);
      }
    });
  }
}

finish('audit-language-lists', suspects,
  'HEURISTIC suspects — verify each reads from language-codes.json/registry, or fix on sight (project rule)');
