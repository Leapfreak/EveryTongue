/* Three-page IA restructure: app.js + index.html surgery.
   Removes the in-client admin (settings overlay, picker PIN login, Live remote
   panel) now that /admin.html owns it; rewires home/roomEnded per tier; adds
   the ?join= template-pointer waiting flow and the host-panel Admin button. */
const fs = require('fs');

function cutBetween(src, startMarker, endMarker, replacement, label) {
    const a = src.indexOf(startMarker);
    if (a < 0) throw new Error('start marker not found: ' + label);
    const b = src.indexOf(endMarker, a);
    if (b < 0) throw new Error('end marker not found: ' + label);
    console.log('cut [' + label + ']: chars ' + a + '..' + b);
    return src.slice(0, a) + replacement + src.slice(b);
}
function mustReplace(src, from, to, label) {
    if (!src.includes(from)) throw new Error('replace marker not found: ' + label);
    console.log('replace [' + label + ']');
    return src.replace(from, to);
}

// ── app.js ──
let js = fs.readFileSync('EveryTongue.Core/wwwroot/js/app.js', 'utf8');

// 1. Admin access block + openServerSettings + buildSettingsOverlay → compact stub.
js = cutBetween(js,
    "/* ── Admin access control ── */",
    "var rOpts=rateSelect.options;",
    "/* ── Admin moved to /admin.html (three-tier IA): the picker's Administrator\n" +
    "   link navigates there; ALL server config UI lives on that page now. ── */\n" +
    "localStorage.removeItem('isAdmin');\n" +
    "var serverPublicHost='';\n" +
    "var serverHasLiveSession=false;\n" +
    "fetch('/api/config').then(function(r){return r.json()}).then(function(cfg){\n" +
    "  serverPublicHost=cfg.publicHost||'';\n" +
    "  serverHasLiveSession=!!cfg.hasLiveSession;\n" +
    "  var lpa=document.getElementById('lpAdmin');\n" +
    "  if(lpa)lpa.style.display='';\n" +
    "}).catch(function(){});\n" +
    "function openAdminPage(){location.href='/admin.html'}\n\n",
    'admin block + settings overlay');

// 2. Admin remote-control panel block (moved to admin.html's Live card).
js = cutBetween(js,
    "/* ── Admin remote control ── */",
    "/* ── Bible Panel ── */",
    "/* Admin remote control moved to /admin.html (Live session card). */\n\n",
    'admin remote panel');

// 3. closeAllPanels / outside-click: drop adminPanel references.
js = mustReplace(js,
    "function closeAllPanels(){panel.style.display='none';adminPanel.style.display='none';if(adminPollTimer){clearInterval(adminPollTimer);adminPollTimer=null}var hp=document.getElementById('hostPanel');",
    "function closeAllPanels(){panel.style.display='none';var hp=document.getElementById('hostPanel');",
    'closeAllPanels');
js = mustReplace(js,
    "if(!panel.contains(e.target)&&!adminPanel.contains(e.target)&&(!hp||!hp.contains(e.target))",
    "if(!panel.contains(e.target)&&(!hp||!hp.contains(e.target))",
    'outside click');

// 4. btnAdmin title + lpAdminPin placeholder i18n lines (elements removed).
js = mustReplace(js, "document.getElementById('btnAdmin').title=t('remote');\n", '', 'btnAdmin title');
js = mustReplace(js, "document.getElementById('lpAdminPin').placeholder=t('adminPin');\n", '', 'lpAdminPin placeholder');

