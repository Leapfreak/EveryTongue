// AUDIT: process pipe redirect without a visible drain.  Tier: full /
// on-demand (HEURISTIC — findings are SUSPECTS for human review).
//
// The 4KB Windows pipe buffer rule (CLAUDE.md "Process pipe buffer"):
// redirecting stdout/stderr and then WaitForExit WITHOUT draining BOTH pipes
// deadlocks the child once the buffer fills (whisper-server froze after ~7
// calls this way). This auditor flags each FILE that sets
// RedirectStandardOutput/Error = True and calls WaitForExit, listing which
// drain evidence it could find — a file with a redirect and NO drain tokens
// is almost certainly wrong; one with partial evidence deserves a read.
'use strict';
const fs = require('fs');
const { vbFiles, rel, finish } = require('./audit-lib');

// Per-pipe drain evidence: a redirected pipe must show a read somewhere in the
// same file (sync/async read, event-based read, or raw BaseStream pumping).
function drained(text, pipe) {
  const evented = pipe === 'Output'
    ? /BeginOutputReadLine|OutputDataReceived/
    : /BeginErrorReadLine|ErrorDataReceived/;
  const direct = new RegExp(`Standard${pipe}\\s*\\.\\s*(Read|BaseStream)`);
  return direct.test(text) || evented.test(text);
}

const suspects = [];
for (const file of vbFiles(false)) {
  const text = fs.readFileSync(file, 'utf8');
  const redirOut = /RedirectStandardOutput\s*=\s*True/.test(text);
  const redirErr = /RedirectStandardError\s*=\s*True/.test(text);
  if (!redirOut && !redirErr) continue;
  if (!/WaitForExit/.test(text)) continue;   // fire-and-forget: different pattern
  const undrained = [];
  if (redirOut && !drained(text, 'Output')) undrained.push('stdout');
  if (redirErr && !drained(text, 'Error')) undrained.push('stderr');
  if (undrained.length) {
    suspects.push(`${rel(file)} redirects ${[redirOut && 'stdout', redirErr && 'stderr'].filter(Boolean).join('+')} `
      + `with WaitForExit, but no read of: ${undrained.join(', ')}`);
  }
}

finish('audit-pipe-drain', suspects,
  'HEURISTIC suspects — verify BOTH pipes are drained before WaitForExit (see SttConcurrencyRunner.vb pattern)');
