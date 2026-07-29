// AUDIT: minimal-API VB async lambda footgun.  Tier: publish (exact).
//
// `Async Function(...) As Task(Of IResult)` registered as a Delegate is NOT
// executed by minimal APIs — every branch silently becomes an empty 200 (an
// invalid-PIN settings save once looked like success). Async endpoint lambdas
// must be `As Task` and write the response directly. (Project rule, CLAUDE.md
// "Minimal API endpoints (VB gotcha)".)
'use strict';
const { vbFiles, scanVbLines, finish } = require('./audit-lib');

const RE = /Async\s+Function[^\n]*As\s+Task\(Of\s+IResult\)/;
const violations = [];
for (const file of vbFiles(false)) {
  scanVbLines(file, (line, n, rel) => {
    if (RE.test(line)) violations.push(`${rel}:${n} ${line.trim().slice(0, 100)}`);
  });
}

finish('audit-api-footgun', violations,
  'async endpoint lambdas must be As Task + write the response directly');
