/* All FormMain Designer controls with hardcoded .Text/.PlaceholderText that
   never get a localized assignment at runtime (FormMain*.vb + Controllers).
     node tools/audit-formmain-texts.js */
const fs = require("fs");

const d = fs.readFileSync("EveryTongue/Forms/FormMain.Designer.vb", "utf8");
let rt = "";
for (const f of ["EveryTongue/Forms/FormMain.vb", "EveryTongue/Forms/FormMain.Shell.vb",
    "EveryTongue/Controllers/TranslateController.vb", "EveryTongue/Controllers/BibleController.vb",
    "EveryTongue/Controllers/TranscribeController.vb", "EveryTongue/Controllers/DictationController.vb"]) {
    try { rt += fs.readFileSync(f, "utf8"); } catch (e) { }
}
try {
    for (const f of fs.readdirSync("EveryTongue.Core/Controllers"))
        rt += fs.readFileSync("EveryTongue.Core/Controllers/" + f, "utf8");
} catch (e) { }

const esc = s => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
const skip = /^(⇄|▶|●|—|\.\.\.|&&|[0-9:/.\s]*)$/;

const missing = [];
for (const m of d.matchAll(/^\s+(\w+)\.(Text|PlaceholderText) = "([^"]+)"$/gm)) {
    const [, name, prop, val] = m;
    if (skip.test(val)) continue;
    // localized if ANY runtime file assigns this prop from the language pack
    const re = new RegExp(esc(name) + "\\." + prop + "\\s*=\\s*[^\"\\r\\n]*(GetString|_getString|LanguagePack|lp\\.)");
    if (!re.test(rt)) missing.push(name + "." + prop + ' = "' + val + '"');
}
console.log(missing.length + " unlocalized:");
missing.forEach(x => console.log("  " + x));
