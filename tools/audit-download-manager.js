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

finish('audit-download-manager', suspects,
  'HEURISTIC — add a DM delivery/check for the tool, wire the installer into the UI, or allowlist with a reason');
