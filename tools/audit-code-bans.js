// AUDIT: banned code patterns.  Tier: publish (exact).
//
// Standing CLAUDE.md bans, previously enforced only by memory:
//  - Debug.WriteLine — stripped from Release builds, production errors go
//    invisible. Use AppLogger.Log(LogEvents.EVENT_ID, msg).
//  - List(Of Object) for known types — use the actual type.
'use strict';
const { vbFiles, scanVbLines, finish } = require('./audit-lib');

const BANS = [
  { name: 'Debug.WriteLine', re: /\bDebug\.WriteLine\s*\(/,
    fix: 'use AppLogger.Log(LogEvents.EVENT_ID, msg)' },
  { name: 'List(Of Object)', re: /\bList\(Of\s+Object\)/,
    fix: 'use the actual element type' },
];

const violations = [];
for (const file of vbFiles(false)) {
  scanVbLines(file, (line, n, rel) => {
    for (const b of BANS) {
      if (b.re.test(line)) {
        violations.push(`${rel}:${n} [${b.name}] ${line.trim().slice(0, 90)} -> ${b.fix}`);
        break;
      }
    }
  });
}

finish('audit-code-bans', violations);
