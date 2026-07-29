// AUDIT: Download Manager coverage.  Tier: full / on-demand (HEURISTIC).
//
// "Everything the app depends on must be deliverable" — a feature that needs
// a tool the Download Manager can't install is a dead end on a fresh machine
// (fresh-install component delivery is a standing project concern).
//
// Two checks:
//  1. DEPENDENCY COVERAGE: every tool executable referenced in code must be
//     mentioned in DependencyManager.vb or FormDownloadManager.vb (i.e. the
//     DM knows how to deliver or at least locate/verify it). System/OS tools
//     are allowlisted with reasons.
//  2. ORPHAN INSTALLERS: every DependencyManager Download*Async must have a
//     caller outside DependencyManager itself — an uncalled installer is a
//     delivery path users can't reach (or dead code).
//
// "…and works" is NOT statically auditable — that's covered at runtime by
// VerifyPaths (startup) + the integrity manifest (checksums.json).
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, vbFiles, finish } = require('./audit-lib');

// System / OS executables the DM must NOT try to deliver (name -> reason).
const ALLOW_EXE = {
  'cmd.exe': 'OS shell',
  'explorer.exe': 'OS shell (open-folder)',
  'tar.exe': 'ships with Windows 10+ (used for archive extraction)',
  'python.exe': 'delivered as part of the python-embed component (DownloadPythonEmbedAsync)',
  'SubtitleEdit.exe': 'deliverable (DownloadSubtitleEditAsync); DM locates it via AppConfig.PathSubtitleEdit, so the literal filename never appears in DM code',
};

const dmFiles = ['EveryTongue.Core/Models/DependencyManager.vb',
                 'EveryTongue/Forms/FormDownloadManager.vb'];
const dmText = dmFiles.map(f => fs.readFileSync(path.join(ROOT, f), 'utf8')).join('\n');

const suspects = [];

// 1. Dependency coverage.
const exeRefs = new Map();  // exe -> first ref location
for (const file of vbFiles(true)) {
  const relF = path.relative(ROOT, file).replace(/\\/g, '/');
  const text = fs.readFileSync(file, 'utf8');
  for (const m of text.matchAll(/"([A-Za-z0-9_.-]+\.exe)"/g)) {
    const exe = m[1];
    if (!exeRefs.has(exe)) exeRefs.set(exe, relF);
  }
}
for (const [exe, where] of exeRefs) {
  if (ALLOW_EXE[exe]) continue;
  if (!dmText.includes(exe)) {
    suspects.push(`dependency "${exe}" (${where}) is not known to the Download Manager — undeliverable on a fresh install`);
  }
}

// 2. Orphan installers. Most are reached via the DownloadToolAsync dispatcher
// (Select Case on ToolState.Name), so "reachable" = referenced anywhere in
// DependencyManager beyond its own declaration, OR called from outside.
const dmCore = fs.readFileSync(path.join(ROOT, dmFiles[0]), 'utf8');
const installers = [...dmCore.matchAll(/Public Async Function (Download\w*Async)\b/g)].map(m => m[1]);
let allOther = '';
for (const file of vbFiles(true)) {
  if (file.endsWith('DependencyManager.vb')) continue;
  allOther += fs.readFileSync(file, 'utf8');
}
for (const name of installers) {
  const internalRefs = (dmCore.match(new RegExp('\\b' + name + '\\b', 'g')) || []).length;
  const outside = new RegExp('\\b' + name + '\\b').test(allOther);
  if (internalRefs <= 1 && !outside) {
    suspects.push(`installer ${name} has NO caller anywhere (not even the dispatcher) — unreachable delivery path`);
  }
}

