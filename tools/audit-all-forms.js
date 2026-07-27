/* Every Designer control string across ALL forms that never gets a localized
   assignment at runtime.  node tools/audit-all-forms.js */
const fs = require("fs");

const dir = "EveryTongue/Forms";
const esc = s => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
// Skip: symbols, acronym-ish tokens, numbers, ellipsis-only, pangrams (data)
const skip = /^(⇄|▶|●|—|\.\.\.|&&|[0-9:/.,\s]*|SRT|VTT|TXT|JSON|CSV|LRC|CUDA|Vulkan|CPU|GPU|RAM|faster-whisper|OK|QR|PIN|Every Tongue|The quick brown fox.*|1(, \d+)*.*)$/;
// Deliberately unlocalized forms
const exempt = new Set(["FormLanguagePicker"]);

let rtAll = "";
for (const f of ["../Controllers"]) {
    try { for (const x of fs.readdirSync(dir + "/" + f)) rtAll += fs.readFileSync(dir + "/" + f + "/" + x, "utf8"); } catch (e) { }
}
try { for (const x of fs.readdirSync("EveryTongue/Controllers")) rtAll += fs.readFileSync("EveryTongue/Controllers/" + x, "utf8"); } catch (e) { }

let total = 0;
for (const f of fs.readdirSync(dir)) {
    if (!f.endsWith(".Designer.vb")) continue;
    const base = f.replace(".Designer.vb", "");
    if (exempt.has(base)) continue;
    const d = fs.readFileSync(dir + "/" + f, "utf8");
    let rt = rtAll;
    for (const sfx of [".vb", ".Shell.vb"]) {
        try { rt += fs.readFileSync(dir + "/" + base + sfx, "utf8"); } catch (e) { }
    }
    const missing = [];
    for (const m of d.matchAll(/(?:Me\.)?(\w+)\.(Text|HeaderText|PlaceholderText) = "([^"]+)"/g)) {
        const [, name, prop, val] = m;
        if (skip.test(val)) continue;
        const re = new RegExp("(?:Me\\.)?" + esc(name) + "\\." + prop + "\\s*=\\s*[^\"\\r\\n]*(GetString|_getString|LanguagePack|lp\\.|S\\()");
        if (!re.test(rt)) missing.push(name + "." + prop + '="' + val + '"');
    }
    if (missing.length) { total += missing.length; console.log(base + " (" + missing.length + "):"); missing.forEach(x => console.log("   " + x)); }
}
console.log("\nTOTAL unlocalized: " + total);
