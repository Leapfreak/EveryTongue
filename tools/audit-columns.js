/* List column headers set to hardcoded strings in Designer files that are
   never re-set from the locale at runtime.  node tools/audit-columns.js */
const fs = require("fs");

const forms = ["FormConnectedClients", "FormDisplayTemplates", "FormDownloadManager",
    "FormEngineTemplates", "FormFilterSets", "FormLogViewer", "FormMain",
    "FormPivotPreview", "FormSpeakerProfiles", "FormTemplateManager"];

const esc = s => s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");

for (const f of forms) {
    const d = fs.readFileSync("EveryTongue/Forms/" + f + ".Designer.vb", "utf8");
    let rt = "";
    for (const sfx of ["", ".Shell"]) {
        try { rt += fs.readFileSync("EveryTongue/Forms/" + f + sfx + ".vb", "utf8"); } catch (e) { }
    }
    const missing = [];
    for (const m of d.matchAll(/(\w+)\.Text = "([^"]+)"/g)) {
        const name = m[1];
        if (!/^col/i.test(name)) continue;
        const re = new RegExp(esc(name) + "\\.Text\\s*=\\s*(GetString|S\\(|_getString|LanguagePack|lp\\.)");
        if (!re.test(rt)) missing.push(name + '("' + m[2] + '")');
    }
    for (const m of d.matchAll(/(\w+)\.HeaderText = "([^"]+)"/g)) {
        const name = m[1];
        const re = new RegExp(esc(name) + "\\.HeaderText\\s*=\\s*(GetString|S\\(|_getString|LanguagePack|lp\\.)");
        if (!re.test(rt)) missing.push(name + '("' + m[2] + '")');
    }
    if (missing.length) console.log(f + ": " + missing.join(", "));
}
