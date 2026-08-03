// AUDIT: AppConfig hygiene.  Tier: full / on-demand (HEURISTIC).
//
// Two failure classes for config properties (132 and counting):
//  1. DEAD — a property no code reads or writes outside AppConfig.vb itself.
//     It deserializes from config.json into nothing. Delete it (removing a
//     property is JSON-safe: unknown keys are ignored on load).
//  2. NO UI — a property the code DOES use, but which no dialog, wizard,
//     descriptor field, or settings endpoint ever touches: the only way to
//     change it is hand-editing config.json. Each is either a missing Options
//     control or a deliberate power-user knob — decide and allowlist.
//
// UI surfaces recognized: Forms/*.vb (Options, wizards, managers, log config),
// engine config descriptor blocks (Services/Stt/Configs, auto-generate UI),
// EndpointRegistration.Settings.vb (web settings UI), admin/web JS via
// /api/settings JSON names.
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, vbFiles, rel, finish } = require('./audit-lib');

// Deliberate file-only / internal-state properties (name -> reason).
const ALLOW_NO_UI = {
  GoogleCloudSttApiKey: 'legacy migration shim — ConfigManager moves it into SttApiKeys and nulls it; must never get UI',
  DictationDeviceIndex: 'deliberate file-only fallback — the UI stores the device by NAME (DictationDeviceName); index is the power-user override (DictationService resolution order)',
  // Translation context system v1 (branch translation-context, 2026-08-04):
  // deliberately file-only until the context dividend is proven on a live
  // service — FormOptions UI + locale keys are the deferred follow-up (PLAN.md).
  TranslationContextEnabled: 'context system v1 — UI deferred until live validation (PLAN.md)',
  TranslationContextSentences: 'context system v1 — UI deferred until live validation (PLAN.md)',
  TranslationContextMaxChars: 'context system v1 — UI deferred until live validation (PLAN.md)',
  TranslationContextMaxAgeMinutes: 'context system v1 — UI deferred until live validation (PLAN.md)',
  TranslationTerminologyEnabled: 'context system v1 — UI deferred until live validation (PLAN.md)',
  TranslationTerminologyPath: 'context system v1 — UI deferred until live validation (PLAN.md)',
};

const cfgPath = path.join(ROOT, 'EveryTongue.Core', 'Models', 'AppConfig.vb');
const cfgText = fs.readFileSync(cfgPath, 'utf8');
const props = [];
for (const m of cfgText.matchAll(/^\s*Public Property (\w+)\b/gm)) props.push(m[1]);

// Partition all VB files into: config-internal, UI surfaces, other code.
const UI_FILE = f => /[\\/]Forms[\\/]|[\\/]Configs[\\/]|EndpointRegistration\.Settings\.vb$/.test(f);
const CONFIG_FILE = f => /AppConfig\.vb$/.test(f);   // ConfigManager reads (migrations) count as real usage

const uiText = [];
const codeText = [];
for (const f of vbFiles(true)) {
  if (CONFIG_FILE(f)) continue;
  (UI_FILE(f) ? uiText : codeText).push(fs.readFileSync(f, 'utf8'));
}
// Web settings UI reads JSON property names (camelCase == PascalCase-insensitive).
for (const jsFile of ['admin.js', 'app.js', 'lobby.js']) {
  const p = path.join(ROOT, 'EveryTongue.Core', 'wwwroot', 'js', jsFile);
  if (fs.existsSync(p)) uiText.push(fs.readFileSync(p, 'utf8'));
}
const uiAll = uiText.join('\n');
const codeAll = codeText.join('\n');

// Helper-method indirection: dictionary-style properties are exposed through
// AppConfig accessors (GetTranslationCharBudget etc.) — a property's UI-ness
// and aliveness follow its HELPERS. Map each property to the AppConfig methods
// whose bodies reference it.
const methods = [];
const methodRe = /^\s*Public (?:Shared )?(?:Function|Sub) (\w+)\b[\s\S]*?^\s*End (?:Function|Sub)/gm;
let mm;
while ((mm = methodRe.exec(cfgText))) methods.push({ name: mm[1], body: mm[0] });
function helpersOf(p) {
  const re = new RegExp('\\b' + p + '\\b');
  return methods.filter(m => re.test(m.body)).map(m => m.name);
}

const dead = [];
const noUi = [];
for (const p of props) {
  const re = new RegExp('\\b' + p + '\\b', 'i');   // case-insensitive: JSON camelCase
  const hs = helpersOf(p);
  const viaHelper = t => hs.some(h => new RegExp('\\b' + h + '\\b').test(t));
  const inCode = re.test(codeAll) || viaHelper(codeAll);
  const inUi = re.test(uiAll) || viaHelper(uiAll);
  if (!inCode && !inUi) dead.push(p);
  else if (!inUi && !ALLOW_NO_UI[p]) noUi.push(p);
}

const suspects = [];
for (const p of dead) suspects.push(`DEAD: ${p} — no reads/writes anywhere outside AppConfig.vb`);
for (const p of noUi) suspects.push(`NO-UI: ${p} — used by code but only changeable by hand-editing config.json`);

finish('audit-config', suspects,
  'DEAD -> delete the property; NO-UI -> add an Options control or allowlist with a reason');
