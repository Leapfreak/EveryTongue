// AUDIT: ES5-only app.js.  Tier: publish (exact).
//
// wwwroot/js/app.js must stay ES5 — it runs on old phone browsers (project
// rule; lobby.js and mic-worklet.js are deliberately exempt, they document
// their own ES6 allowance). Flags: arrow functions, let/const, template
// literals, class declarations, spread/rest.
'use strict';
const fs = require('fs');
const path = require('path');
const { ROOT, finish } = require('./audit-lib');

const FILE = path.join(ROOT, 'EveryTongue.Core', 'wwwroot', 'js', 'app.js');
const CHECKS = [
  { name: 'arrow function', re: /=>/ },
  { name: 'let',            re: /\blet\s+[A-Za-z_$]/ },
  { name: 'const',          re: /\bconst\s+[A-Za-z_$]/ },
  { name: 'template literal', re: /`/ },
  { name: 'class declaration', re: /\bclass\s+[A-Z]/ },
  { name: 'spread/rest',    re: /\.\.\.[A-Za-z_$]/ },
];

const violations = [];
const lines = fs.readFileSync(FILE, 'utf8').split(/\r?\n/);
lines.forEach((line, i) => {
  const t = line.trim();
  if (t.startsWith('//') || t.startsWith('*')) return;
  for (const c of CHECKS) {
    if (c.re.test(line)) {
      violations.push(`app.js:${i + 1} [${c.name}] ${t.slice(0, 90)}`);
      break;
    }
  }
});

finish('audit-es5-appjs', violations, 'app.js is ES5-only (old phone browsers)');