// 5. goHome per tier + afterRoomGone.
js = mustReplace(js,
    "function goHome(){\n  LOG('goHome');\n  ssRemove('langChosen');\n  /* Navigate to lobby */\n  window.location.href=window.location.origin+'/lobby.html';\n}",
    "function goHome(){\n" +
    "  LOG('goHome');\n" +
    "  /* Three-tier IA: volunteers (device holds the host-tools code) go back to\n" +
    "     the lobby; for guests \"home\" is re-choosing their language — the entry\n" +
    "     picker is a right, and there is no guest landing page to go back to. */\n" +
    "  if(localStorage.getItem('creatorCode')){ssRemove('langChosen');window.location.href=window.location.origin+'/lobby.html';return}\n" +
    "  showLangPicker();\n" +
    "}\n" +
    "/* Where to land when the room is gone (ended/kicked): permanent-QR guests\n" +
    "   re-resolve their template (a restarted service heals seamlessly),\n" +
    "   volunteers return to the lobby, other guests get the language picker. */\n" +
    "function afterRoomGone(){\n" +
    "  var tpl=sessionStorage.getItem('joinTpl');\n" +
    "  if(tpl){location.replace('/index.html?join='+encodeURIComponent(tpl));return}\n" +
    "  if(localStorage.getItem('creatorCode')){location.href='/lobby.html';return}\n" +
    "  showLangPicker();\n" +
    "}",
    'goHome');

// 6. roomClosed / kicked → afterRoomGone.
js = mustReplace(js,
    "else if(msg.type==='roomClosed'){stopBroadcast(false);showRoomError(t('roomEnded'));setTimeout(function(){location.href='/lobby.html'},3000)}",
    "else if(msg.type==='roomClosed'){stopBroadcast(false);showRoomError(t('roomEnded'));setTimeout(afterRoomGone,3000)}",
    'roomClosed');
js = mustReplace(js,
    "else if(msg.type==='kicked'){showRoomError(t('roomKicked'));setTimeout(function(){location.href='/lobby.html'},3000)}",
    "else if(msg.type==='kicked'){showRoomError(t('roomKicked'));setTimeout(afterRoomGone,3000)}",
    'kicked');

// 7. ?join= template-pointer flow: waiting overlay + poll, before the picker IIFE.
js = mustReplace(js,
    "  /* Show language picker on page load — skip if desktop app passed ?bibleLang= */\n" +
    "  var _qs=new URLSearchParams(window.location.search);\n" +
    "  if(!_qs.get('bibleLang')){showLangPicker()}",
    "  /* Show language picker on page load — skip for ?bibleLang= (desktop panel)\n" +
    "     and ?join= (template-pointer waiting flow shows its own overlay first) */\n" +
    "  var _qs=new URLSearchParams(window.location.search);\n" +
    "  if(!_qs.get('bibleLang')&&!_qs.get('join')){showLangPicker()}",
    'picker skip on join');

js = mustReplace(js,
    "/* ── Keep screen on (Wake Lock)",
    "/* ── Permanent-QR join flow: ?join={templateId} resolves to whichever room is\n" +
    "   currently running from that template. No room yet → waiting page that polls\n" +
    "   and auto-joins the moment the host starts the service. The template id is\n" +
    "   remembered so a mid-service room restart heals on the next resolve. ── */\n" +
    "(function(){\n" +
    "  var _js=new URLSearchParams(window.location.search);\n" +
    "  var joinTpl=_js.get('join');\n" +
    "  if(!joinTpl)return;\n" +
    "  sessionStorage.setItem('joinTpl',joinTpl);\n" +
    "  var ov=document.createElement('div');\n" +
    "  ov.id='joinWait';\n" +
    "  ov.style.cssText='position:fixed;inset:0;background:#000;z-index:2000;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:32px;text-align:center';\n" +
    "  ov.innerHTML='<div style=\"font-size:42px;margin-bottom:18px\">\\uD83D\\uDD4A\\uFE0F</div>'+\n" +
    "    '<div id=\"joinWaitMsg\" style=\"color:#ccc;font-size:17px;line-height:1.5;max-width:340px\">'+t('joinWaiting')+'</div>'+\n" +
    "    '<div id=\"joinWaitDots\" style=\"color:#7c9cf7;font-size:26px;margin-top:14px;min-height:30px\">\\u00B7</div>';\n" +
    "  document.body.appendChild(ov);\n" +
    "  var dots=0;\n" +
    "  setInterval(function(){dots=(dots%4)+1;var d=document.getElementById('joinWaitDots');if(d)d.textContent='\\u00B7\\u00B7\\u00B7\\u00B7'.slice(0,dots)},600);\n" +
    "  function poll(){\n" +
    "    fetch('/api/templates/'+encodeURIComponent(joinTpl)+'/active-room').then(function(r){return r.json()}).then(function(res){\n" +
    "      if(res.error){document.getElementById('joinWaitMsg').textContent=t('joinUnknown');return}\n" +
    "      if(res.active){location.replace('/index.html?room='+encodeURIComponent(res.roomId));return}\n" +
    "      setTimeout(poll,5000);\n" +
    "    }).catch(function(){setTimeout(poll,7000)});\n" +
    "  }\n" +
    "  poll();\n" +
    "})();\n\n" +
    "/* ── Keep screen on (Wake Lock)",
    'join waiting flow');