// 3. Name-matched dispatch: every registered ToolState.Name must have a Case
// in DownloadToolAsync (a tool with no case = "Unknown tool name" AT CLICK
// TIME), and every Case must correspond to a registered tool. Tools that are
// check-only (no download button / delivered elsewhere) are allowlisted.
const CHECK_ONLY = {
  'Python Embedded': 'delivered by DownloadPythonEmbedAsync via its own dedicated UI flow',
  'Python Packages': 'installed via pip flow, not a single download',
  'MMS-TTS (optional)': 'installed via its own dedicated button (btnInstallMmsTts)',
};
const registered = [...dmCore.matchAll(/\.Name = "([^"]+)"/g)].map(m => m[1]);
const dispatchBlock = (dmCore.match(/Function DownloadToolAsync[\s\S]*?End Function/) || [''])[0];
const cases = [...dispatchBlock.matchAll(/Case "([^"]+)"/g)].map(m => m[1]);
for (const name of registered) {
  if (CHECK_ONLY[name]) continue;
  if (!cases.includes(name)) {
    suspects.push(`tool "${name}" is registered but has NO Case in DownloadToolAsync — Install fails with "Unknown tool name" at click time`);
  }
}
for (const c of cases) {
  if (!registered.includes(c)) {
    suspects.push(`dispatcher Case "${c}" matches NO registered tool name — dead branch or a renamed tool`);
  }
}

// 4. PYTHON PACKAGE COVERAGE: every third-party import in shipped sidecar .py
// files must appear in some requirements*.txt, or the feature works on the dev
// box (where someone pip-installed it once) and silently degrades on a fresh
// install. Field case 2026-07-31: notes_names.py imported pypdf — present on
// dev, in NO requirements file — so Jezer's PDF name extraction fell back to
// a garbage path. Same failure class as an undeliverable exe.
const PY_DIRS = ['live-server', 'translate-server', 'mms-tts-server', 'qe-server'];
const STDLIB = new Set(('sys os io re json time math logging threading queue zipfile subprocess tempfile wave argparse asyncio urllib collections enum dataclasses typing pathlib functools itertools random struct socket ssl hashlib base64 shutil glob unicodedata contextlib traceback warnings inspect abc copy string textwrap datetime signal ctypes platform importlib http email zlib codecs encodings concurrent difflib sqlite3 gc uuid statistics numbers array bisect heapq pickle csv').split(' '));
// Delivered outside requirements files (name -> reason).
const ALLOW_PY = {
  torch: 'delivered inside the whisper-stack / SaT components, not pip',
  silero_vad: 'delivered with the whisper stack (silero-vad pip name differs from import name)',
  transformers: 'delivered inside the NLLB / MMS model components',
  wtpsplit: 'delivered inside the SaT component (sat-libs)',
  google: 'google-cloud-speech distribution (import name differs)',
  speechmatics: 'speechmatics-rt distribution (import name differs)',
  azure: 'azure-cognitiveservices-speech distribution (import name differs)',
  engines: 'local package', vad: 'local package', sat_segmenter: 'local module',
  pace_tuner: 'local module', common: 'local module', server: 'local module',
};
const reqText = [];
for (const d of PY_DIRS) {
  for (const f of ['requirements.txt', 'requirements-lite.txt']) {
    const p = path.join(ROOT, d, f);
    if (fs.existsSync(p)) reqText.push(fs.readFileSync(p, 'utf8'));
  }
}
const reqAll = reqText.join('\n').toLowerCase();
const pyImports = new Map();   // module -> first ref
for (const d of PY_DIRS) {
  const full = path.join(ROOT, d);
  if (!fs.existsSync(full)) continue;
  for (const file of require('./audit-lib').walkFiles(full, ['.py'])) {
    const relF = path.relative(ROOT, file).replace(/\\/g, '/');
    for (const line of fs.readFileSync(file, 'utf8').split(/\r?\n/)) {
      const m = /^\s*(?:from\s+([A-Za-z_][\w]*)|import\s+([A-Za-z_][\w]*))/.exec(line);
      if (!m) continue;
      const mod = (m[1] || m[2]);
      if (!pyImports.has(mod)) pyImports.set(mod, relF);
    }
  }
}
for (const [mod, where] of pyImports) {
  if (STDLIB.has(mod) || ALLOW_PY[mod]) continue;
  const norm = mod.toLowerCase().replace(/_/g, '-');
  if (!reqAll.includes(mod.toLowerCase()) && !reqAll.includes(norm)) {
    suspects.push(`python package "${mod}" (${where}) is in NO requirements*.txt — works on dev, undeliverable on a fresh install`);
  }
}

finish('audit-download-manager', suspects,
  'HEURISTIC — add a DM delivery/check for the tool, wire the installer into the UI, add the package to requirements, or allowlist with a reason');