// 8. Host panel: Admin button after Clear Captions.
js = mustReplace(js,
    "'<button id=\"hcClear\" style=\"width:100%;padding:10px;border:none;border-radius:8px;background:#555;color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:8px\">✕ '+t('clearCaptions')+'</button>';",
    "'<button id=\"hcClear\" style=\"width:100%;padding:10px;border:none;border-radius:8px;background:#555;color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:8px\">✕ '+t('clearCaptions')+'</button>'+\n" +
    "    '<button id=\"hcAdmin\" style=\"width:100%;padding:10px;border:1px solid #7c9cf7;border-radius:8px;background:transparent;color:#7c9cf7;font-size:14px;cursor:pointer;margin-bottom:8px\">⚙ '+t('hostAdmin')+'</button>';",
    'host panel admin button');
js = mustReplace(js,
    "  document.body.appendChild(panel);\n\n  /* Pre-select pipeline values from room state */",
    "  document.body.appendChild(panel);\n\n" +
    "  var _hcAdm=document.getElementById('hcAdmin');\n" +
    "  if(_hcAdm)_hcAdm.addEventListener('click',function(){window.open('/admin.html','_blank')});\n\n" +
    "  /* Pre-select pipeline values from room state */",
    'host admin handler');

// 9. New T strings.
js = mustReplace(js,
    "    dictCopy:'Copy',dictCopied:'Copied \\u2713',dictDone:'Done',dictOutLang:'Output language',",
    "    dictCopy:'Copy',dictCopied:'Copied \\u2713',dictDone:'Done',dictOutLang:'Output language',\n" +
    "    joinWaiting:'The service hasn\\u2019t started yet \\u2014 this page will join automatically as soon as it begins.',\n" +
    "    joinUnknown:'This QR code isn\\u2019t valid for this server any more \\u2014 please ask for a new one.',\n" +
    "    hostAdmin:'Admin',",
    'T strings');

fs.writeFileSync('EveryTongue.Core/wwwroot/js/app.js', js);

// ── index.html ──
let html = fs.readFileSync('EveryTongue.Core/wwwroot/index.html', 'utf8');

// btnAdmin out of the toolbar.
html = mustReplace(html,
    /<button id="btnAdmin"[^\n]*<\/button>\s*\n/.exec(html)?.[0] ?? '@@nomatch@@',
    '', 'btnAdmin button');

// adminPanel block out (comment + div).
html = cutBetween(html,
    '<!-- Admin panel: SERVER-scoped only.',
    '<div id="panel">',
    '<div id="panel">',
    'adminPanel div');

// lpAdmin form → plain link to /admin.html.
html = mustReplace(html,
    '    <div id="lpAdmin">\n' +
    '      <div class="lp-admin-toggle" id="lpAdminToggle" onclick="toggleAdminLogin()">Administrator</div>\n' +
    '      <div id="lpAdminForm" style="display:none">\n' +
    '        <input id="lpAdminPin" type="password" inputmode="numeric" pattern="[0-9]*" maxlength="8" autocomplete="off">\n' +
    '        <button id="lpAdminGo" onclick="verifyAdminPin()">OK</button>\n' +
    '        <div id="lpAdminMsg"></div>\n' +
    '      </div>\n' +
    '    </div>',
    '    <div id="lpAdmin">\n' +
    '      <div class="lp-admin-toggle" id="lpAdminToggle" onclick="openAdminPage()">Administrator</div>\n' +
    '    </div>',
    'lpAdmin form');

fs.writeFileSync('EveryTongue.Core/wwwroot/index.html', html);
console.log('done');
