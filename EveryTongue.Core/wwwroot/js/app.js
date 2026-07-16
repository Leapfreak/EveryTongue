/* Every Tongue - Subtitle Client
   ES5 compatible (except async/await for Wake Lock API) */

function LOG(msg){try{console.log('[TT] '+msg)}catch(e){}}

/* ── Per-window session storage ──
   Session-specific settings use sessionStorage (per-tab) so two browser
   windows don't interfere.  On first load, seed from localStorage defaults.
   Writing also updates localStorage so new tabs get the latest defaults. */
var _sessionKeys=['voice','transLang','langChosen','speak','rate','serverTts','displayName'];
(function(){
  if(!sessionStorage.getItem('_sessionSeeded')){
    for(var i=0;i<_sessionKeys.length;i++){
      var k=_sessionKeys[i];
      var v=localStorage.getItem(k);
      if(v!==null)sessionStorage.setItem(k,v);
    }
    sessionStorage.setItem('_sessionSeeded','1');
  }
})();
function ss(key){return sessionStorage.getItem(key)}
function ssSet(key,val){sessionStorage.setItem(key,val);localStorage.setItem(key,val)}
function ssRemove(key){sessionStorage.removeItem(key);localStorage.removeItem(key)}

/* ── i18n ── */
var T={connecting:'Connecting...',connected:'Connected',disconnected:'Disconnected - reconnecting...',
    wakeTitle:'Keep Screen On',wakeDesc:'A secure connection is needed (one-time setup):',
    stepTap:'Tap the button below',stepWarn:'You will see a warning page \u2014 this is normal',
    stepAdv:'Tap "Advanced"',stepProceed:'Tap "Proceed to {0}"',
    stepAccept:'Tap "Accept the Risk and Continue"',
    stepDetails:'Tap "Show Details"',stepVisit:'Tap "visit this website"',
    stepRetry:'Tap the screen wake button again',
    openSecure:'Open Secure Page',cancel:'Cancel',
    dictCopy:'Copy',dictCopied:'Copied \u2713',dictDone:'Done',dictOutLang:'Output language',
    joinWaiting:'The service hasn\u2019t started yet \u2014 this page will join automatically as soon as it begins.',
    joinUnknown:'This QR code isn\u2019t valid for this server any more \u2014 please ask for a new one.',
    hostAdmin:'Admin',
    rsPreparing:'Preparing speech engine...',vmShared:'(shared)',
    hostSpeakerLang:'Speaker Language',hostApply:'Apply',pipeReset:'Reset Pipeline',autoDetect:'Auto Detect',
    rsWaitMicHost:'Waiting for microphone — open Host Controls and tap Broadcast Mic',
    rsWaitMic:'Waiting for the host to start the microphone…',
    setTitle:'Server Settings',setBootstrap:'First-time setup: choose your engines, paste your API keys, and set an admin PIN to secure this server.',
    setSttEngine:'Speech engine',setTransEngine:'Translation engine',
    setKeysStt:'Speech API keys',setKeysTrans:'Translation API keys',
    setKeySet:'•••• configured — leave blank to keep',setKeyEmpty:'not set — paste key',
    setPinLabel:'Admin PIN',setPinNew:'choose a PIN',setPinRequired:'Set an admin PIN to secure the server first',
    setCreatorLabel:'Host tools code',setCreatorHint:'Volunteers enter this in the lobby to create rooms. Empty = anyone can create. Enter "-" to clear.',setCreatorEmpty:'not set — room creation is open',
    setSave:'Save',setSaved:'Saved ✓',setViewLog:'View server log',setBadPin:'Not authorized',
    setRawBtn:'Advanced: edit raw config',setRawHint:'The full server configuration (config.json). Engine, API key, PIN and host-code changes apply immediately; other changes apply after a restart.',
    setRawSave:'Save raw config',setRawInvalid:'Invalid JSON: ',setRawSaved:'Config saved ✓',
    setRawRestart:'Config saved ✓ — some changes apply after a restart',setRawLoadFail:'Failed to load config',
    setRawPinCleared:'Warning: admin PIN cleared — settings are now open',
    setBiblesBtn:'Bibles (download)',
    setBiblesHint:'Freely-redistributable Bibles from eBible.org. Copyrighted translations must be copied into the Bibles folder manually.',
    setBiblesSearch:'Search by language or name...',setBiblesInstalled:'installed',
    setBiblesDownload:'Download',setBiblesRetry:'Retry',
    setBiblesDownloading:'downloading',setBiblesConverting:'converting',setBiblesVerifying:'verifying',
    setBiblesTypeToSearch:'Type at least 2 letters to search the catalog. Installed Bibles are listed above.',
    setBiblesMore:'+{0} more — refine your search',setBiblesNone:'No matches.',
    setTplsBtn:'Conference templates',setTplsNew:'New template',setTplsNone:'No templates yet.',
    setTplsEdit:'Edit',setTplsDelete:'Delete',setTplsDeleteConfirm:'Delete this template?',
    setTplsName:'Name',setTplsHostCode:'Hosting code (volunteers enter this to start the room)',
    setTplsSourceLang:'Speaker language',setTplsAudio:'Microphone',
    setTplsAudioWeb:'Web mic (browser broadcast)',setTplsAudioWebRaw:'Web mic, raw (PA/line feed)',setTplsAudioLocal:'Local device (server machine)',
    setTplsVisibility:'Room visibility',setTplsPublic:'Public (listed in lobby)',setTplsPrivate:'Private (QR/link only)',
    setTplsOffered:'Offered languages',
    setTplsOfferedHint:'Comma-separated FLORES codes, e.g. spa_Latn, eng_Latn, cat_Latn. Empty = listeners can pick any language.',
    setTplsServerDefault:'(server default)',setTplsNameReq:'Name and hosting code are required',
    bcStart:'Broadcast Mic',bcStop:'LIVE — tap to stop',bcStarting:'Starting mic…',
    bcTakenOver:'Another device took over the microphone',
    bcRejected:'Microphone broadcast not allowed (host only)',
    bcUnsupported:'This browser cannot broadcast (needs HTTPS + a recent browser)',
    bcMicFailed:'Could not access the microphone — check permissions',
    micUnavailable:'Microphone not available',micDenied:'Mic access denied',recording:'Recording...',
    hostControls:'Host Controls',endRoom:'End Room',clearCaptions:'Clear Captions',
    addGuest:'Add Guest',guestName:'Name',guestAdd:'Add',youLabel:'You',typeMessage:'Type a message...',
    pipeResetting:'Resetting...',pipeResetOk:'Pipeline reset OK',netError:'Network error',
    applying:'Applying...',applied:'Applied ({0} params)',failed:'Failed',errorLabel:'Error',
    bibleNoBibles:'No Bibles installed for this language.',bibleNoBiblesHint:'Use the Download Manager to add Bibles.',
    bibleLoadFail:'Failed to load',bibleTransFail:'Failed to load translations',bibleNoVerses:'No verses',
    bibleBadRef:'Could not parse reference',bibleRefError:'Error looking up reference',
    bibleSearching:'Searching...',bibleSearchFail:'Search failed',bibleResults:'{0} results',
    rsReady:'Ready',rsTransWarming:'Translation warming up...',resetFailed:'Reset failed',
    roomEnded:'Room has ended',roomKicked:'You have been removed',
    yourName:'Your Name',enterYourName:'Enter your name',guestLabel:'Guest',
    sending:'Sending...',cmdSent:' command sent',cmdFail:'Failed to send command',
    liveRun:'Live: RUNNING',stopped:'Status: STOPPED',
    noServer:'Unable to reach server',checking:'Checking...',
    dfltVoice:'Default',title:'Every Tongue',
    bold:'Bold',font:'Font',style:'Style',voice:'Voice',speed:'Speed',color:'Text Color',
    slow:'Slow',normal:'Normal',fast:'Fast',vfast:'Very Fast',
    start:'Start',stop:'Stop',restart:'Restart',clear:'Clear',
    saveTranscript:'Save Transcript',transLang:'Translation',remote:'Remote Control',settings:'Settings',readAloud:'Read aloud',keepScreen:'Keep screen on',scrollDir:'Scroll Direction',scrollUp:'Bottom-up (newest at bottom)',scrollDown:'Top-down (newest at top)',tags:'Tags',tagOff:'Off',tagLang:'Language',tagTime:'Time',tagBoth:'Language + Time',bible:'Bible',bibleOT:'Old Testament',bibleNT:'New Testament',bibleSearch:'Search',bibleNoResults:'No results found',bibleSelectTrans:'Select a translation',cloudVoice:'Every Tongue Voices',ttsBehind:'{0} behind \u2014 tap to skip',readAll:'Read All',readVerse:'Read',bibleTranslate:'Translate',bibleOriginal:'Original',hostSpeaker:'Speaker',hostSpeakerDefault:'(template default)',hostMode:'Connectivity',hostModeOnline:'Online',hostModeOffline:'Offline',chooseLang:'Choose your language',lpPopular:'Popular',lpAll:'All Languages',searchLangs:'Search languages...',noTranslation:'No translation',browseAll:'Browse All',adminLabel:'Administrator',adminPin:'PIN',adminBad:'Invalid PIN',adminOk:'Admin access granted'};
/* Detect browser language and fetch matching server-side locale */
var detectedBrowserLang='';
(function(){
  try{
    var nav=navigator.language||navigator.userLanguage||'';
    /* "es-MX" -> "es", "zh-Hans" -> "zh", "pt-BR" -> "pt" */
    detectedBrowserLang=nav.split('-')[0].toLowerCase();
    LOG('Browser language detected: '+nav+' -> '+detectedBrowserLang);
    var url='/api/locale';
    if(detectedBrowserLang)url+='?lang='+encodeURIComponent(detectedBrowserLang);
    var xhr=new XMLHttpRequest();
    xhr.open('GET',url,true);
    xhr.onload=function(){
      if(xhr.status===200){
        try{
          var data=JSON.parse(xhr.responseText);
          for(var k in data){if(data.hasOwnProperty(k))T[k]=data[k]}
        }catch(e){LOG('locale parse error: '+e)}
      }
    };
    xhr.send();
  }catch(e){LOG('locale fetch error: '+e)}
})();
function t(k){return T[k]||k}
/* Server sends the literal "Guest" as the unnamed-client sentinel — swap it
   for the localized label at render. Empty stays empty (means "no speaker"). */
function dispName(n){return n==='Guest'?t('guestLabel'):(n||'')}

/* Rebuild a <select> with the active STT engine's languages from
   /api/stt-languages (engine-declared list, or the whisper set). Native names
   shown — locale-independent. Keeps the current selection; optionally keeps a
   leading "auto" option. Hardcoded fallback options survive if the fetch fails. */
function populateSttLangs(sel,withAuto){
  if(!sel)return;
  var xhr=new XMLHttpRequest();
  xhr.open('GET','/api/stt-languages',true);
  xhr.onload=function(){
    if(xhr.status!==200)return;
    try{
      var langs=JSON.parse(xhr.responseText);
      if(!langs||!langs.length)return;
      var current=sel.value;
      sel.innerHTML='';
      if(withAuto){var ao=document.createElement('option');ao.value='auto';ao.textContent=t('autoDetect');sel.appendChild(ao)}
      for(var i=0;i<langs.length;i++){
        var o=document.createElement('option');
        o.value=langs[i].code;
        o.textContent=langs[i].native&&langs[i].native!==langs[i].name?langs[i].native+' ('+langs[i].name+')':langs[i].name;
        sel.appendChild(o);
      }
      if(current){sel.value=current;if(sel.value!==current&&sel.options.length)sel.selectedIndex=0}
    }catch(e){LOG('stt-languages parse error: '+e)}
  };
  xhr.send();
}

/* ── Language data: [floresCode, nativeName, englishName, bcp47Prefix] ── */
/* Loaded from /api/languages; minimal fallback until fetch completes */
var LANGS=[['eng_Latn','English','English','en']];
var _langsLoaded=false;

/* Popular languages shown at top of picker */
var POPULAR_LANGS=['eng_Latn','spa_Latn','fra_Latn','por_Latn','deu_Latn','cat_Latn','zho_Hans','arb_Arab','rus_Cyrl','hin_Deva','kor_Hang','jpn_Jpan','ita_Latn'];

/* Fetch full language list from server */
(function(){
  try{
    var xhr=new XMLHttpRequest();
    xhr.open('GET','/api/languages',true);
    xhr.onload=function(){
      if(xhr.status===200){
        try{
          var data=JSON.parse(xhr.responseText);
          if(data&&data.length>0){
            LANGS=data;_langsLoaded=true;
            /* Re-render picker if it's already open */
            var picker=document.getElementById('langPicker');
            if(picker&&picker.classList.contains('open')){renderLangList('');detectAndSuggest()}
            /* Re-populate transLangSelect (+ the dictation output dropdown if open) */
            populateTransLangSelect();
            populateDictOutLang();
          }
        }catch(e){LOG('languages parse error: '+e)}
      }
    };
    xhr.send();
  }catch(e){LOG('languages fetch error: '+e)}
})();

/* ── Language Picker ── */
function showLangPicker(){
  var picker=document.getElementById('langPicker');
  picker.classList.add('open');
  document.getElementById('lpSearch').value='';
  renderLangList('');
  detectAndSuggest();
  diagLog('picker_shown items='+document.getElementById('lpList').children.length);
}
function hideLangPicker(){
  document.getElementById('langPicker').classList.remove('open');
  diagLog('picker_hidden');
  var dock=document.getElementById('roomControlsDock');
  if(dock){dock.style.display='flex';setTimeout(adjustDockPadding,50)}
}
var voiceManuallySet=!!ss('voice');
function pickLang(code){
  diagLog('pickLang code='+code);
  myTransLang=code;
  ssSet('transLang',code);
  ssSet('langChosen','true');
  hideLangPicker();
  setTransLang(code);
  /* sync dropdown */
  var sel=document.getElementById('transLangSelect');
  for(var i=0;i<sel.options.length;i++){if(sel.options[i].value===code){sel.selectedIndex=i;break}}
  /* Auto-select a matching voice for the new language, unless user manually chose one */
  if(!voiceManuallySet&&!serverTtsActive){autoSelectVoiceForLang(code)}
}
function autoSelectVoiceForLang(floresCode){
  var bcp=floresToBcp47Lookup(floresCode);
  if(!bcp){LOG('autoSelectVoice: no BCP47 mapping for '+floresCode);return}
  var voices=synth.getVoices();
  for(var i=0;i<voices.length;i++){
    if(voices[i].lang&&voices[i].lang.toLowerCase().indexOf(bcp)===0){
      LOG('autoSelectVoice: picked '+voices[i].name+' for '+bcp);
      selectedVoice=voices[i].name;
      ssSet('voice',voices[i].name);
      voiceSelect.value=voices[i].name;
      voiceManuallySet=false; /* auto-set, not manual */
      return;
    }
  }
  LOG('autoSelectVoice: no voice found for '+bcp+', keeping default');
  selectedVoice='';
  ssSet('voice','');
  voiceSelect.value='';
}
function detectAndSuggest(){
  var bl=(navigator.language||'en').toLowerCase().split('-')[0];
  var detected=null;
  for(var i=0;i<LANGS.length;i++){if(LANGS[i][3]===bl){detected=LANGS[i];break}}
  var el=document.getElementById('lpDetected');
  el.innerHTML='';
  if(detected){
    var btn=document.createElement('button');btn.className='lp-detected-btn';
    btn.textContent=detected[1];
    btn.onclick=function(){pickLang(detected[0])};
    el.appendChild(btn);
    var sub=document.createElement('div');sub.className='lp-detected-sub';
    sub.textContent=detected[2];
    el.appendChild(sub);
  }
}
function renderLangList(query){
  var list=document.getElementById('lpList');
  list.innerHTML='';
  var q=query.toLowerCase();
  if(!q){
    var popLabel=document.createElement('div');popLabel.className='lp-section-label';
    popLabel.textContent=t('lpPopular');list.appendChild(popLabel);
    for(var p=0;p<POPULAR_LANGS.length;p++){
      if(!isLangOffered(POPULAR_LANGS[p]))continue;
      for(var j=0;j<LANGS.length;j++){
        if(LANGS[j][0]===POPULAR_LANGS[p]){list.appendChild(createLangItem(LANGS[j]));break}
      }
    }
    var allLabel=document.createElement('div');allLabel.className='lp-section-label';
    allLabel.textContent=t('lpAll');list.appendChild(allLabel);
  }
  for(var i=0;i<LANGS.length;i++){
    var l=LANGS[i];
    if(!isLangOffered(l[0]))continue;
    if(q&&l[1].toLowerCase().indexOf(q)===-1&&l[2].toLowerCase().indexOf(q)===-1&&l[0].toLowerCase().indexOf(q)===-1)continue;
    list.appendChild(createLangItem(l));
  }
  if(q&&list.children.length===0){
    var noRes=document.createElement('div');noRes.style.cssText='color:#888;text-align:center;padding:40px';
    noRes.textContent=t('bibleNoResults');list.appendChild(noRes);
  }
}
function createLangItem(lang){
  var div=document.createElement('div');div.className='lp-lang-item';
  var code=lang[0];
  div.onclick=function(){pickLang(code)};
  var native=document.createElement('span');native.className='lp-lang-native';
  native.textContent=lang[1];
  var eng=document.createElement('span');eng.className='lp-lang-eng';
  eng.textContent=lang[2];
  div.appendChild(native);div.appendChild(eng);
  return div;
}
document.getElementById('lpSearch').oninput=function(){renderLangList(this.value)};

function goHome(){
  LOG('goHome');
  /* Three-tier IA: volunteers (device holds the host-tools code) go back to
     the lobby; for guests "home" is re-choosing their language — the entry
     picker is a right, and there is no guest landing page to go back to. */
  if(localStorage.getItem('creatorCode')){ssRemove('langChosen');window.location.href=window.location.origin+'/lobby.html';return}
  showLangPicker();
}
/* Where to land when the room is gone (ended/kicked): permanent-QR guests
   re-resolve their template (a restarted service heals seamlessly),
   volunteers return to the lobby, other guests get the language picker. */
function afterRoomGone(){
  var tpl=sessionStorage.getItem('joinTpl');
  if(tpl){location.replace('/index.html?join='+encodeURIComponent(tpl));return}
  if(localStorage.getItem('creatorCode')){location.href='/lobby.html';return}
  showLangPicker();
}

/* ── Room QR sharing ── */
var _roomQrVisible=false;
function initRoomShareButton(){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  var roomId=roomMatch[1];
  var btn=document.getElementById('btnShareRoom');
  if(btn)btn.style.display='inline-block';
  /* Pre-load room info and QR */
  var xhr=new XMLHttpRequest();
  xhr.open('GET','/api/rooms/'+encodeURIComponent(roomId),true);
  xhr.onload=function(){
    if(xhr.status===200){
      try{
        var room=JSON.parse(xhr.responseText);
        document.getElementById('roomQrTitle').textContent=room.name||'Room';
      }catch(e){}
    }
  };
  xhr.send();
  /* Share link must be PHONE-reachable: prefer the server-reported public host
     (never "localhost" — that would point each phone at itself). */
  var base=serverPublicHost?location.protocol+'//'+serverPublicHost:location.origin;
  var joinUrl=base+'/index.html?room='+encodeURIComponent(roomId);
  document.getElementById('roomQrImg').src='/api/rooms/'+encodeURIComponent(roomId)+'/qr';
  var qrUrlEl=document.getElementById('roomQrUrl');qrUrlEl.innerHTML='<a href="'+joinUrl+'" style="color:#7c9cf7;text-decoration:underline">'+joinUrl+'</a>';
}
function toggleRoomQr(){
  var overlay=document.getElementById('roomQrOverlay');
  if(_roomQrVisible){_roomQrVisible=false;overlay.style.display='none';return}
  closeAllPanels();
  _roomQrVisible=true;
  overlay.style.display='flex';
}
initRoomShareButton();

/* Populate transLangSelect dropdown dynamically from LANGS */
function populateTransLangSelect(){
  var sel=document.getElementById('transLangSelect');
  sel.innerHTML='';
  for(var i=0;i<LANGS.length;i++){
    if(!isLangOffered(LANGS[i][0]))continue;
    var opt=document.createElement('option');opt.value=LANGS[i][0];
    opt.textContent=LANGS[i][1]+' ('+LANGS[i][2]+')';
    sel.appendChild(opt);
  }
  var saved=ss('transLang')||'';
  for(var j=0;j<sel.options.length;j++){if(sel.options[j].value===saved){sel.selectedIndex=j;break}}
}
populateTransLangSelect();

/* ── Caption-delivery self-check ──
   Counts commit messages actually RECEIVED on this device, shown as a small
   badge in a room and heartbeated to the server log (clientLog). This is the
   instrument for "the host saw no subtitles": compare per-client received-count
   against the server's MessagesSent — a gap localizes the fault to delivery vs
   render, without reconstructing it from logs afterwards. */
var capCount=0,capRendered=0,capLastAt=0,capBadge=null;
/* Diagnostics mode: ?diag=1 on the URL (or localStorage etDiag=1) turns on the
   caption badge, the 15s received/rendered+visual-state heartbeats and picker
   lifecycle logs — the instruments that found the v2.8.1 invisible-UI bug.
   OFF by default: a healthy service logs nothing from the client. Anomalies
   (render errors, skipped commits, WS message errors) ALWAYS report. */
var capDiag=/[?&]diag=1/.test(location.search)||localStorage.getItem('etDiag')==='1';
function inRoomView(){return location.search.indexOf('room=')!==-1}
/* Route a diagnostic line into the SERVER log (SLOG pattern — never asks the
   user to open browser dev tools). Pre-connection lines queue and flush on
   WS open so page-load events (picker shown) aren't lost. */
var _slogQueue=[];
function slogDiag(msg){
  try{
    if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'clientLog',msg:msg}))}
    else{_slogQueue.push(msg);if(_slogQueue.length>20)_slogQueue.shift()}
  }catch(e){}
  LOG(msg);
}
function slogFlush(){
  try{
    while(_slogQueue.length&&wsRef&&wsRef.readyState===1){
      wsRef.send(JSON.stringify({type:'clientLog',msg:'(queued) '+_slogQueue.shift()}));
    }
  }catch(e){}
}
/* Chatty diagnostics: server log only in diag mode, console otherwise. */
function diagLog(msg){if(capDiag)slogDiag(msg);else LOG(msg)}
function ensureCapBadge(){
  if(capBadge||!inRoomView()||!capDiag)return;
  capBadge=document.createElement('div');
  capBadge.id='capBadge';
  capBadge.title='Captions received on this device (tap to hide)';
  capBadge.style.cssText='position:fixed;bottom:8px;right:8px;z-index:150;font-size:11px;padding:4px 9px;border-radius:10px;background:rgba(0,0,0,0.55);font-family:monospace;cursor:pointer;user-select:none';
  capBadge.addEventListener('click',function(){capBadge.style.display='none'});
  document.body.appendChild(capBadge);
  updateCapBadge();
}
function updateCapBadge(){
  if(!capBadge)return;
  var age=capLastAt?Math.round((Date.now()-capLastAt)/1000):-1;
  var col,txt;
  if(!wsRef||wsRef.readyState!==1){col='#f55';txt='● offline'}
  else if(capCount===0){col='#fa4';txt='● 0 captions'}
  else if(age>=0&&age<=20){col='#5c5';txt='● '+capCount}
  else{col='#fa4';txt='● '+capCount+' · '+age+'s'}
  capBadge.style.color=col;capBadge.textContent=txt;
}
/* Where are the drawn captions PHYSICALLY, and what owns the screen center?
   rendered=N with a blank screen means the fault is visual — this answers
   overlay-vs-offscreen-vs-CSS without needing eyes on the device. */
function visualState(){
  try{
    var lines=document.getElementById('lines');
    var cont=document.getElementById('container');
    var n=lines?Math.max(0,lines.children.length-1):-1; /* minus spacer */
    var out='lines='+n;
    if(lines&&lines.children.length>1){
      var last=lines.children[lines.children.length-1];
      var r=last.getBoundingClientRect();
      var off=(r.top>=window.innerHeight||r.bottom<=0||r.width===0);
      var st=getComputedStyle(last);
      out+=' last@'+Math.round(r.top)+'px/'+window.innerHeight+'px w'+Math.round(r.width)+'xh'+Math.round(r.height)+' '+(off?'OFFSCREEN':'onscreen')+
           ' color='+st.color+' size='+st.fontSize;
      if(off){
        /* Walk up to find WHICH ancestor collapsed the line */
        var anc=last,chain='';
        while(anc&&anc.tagName!=='HTML'&&chain.length<200){
          var ar=anc.getBoundingClientRect();
          chain+=(anc.id||anc.tagName)+'['+Math.round(ar.width)+'x'+Math.round(ar.height)+' '+getComputedStyle(anc).display+'] ';
          anc=anc.parentElement;
        }
        out+=' chain='+chain;
      }
    }
    var spacer=document.getElementById('spacer');
    if(spacer)out+=' spacerH='+Math.round(spacer.getBoundingClientRect().height);
    if(cont)out+=' container='+getComputedStyle(cont).display;
    var picker=document.getElementById('langPicker');
    out+=' picker='+(picker&&picker.classList.contains('open')?'OPEN':'closed');
    out+=' hostPanel='+(document.getElementById('hostPanel')?'open':'closed');
    var c=document.elementFromPoint(Math.floor(window.innerWidth/2),Math.floor(window.innerHeight/2));
    out+=' center='+(c?(c.id||String(c.className).split(' ')[0]||c.tagName):'?');
    return out;
  }catch(e){return 'visualState_err='+e}
}
/* Refresh the badge staleness + heartbeat the count into the server log. */
setInterval(function(){
  updateCapBadge();
  if(capDiag&&wsRef&&wsRef.readyState===1&&inRoomView()){
    try{wsRef.send(JSON.stringify({type:'clientLog',msg:'captions_received='+capCount+' rendered='+capRendered+' lang='+(myTransLang||'source')+' bc='+(bcActive?'1':'0')+' | '+visualState()}))}catch(e){}
  }
},15000);

/* ── DOM references ── */
var fontSize=28;
var currentEl=null;
var speakEnabled=false;
var selectedVoice='';
var speechRate=1;
var myTransLang=ss('transLang')||'';
var synth=window.speechSynthesis;
var lines=document.getElementById('lines');
var container=document.getElementById('container');
var statusBar=document.getElementById('status');
var statusEl=document.getElementById('statusText')||statusBar;
var panel=document.getElementById('panel');
var btnSpeak=document.getElementById('btnSpeak');
var voiceSelect=document.getElementById('voiceSelect');
var rateSelect=document.getElementById('rateSelect');

/* ── Restore saved preferences ── */
if(ss('voice'))selectedVoice=ss('voice');
if(ss('rate')){speechRate=parseFloat(ss('rate'));rateSelect.value=ss('rate')}
if(ss('speak')==='true'){speakEnabled=true;btnSpeak.classList.add('active');btnSpeak.innerHTML='&#128266; '+t('readAloud')}
(function(){var dn=ss('displayName');if(dn){var inp=document.getElementById('displayNameInput');if(inp)inp.value=dn}})()

/* ── Voice synthesis ── */
function populateVoices(){
  var voices=synth.getVoices();
  voiceSelect.innerHTML='';
  var cloudOpt=document.createElement('option');cloudOpt.value='__cloud__';cloudOpt.textContent=t('cloudVoice');voiceSelect.appendChild(cloudOpt);
  for(var i=0;i<voices.length;i++){
    var v=voices[i];
    var opt=document.createElement('option');opt.value=v.name;
    opt.textContent=v.name+(v.lang?' ('+v.lang+')':'');
    voiceSelect.appendChild(opt);
  }
  /* Restore selection */
  if(serverTtsActive){voiceSelect.value='__cloud__'}
  else if(selectedVoice){voiceSelect.value=selectedVoice}
}
function onVoiceChange(val){
  LOG('onVoiceChange: '+val);
  if(val==='__cloud__'){
    serverTtsActive=true;
    ssSet('serverTts','true');
    selectedVoice='';
    ssSet('voice','');
    voiceManuallySet=true; /* user explicitly chose cloud */
  }else{
    serverTtsActive=false;
    ssSet('serverTts','false');
    selectedVoice=val;
    ssSet('voice',val);
    voiceManuallySet=!!val; /* manual if they picked a specific voice, not 'Default' */
    clearTtsQueue();
  }
  sendTtsState();
}
populateVoices();
if(synth.onvoiceschanged!==undefined)synth.onvoiceschanged=populateVoices;

/* ── Panel toggle ── */
function closeAllPanels(){panel.style.display='none';var hp=document.getElementById('hostPanel');if(hp)hp.remove();biblePanel.classList.remove('open');var qr=document.getElementById('roomQrOverlay');if(qr){qr.style.display='none';_roomQrVisible=false}}
function togglePanel(){if(panel.style.display==='block'){panel.style.display='none'}else{closeAllPanels();panel.style.display='block'}}
document.addEventListener('click',function(e){if(!document.body.contains(e.target))return;var hp=document.getElementById('hostPanel');var toolbar=document.getElementById('toolbar');var qr=document.getElementById('roomQrOverlay');if(!panel.contains(e.target)&&(!hp||!hp.contains(e.target))&&!toolbar.contains(e.target)&&!biblePanel.contains(e.target)&&(!qr||!qr.contains(e.target))){closeAllPanels()}})

/* Tell the server whether this client will actually PLAY pushed server TTS —
   it skips synthesising languages nobody is listening to. */
function sendTtsState(){
  try{
    if(wsRef&&wsRef.readyState===1){
      wsRef.send(JSON.stringify({type:'ttsState',enabled:useServerTts()}));
    }
  }catch(e){}
}

function toggleSpeak(){
  speakEnabled=!speakEnabled;
  ssSet('speak',speakEnabled);
  LOG('toggleSpeak → '+speakEnabled);
  if(speakEnabled){btnSpeak.classList.add('active');btnSpeak.innerHTML='&#128266; '+t('readAloud')}
  else{btnSpeak.classList.remove('active');btnSpeak.innerHTML='&#128264; '+t('readAloud');synth.cancel();clearTtsQueue()}
  sendTtsState();
}

function speak(text,langHint){
  if(!speakEnabled||!synth||!text){LOG('speak SKIP: enabled='+speakEnabled+' synth='+!!synth+' text='+(text?text.substring(0,30):'(empty)'));return}
  var inRoom=location.search.indexOf('room=')!==-1;
  /* Server TTS (Every Tongue Voices): send requestTts to server */
  if(serverTtsActive){
    if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'requestTts',text:text,language:langHint||''}))}
    return;
  }
  LOG('speak: "'+text.substring(0,50)+'"');
  var utter=new SpeechSynthesisUtterance(text);
  utter.rate=speechRate;
  if(selectedVoice){var voices=synth.getVoices();for(var i=0;i<voices.length;i++){if(voices[i].name===selectedVoice){utter.voice=voices[i];break}}}
  synth.speak(utter);
}

/* ── Server TTS (hybrid playback) ── */
var serverTtsActive=ss('serverTts')==='true';
var ttsAudio=null;
var ttsQueue=[];
var ttsPlaying=false;
var ttsSkipIndicator=null;

/* FLORES language code to BCP47 prefix — built dynamically from LANGS */
function floresToBcp47Lookup(flores){
  for(var i=0;i<LANGS.length;i++){if(LANGS[i][0]===flores)return LANGS[i][3]}
  return '';
}

function hasBrowserVoiceForLang(){
  var transLang=myTransLang||'';
  if(!transLang)return true; /* original language — browser usually has it */
  var bcp=floresToBcp47Lookup(transLang);
  if(!bcp)return false;
  var voices=synth.getVoices();
  for(var i=0;i<voices.length;i++){
    if(voices[i].lang&&voices[i].lang.toLowerCase().indexOf(bcp)===0)return true;
  }
  return false;
}

function useServerTts(){
  if(!speakEnabled)return false;
  return serverTtsActive||!hasBrowserVoiceForLang();
}


function handleTtsMessage(msg){
  LOG('handleTtsMessage: url='+msg.url+' id='+msg.id+' speakEnabled='+speakEnabled+' useServer='+useServerTts());
  /* id === -1 means this is a requestTts response (e.g. tap-to-read) — always play */
  var isOnDemand=(msg.id===-1);
  if(!isOnDemand){
    if(!speakEnabled)return;
    if(!useServerTts())return;
  }
  synth.cancel(); /* stop any browser TTS */
  enqueueTts(msg.url);
}

function enqueueTts(url){
  LOG('enqueueTts: '+url+' queueLen='+ttsQueue.length+' playing='+ttsPlaying);
  ttsQueue.push(url);
  updateTtsSkipIndicator();
  if(!ttsPlaying)playNextTts();
}

function playNextTts(){
  if(ttsQueue.length===0){LOG('playNextTts: queue empty, done');ttsPlaying=false;updateTtsSkipIndicator();bibleTtsActive=false;return}
  ttsPlaying=true;
  var url=ttsQueue.shift();
  LOG('playNextTts: playing '+url+' remaining='+ttsQueue.length);
  updateTtsSkipIndicator();
  if(!ttsAudio){ttsAudio=new Audio()}
  ttsAudio.src=url;
  ttsAudio.playbackRate=speechRate;
  ttsAudio.onended=function(){LOG('ttsAudio ended');playNextTts()};
  ttsAudio.onerror=function(e){LOG('ttsAudio error: '+e.type);playNextTts()};
  ttsAudio.play().catch(function(err){LOG('ttsAudio play() rejected: '+err);playNextTts()});
}

function clearTtsQueue(){
  ttsQueue=[];
  ttsPlaying=false;
  if(ttsAudio){ttsAudio.pause();ttsAudio.src=''}
  updateTtsSkipIndicator();
}

function updateTtsSkipIndicator(){
  if(ttsQueue.length>=2){
    if(!ttsSkipIndicator){
      ttsSkipIndicator=document.createElement('div');
      ttsSkipIndicator.id='ttsSkip';
      ttsSkipIndicator.onclick=function(){clearTtsQueue()};
      document.body.appendChild(ttsSkipIndicator);
    }
    ttsSkipIndicator.textContent=t('ttsBehind').replace('{0}',ttsQueue.length);
    ttsSkipIndicator.style.display='block';
  }else{
    if(ttsSkipIndicator)ttsSkipIndicator.style.display='none';
  }
}

/* ── Font/style settings ── */
var fontFamily=localStorage.getItem('fontFamily')||"'Segoe UI',Arial,sans-serif";
var isBold=localStorage.getItem('bold')==='true';
var textColor=localStorage.getItem('textColor')||'#FFFFFF';
if(localStorage.getItem('fontSize'))fontSize=parseInt(localStorage.getItem('fontSize'));
(function(){var fs=document.getElementById('fontSelect');for(var i=0;i<fs.options.length;i++){if(fs.options[i].value===fontFamily){fs.selectedIndex=i;break}}
  var bp=document.getElementById('btnBold');if(isBold){bp.classList.add('active')}
  document.getElementById('colorPicker').value=textColor;
})();
function applyStylesToAll(){
  var allLines=document.querySelectorAll('.line');for(var li=0;li<allLines.length;li++){var el=allLines[li];el.style.fontSize=fontSize+'px';el.style.fontFamily=fontFamily;el.style.fontWeight=isBold?'bold':'normal';if(!el.classList.contains('in-progress')&&!el.dataset.highlighted)el.style.color=textColor}
  if(currentEl){currentEl.style.fontSize=fontSize+'px';currentEl.style.fontFamily=fontFamily;currentEl.style.fontWeight=isBold?'bold':'normal'}
  autoScroll()}
function changeFontSize(d){fontSize=Math.max(12,Math.min(80,fontSize+d));localStorage.setItem('fontSize',fontSize);applyStylesToAll();closeAllPanels()}
function changeFont(f){fontFamily=f;localStorage.setItem('fontFamily',f);applyStylesToAll()}
function toggleBold(){isBold=!isBold;localStorage.setItem('bold',isBold);document.getElementById('btnBold').classList.toggle('active');applyStylesToAll();closeAllPanels()}
function changeColor(c){textColor=c;localStorage.setItem('textColor',c);applyStylesToAll()}

/* ── Save transcript ── */
function saveTranscript(){
  var els=document.querySelectorAll('.line:not(.in-progress)');
  var text='';
  for(var i=0;i<els.length;i++){
    var ln=els[i].textContent;if(ln)text+=ln+'\n';
  }
  if(!text){return}
  var blob=new Blob(['\uFEFF'+text],{type:'text/plain;charset=utf-8'});
  var url=URL.createObjectURL(blob);
  var a=document.createElement('a');
  a.href=url;
  var d=new Date();
  var pad=function(n){return n<10?'0'+n:''+n};
  a.download='transcript_'+d.getFullYear()+'-'+pad(d.getMonth()+1)+'-'+pad(d.getDate())+'_'+pad(d.getHours())+pad(d.getMinutes())+'.txt';
  document.body.appendChild(a);a.click();
  setTimeout(function(){document.body.removeChild(a);URL.revokeObjectURL(url)},100);
  closeAllPanels();
}

/* ── Scroll management ── */
var userScrolled=false;
var scrollMode=localStorage.getItem('scrollDir')||'up';
var tagMode=localStorage.getItem('tagMode')||'lang';
function setTagMode(val){tagMode=val;localStorage.setItem('tagMode',val);closeAllPanels()}
function buildTag(lang,time){
  var parts=[];
  if((tagMode==='lang'||tagMode==='both')&&lang){parts.push(lang)}
  if((tagMode==='time'||tagMode==='both')&&time){parts.push(time)}
  if(parts.length===0)return '';
  return '['+parts.join(' ')+'] ';
}
var spacer=document.getElementById('spacer');
function applyScrollMode(){
  if(scrollMode==='down'){spacer.style.display='none';container.scrollTop=0}
  else{spacer.style.display='';container.scrollTop=container.scrollHeight}
  userScrolled=false;
}
function setScrollDir(val){scrollMode=val;localStorage.setItem('scrollDir',val);applyScrollMode();closeAllPanels()}
container.addEventListener('scroll',function(){
  if(scrollMode==='down'){var atTop=container.scrollTop<60;userScrolled=!atTop}
  else{var atBottom=container.scrollHeight-container.scrollTop-container.clientHeight<60;userScrolled=!atBottom}
});
function autoScroll(){if(!userScrolled){if(scrollMode==='down'){container.scrollTop=0}else{container.scrollTop=container.scrollHeight}}}

/* ── Subtitle rendering ── */
function insertLine(el){
  if(scrollMode==='down'){
    var first=spacer.nextSibling;
    if(first){lines.insertBefore(el,first)}else{lines.appendChild(el)}
  }else{lines.appendChild(el)}
}
function addCommitted(text,lang,time,refs,speaker,ttsLang){
  /* Dictation view: commits accumulate in the editor, not the caption stream. */
  if(pttRoomType==='dictation'){
    var dta=document.getElementById('dictText');
    if(dta){
      var ds=(text||'').replace(/^\s+|\s+$/g,'');
      if(ds){dta.value+=(dta.value&&!(/\s$/.test(dta.value))?' ':'')+ds;dta.scrollTop=dta.scrollHeight}
      return;
    }
  }
  var el;
  if(currentEl){el=currentEl;currentEl=null;
    if(scrollMode==='down'&&el.parentNode){el.parentNode.removeChild(el);insertLine(el)}
  }else{el=document.createElement('div');insertLine(el)}
  var tag=buildTag(lang,time);
  var speakerCol=speaker?getSpeakerColor(speaker):'';
  var inRoom=location.search.indexOf('room=')!==-1;
  if(refs&&refs.length>0){
    el.innerHTML='';
    if(tag){var tagSpan=document.createElement('span');tagSpan.textContent=tag;el.appendChild(tagSpan)}
    if(speaker){var spk=document.createElement('span');spk.style.fontWeight='600';if(speakerCol)spk.style.color=speakerCol;spk.textContent=speaker+': ';el.appendChild(spk)}
    renderTextWithRefs(el,text,refs);
  }else{
    el.innerHTML='';
    if(tag){el.appendChild(document.createTextNode(tag))}
    if(speaker){var spk2=document.createElement('span');spk2.style.fontWeight='600';if(speakerCol)spk2.style.color=speakerCol;spk2.textContent=speaker+': ';el.appendChild(spk2)}
    el.appendChild(document.createTextNode(text));
  }
  if(inRoom&&speaker){addLineSpeakBtn(el,text,ttsLang)}
  el.className='line';
  el.style.fontSize=fontSize+'px';el.style.fontFamily=fontFamily;el.style.fontWeight=isBold?'bold':'normal';
  el.style.color='#ffdd57';
  el.dataset.highlighted='1';
  setTimeout(function(){el.style.color=textColor;delete el.dataset.highlighted},5000);
  autoScroll();
  /* Server TTS already pushes audio for auto-commits via FireTtsForCommit.
     Only call speak() for browser voices — requestTts is for on-demand only. */
  if(!useServerTts()){speak(text,ttsLang)}
}

/* Tap-to-read button on room message lines */
function addLineSpeakBtn(lineEl,text,ttsLang){
  var btn=document.createElement('span');
  btn.className='line-speak-btn';
  btn.textContent='\u25B6';
  btn.title='Read aloud';
  btn.addEventListener('click',function(e){
    e.stopPropagation();
    var lang=ttsLang||getActiveIdentityLang();
    if(serverTtsActive){
      /* Use server TTS */
      if(wsRef&&wsRef.readyState===1){
        wsRef.send(JSON.stringify({type:'requestTts',text:text,language:lang}));
      }
    }else{
      /* Use browser voice */
      synth.cancel();
      var utter=new SpeechSynthesisUtterance(text);
      utter.rate=speechRate;
      if(selectedVoice){var voices=synth.getVoices();for(var i=0;i<voices.length;i++){if(voices[i].name===selectedVoice){utter.voice=voices[i];break}}}
      synth.speak(utter);
    }
    btn.style.color='#4f4';
    setTimeout(function(){btn.style.color=''},1000);
  });
  lineEl.style.position='relative';
  lineEl.classList.add('has-speak');
  lineEl.appendChild(btn);
}

function renderTextWithRefs(el,text,refs){
  /* Sort refs by start index descending so we can splice from the end */
  var sorted=refs.slice().sort(function(a,b){return a.start-b.start});
  var pos=0;
  for(var i=0;i<sorted.length;i++){
    var r=sorted[i];
    if(r.start>pos){el.appendChild(document.createTextNode(text.substring(pos,r.start)))}
    var link=document.createElement('span');
    link.className='bible-ref-link';
    link.textContent=text.substring(r.start,r.start+r.len);
    link.dataset.book=r.book;link.dataset.ch=r.chapter;
    link.dataset.vs=r.verseStart;link.dataset.ve=r.verseEnd;
    link.onclick=function(e){
      e.stopPropagation();
      openBibleRef(this.dataset.book,parseInt(this.dataset.ch),parseInt(this.dataset.vs),parseInt(this.dataset.ve));
    };
    el.appendChild(link);
    pos=r.start+r.len;
  }
  if(pos<text.length){el.appendChild(document.createTextNode(text.substring(pos)))}
}
function updateCurrent(text){
  if(!currentEl){currentEl=document.createElement('div');currentEl.className='line in-progress';currentEl.style.fontSize=fontSize+'px';currentEl.style.fontFamily=fontFamily;currentEl.style.fontWeight=isBold?'bold':'normal';insertLine(currentEl)}
  currentEl.textContent=text;
  autoScroll()
}
applyScrollMode();

/* ── WebSocket connection ── */
var wsRef=null;
var lastCommitId=0;
var myClientId='';
function setTransLang(lang){
  LOG('setTransLang: '+lang);
  myTransLang=lang;
  ssSet('transLang',lang);
  closeAllPanels();
  if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'setLanguage',language:lang}))}
  sendTtsState(); /* useServerTts() depends on voice availability for the new language */
  /* Reload Bible translations for the new language */
  bibleTranslations=[];
  bibleNavStack=[];
  cachedBooks=[];
  cachedBooksTransId='';
}
/* transLangSelect populated dynamically from LANGS array above */
function connect(){
  if(wsRef&&wsRef.readyState<2){return}
  var proto=location.protocol==='https:'?'wss:':'ws:';
  var wsUrl=proto+'//'+location.host+'/ws';
  var wsParams=[];
  if(location.search.indexOf('preview')!==-1){wsParams.push('preview=1')}
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(roomMatch){wsParams.push('room='+roomMatch[1])}
  if(wsParams.length>0){wsUrl+='?'+wsParams.join('&')}
  var ws=new WebSocket(wsUrl);
  wsRef=ws;
  ws.onopen=function(){LOG('WS connected');statusEl.textContent=t('connected');statusBar.className='connected';
    ensureCapBadge();updateCapBadge();slogFlush();
    if(currentEl){currentEl.remove();currentEl=null}
    ws.send(JSON.stringify({type:'setLanguage',language:myTransLang||'',lastId:lastCommitId}));
    sendTtsState();
    /* Auto-resume web-mic broadcast: intent survives the reconnect; the server
       re-grants (our new clientId re-claims host via tryClaimHost first, so a
       brief retry loop covers the ordering). */
    if(bcWant&&!bcActive){setTimeout(function(){if(bcWant&&!bcActive&&wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'broadcastStart'}))}},1500)}
  };
  ws.onclose=function(){LOG('WS closed');statusEl.textContent=t('disconnected');statusBar.className='disconnected';wsRef=null;updateCapBadge();
    /* Keep bcWant (auto-resume) but tear down capture — frames have nowhere to go. */
    if(bcActive){stopBroadcastCapture();updateBroadcastUi('starting')}
    setTimeout(connect,2000)};
  ws.onerror=function(){LOG('WS error');ws.close()};
  ws.onmessage=function(e){
    try{var msg=JSON.parse(e.data);
      if(msg.type==='commit'){
        /* Delivery self-check: count RAW arrivals (before id-dedup) so the badge
           reflects what the socket actually delivered to this device. */
        capCount++;capLastAt=Date.now();updateCapBadge();
        /* Diagnose-in-the-field: the very first commit's raw JSON goes to the
           server log, and every commit reports rendered/skipped/error — this
           handler swallowing a problem silently is how "no subtitles" hid. */
        if(capCount===1){diagLog('first_commit_raw='+String(e.data).slice(0,300))}
        /* Commit arrival clears speaking indicator for that speaker */
        if(msg.speaker){clearSpeakerByName(msg.speaker);updateSpeakingUI()}
        var id=msg.id||0;
        if(id>lastCommitId){
          lastCommitId=id;
          try{
            if(msg.translations){
              /* Shared-device message with all translations */
              transcriptCache.push({id:id,speaker:dispName(msg.speaker),time:msg.time||'',lang:msg.lang||'',sourceLang:msg.sourceLang||'',translations:msg.translations});
              var activeLang=getActiveIdentityLang();
              var displayText=msg.translations[activeLang]||msg.translations[msg.sourceLang]||msg.translations[Object.keys(msg.translations)[0]]||'';
              addCommitted(displayText,msg.lang||'',msg.time||'',null,dispName(msg.speaker),activeLang);
            } else {
              /* Normal single-language message — text is in my language (server translated for me) */
              var textLang=myTransLang||msg.sourceLang||'';
              transcriptCache.push({id:id,speaker:dispName(msg.speaker),time:msg.time||'',text:msg.text||'',lang:msg.lang||'',sourceLang:msg.sourceLang||'',ttsLang:textLang});
              addCommitted(msg.text,msg.lang||'',msg.time||'',msg.refs||null,dispName(msg.speaker),textLang);
            }
            capRendered++;
          }catch(rex){
            slogDiag('commit_render_error id='+id+' err='+rex+(rex&&rex.stack?' @'+String(rex.stack).slice(0,200):''));
          }
        }else{
          slogDiag('commit_skipped id='+id+' lastCommitId='+lastCommitId);
        }
      }
      else if(msg.type==='update')updateCurrent(msg.text);
      else if(msg.type==='clear'){LOG('WS clear');if(currentEl){currentEl.remove();currentEl=null}while(lines.children.length>1)lines.removeChild(lines.children[1]);lastCommitId=0;transcriptCache=[];clearTtsQueue();autoScroll()}
      else if(msg.type==='tts'){handleTtsMessage(msg)}
      else if(msg.type==='welcome'){myClientId=msg.clientId||'';LOG('My client ID: '+myClientId);initPushToTalk();tryClaimHost()}
      else if(msg.type==='pong'){}
      else if(msg.type==='error'){showRoomError(msg.message||'Error')}
      else if(msg.type==='broadcastState'){handleBroadcastState(msg)}
      else if(msg.type==='roomClosed'){stopBroadcast(false);showRoomError(t('roomEnded'));setTimeout(afterRoomGone,3000)}
      else if(msg.type==='kicked'){showRoomError(t('roomKicked'));setTimeout(afterRoomGone,3000)}
      else if(msg.type==='roomLocked'){LOG('Room locked: '+msg.locked)}
      else if(msg.type==='pttModeChanged'){pttMode=msg.mode||'hold';updatePttLabel()}
      else if(msg.type==='pauseStateChanged'){
        window._roomPaused=!!msg.paused;
        var pb=document.getElementById('hcPauseBtn');
        if(pb){pb.style.background=msg.paused?'#e74c3c':'#27ae60';pb.textContent=msg.paused?'\u23F8 Paused':'\u25B6 Playing'}
        LOG('Room paused: '+msg.paused);
      }
      else if(msg.type==='memberJoined'){addRoomMember(msg)}
      else if(msg.type==='memberLeft'){removeRoomMember(msg.clientId)}
      else if(msg.type==='memberUpdated'){updateRoomMember(msg)}
      else if(msg.type==='virtualMemberAdded'){addVirtualMemberToRoom(msg)}
      else if(msg.type==='virtualMemberRemoved'){removeVirtualMemberFromRoom(msg.id)}
      else if(msg.type==='speaking'){handleSpeakingIndicator(msg)}
      else if(msg.type==='roomStatus'){handleRoomStatus(msg)}
      else{LOG('WS unknown msg type: '+msg.type)}
    }catch(ex){slogDiag('ws_msg_error='+ex+' data='+String(e.data).substring(0,100))}
  }
}

/* Reconnect immediately when phone screen wakes / tab becomes visible */
document.addEventListener('visibilitychange',function(){
  if(!document.hidden&&(!wsRef||wsRef.readyState>1)){connect()}
});

/* Keepalive: detect dead connections faster (every 15s) */
setInterval(function(){
  if(wsRef&&wsRef.readyState===1){try{wsRef.send(JSON.stringify({type:'ping'}))}catch(ex){}}
},15000);

/* ── Apply i18n to HTML elements (pre-connect) ── */
document.title=t('title');
statusEl.textContent=t('connecting');
document.getElementById('btnSettings').title=t('settings');
document.getElementById('lblSpeak').textContent=t('readAloud');
if(!speakEnabled){btnSpeak.innerHTML='&#128264; '+t('readAloud')}
document.getElementById('lblFont').textContent=t('font');
document.getElementById('lblColor').textContent=t('color');
document.getElementById('btnBold').textContent=t('bold');
document.getElementById('lblVoice').textContent=t('voice');
document.getElementById('lblSpeed').textContent=t('speed');
document.getElementById('lblScroll').textContent=t('scrollDir');
var sdOpts=document.getElementById('scrollDir').options;sdOpts[0].textContent=t('scrollUp');sdOpts[1].textContent=t('scrollDown');
(function(){var sd=document.getElementById('scrollDir');sd.value=scrollMode;})();
document.getElementById('lblTags').textContent=t('tags');
var tmOpts=document.getElementById('tagMode').options;tmOpts[0].textContent=t('tagOff');tmOpts[1].textContent=t('tagLang');tmOpts[2].textContent=t('tagTime');tmOpts[3].textContent=t('tagBoth');
(function(){var tm=document.getElementById('tagMode');tm.value=tagMode;})();
document.getElementById('btnSave').innerHTML='&#128190; '+t('saveTranscript');
document.getElementById('lblDisplayName').textContent=t('yourName');
document.getElementById('displayNameInput').placeholder=t('enterYourName');
document.getElementById('lpTitle').textContent=t('chooseLang');
document.getElementById('lpSearch').placeholder=t('searchLangs');
document.getElementById('lpSkip').textContent=t('noTranslation');
document.getElementById('lpAdminToggle').textContent=t('adminLabel');

/* ── Admin moved to /admin.html (three-tier IA): the picker's Administrator
   link navigates there; ALL server config UI lives on that page now. ── */
localStorage.removeItem('isAdmin');
var serverPublicHost='';
var serverHasLiveSession=false;
fetch('/api/config').then(function(r){return r.json()}).then(function(cfg){
  serverPublicHost=cfg.publicHost||'';
  serverHasLiveSession=!!cfg.hasLiveSession;
  var lpa=document.getElementById('lpAdmin');
  if(lpa)lpa.style.display='';
}).catch(function(){});
function openAdminPage(){location.href='/admin.html'}

var rOpts=rateSelect.options;rOpts[0].textContent=t('slow');rOpts[1].textContent=t('normal');rOpts[2].textContent=t('fast');rOpts[3].textContent=t('vfast');

/* ── Fetch config and apply dynamic colors, then connect ── */
(function(){
  fetch('/api/config').then(function(r){return r.json()}).then(function(cfg){
    if(cfg.showBibleCopyright===false){showBibleCopyright=false}
    if(cfg.bgColor){document.documentElement.style.setProperty('--bg-color',cfg.bgColor)}
    if(cfg.fgColor){
      document.documentElement.style.setProperty('--fg-color',cfg.fgColor);
      if(!localStorage.getItem('textColor')){textColor=cfg.fgColor;document.getElementById('colorPicker').value=cfg.fgColor}
    }
  }).catch(function(){});
  connect();
  /* Preview mode (embedded WebView2) — use full width */
  if(location.search.indexOf('preview')!==-1){
    document.getElementById('lines').style.maxWidth='none';
  }
  /* Show language picker on page load — skip for ?bibleLang= (desktop panel)
     and ?join= (template-pointer waiting flow shows its own overlay first) */
  var _qs=new URLSearchParams(window.location.search);
  if(!_qs.get('bibleLang')&&!_qs.get('join')){showLangPicker()}
})();

/* ── Permanent-QR join flow: ?join={templateId} resolves to whichever room is
   currently running from that template. No room yet → waiting page that polls
   and auto-joins the moment the host starts the service. The template id is
   remembered so a mid-service room restart heals on the next resolve. ── */
(function(){
  var _js=new URLSearchParams(window.location.search);
  var joinTpl=_js.get('join');
  if(!joinTpl)return;
  sessionStorage.setItem('joinTpl',joinTpl);
  var ov=document.createElement('div');
  ov.id='joinWait';
  ov.style.cssText='position:fixed;inset:0;background:#000;z-index:2000;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:32px;text-align:center';
  ov.innerHTML='<div style="font-size:42px;margin-bottom:18px">\uD83D\uDD4A\uFE0F</div>'+
    '<div id="joinWaitMsg" style="color:#ccc;font-size:17px;line-height:1.5;max-width:340px">'+t('joinWaiting')+'</div>'+
    '<div id="joinWaitDots" style="color:#7c9cf7;font-size:26px;margin-top:14px;min-height:30px">\u00B7</div>';
  document.body.appendChild(ov);
  var dots=0;
  setInterval(function(){dots=(dots%4)+1;var d=document.getElementById('joinWaitDots');if(d)d.textContent='\u00B7\u00B7\u00B7\u00B7'.slice(0,dots)},600);
  function poll(){
    fetch('/api/templates/'+encodeURIComponent(joinTpl)+'/active-room').then(function(r){return r.json()}).then(function(res){
      if(res.error){document.getElementById('joinWaitMsg').textContent=t('joinUnknown');return}
      if(res.active){location.replace('/index.html?room='+encodeURIComponent(res.roomId));return}
      setTimeout(poll,5000);
    }).catch(function(){setTimeout(poll,7000)});
  }
  poll();
})();

/* ── Keep screen on (Wake Lock) — ALWAYS ON, no button.
   Silent no-op where unsupported (plain HTTP, iOS before 16.4) — those users
   just keep their normal auto-lock behaviour. The lock auto-releases when the
   page is hidden; we re-acquire on return-to-visible. Failures only LOG. */
var wakeLockObj=null;

function acquireWakeLock(){
  if(!window.isSecureContext||!('wakeLock' in navigator)){
    LOG('Wake lock unavailable (secureContext='+window.isSecureContext+')');
    return;
  }
  navigator.wakeLock.request('screen').then(function(lock){
    wakeLockObj=lock;
    LOG('Wake lock acquired');
    lock.addEventListener('release',function(){wakeLockObj=null});
  },function(e){
    LOG('Wake lock denied: '+e);
  });
}

document.addEventListener('visibilitychange',function(){
  if(document.visibilityState==='visible'&&!wakeLockObj)acquireWakeLock();
});

acquireWakeLock();

/* Admin remote control moved to /admin.html (Live session card). */

/* ── Bible Panel ── */
var biblePanel=document.getElementById('biblePanel');
var bibleTransSelect=document.getElementById('bibleTransSelect');
var bibleContent=document.getElementById('bibleContent');
var bibleNavTitle=document.getElementById('bibleNavTitle');
var btnBibleBack=document.getElementById('btnBibleBack');
var bibleSearchBox=document.getElementById('bibleSearchBox');
var bibleTranslations=[];
var bibleNavStack=[];
var currentBibleTrans='';
document.getElementById('btnBible').title=t('bible');

/* OT/NT book order for display (short_name codes matching BibleService BookAliases) */
var otBooks=['Gen','Exod','Lev','Num','Deut','Josh','Judg','Ruth','1Sam','2Sam','1Kgs','2Kgs','1Chr','2Chr','Ezra','Neh','Esth','Job','Ps','Prov','Eccl','Song','Isa','Jer','Lam','Ezek','Dan','Hos','Joel','Amos','Obad','Jonah','Mic','Nah','Hab','Zeph','Hag','Zech','Mal'];
var ntBooks=['Matt','Mark','Luke','John','Acts','Rom','1Cor','2Cor','Gal','Eph','Phil','Col','1Thess','2Thess','1Tim','2Tim','Titus','Phlm','Heb','Jas','1Pet','2Pet','1John','2John','3John','Jude','Rev'];

function toggleBible(){LOG('toggleBible');
  if(biblePanel.classList.contains('open')){biblePanel.classList.remove('open');return}
  closeAllPanels();
  biblePanel.classList.add('open');
  if(bibleTranslations.length===0){loadBibleTranslations()}
  else{showBookList()}
}

function closeBible(){LOG('closeBible');biblePanel.classList.remove('open')}

/* Auto-open Bible panel when loaded from desktop app with ?bibleLang= */
(function(){
  var params=new URLSearchParams(window.location.search);
  if(params.get('bibleLang')){toggleBible()}
})();

function getBibleLang(){
  /* 1. Check URL param from desktop app (e.g. ?bibleLang=eng) */
  var params=new URLSearchParams(window.location.search);
  var bl=params.get('bibleLang');
  if(bl)return bl;
  /* 2. Use app translation language — extract 3-letter code from FLORES code */
  var tl=myTransLang||'';
  if(tl){
    for(var i=0;i<LANGS.length;i++){if(LANGS[i][0]===tl)return LANGS[i][0].substring(0,3)}
  }
  /* 3. Fall back to browser language — look up ISO 639-1 in LANGS to get 3-letter code */
  var iso2=(navigator.language||'en').split('-')[0];
  for(var i=0;i<LANGS.length;i++){if(LANGS[i][3]===iso2)return LANGS[i][0].substring(0,3)}
  return 'eng';
}

function loadBibleTranslations(){
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Loading...</div>';
  currentBibleTrans='';
  var lang=getBibleLang();
  fetch('/bible/translations?lang='+encodeURIComponent(lang),{cache:'no-store'}).then(function(r){return r.json()}).then(function(data){
    if(!data||!Array.isArray(data)||data.length===0){
      bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">'+t('bibleNoBibles')+'<br>'+t('bibleNoBiblesHint')+'</div>';
      return;
    }
    populateBibleData(data);
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:40px">'+t('bibleTransFail')+'</div>';
  });
}

function populateBibleData(data){
  bibleTranslations=data;
  bibleTransSelect.innerHTML='';
  for(var i=0;i<data.length;i++){
    var opt=document.createElement('option');
    opt.value=data[i].id;
    opt.textContent=data[i].name+' ('+data[i].language+')';
    bibleTransSelect.appendChild(opt);
  }
  if(data.length>0){
    var saved=localStorage.getItem('bibleTrans');
    if(saved){for(var j=0;j<data.length;j++){if(data[j].id===saved){bibleTransSelect.value=saved;break}}}
    currentBibleTrans=bibleTransSelect.value;
    if(currentBibleTrans)localStorage.setItem('bibleTrans',currentBibleTrans);
  }
  showBookList();
}

function refreshBibleDropdown(){
  var lang=getBibleLang();
  fetch('/bible/translations?lang='+encodeURIComponent(lang),{cache:'no-store'}).then(function(r){return r.json()}).then(function(data){
    if(!data||!Array.isArray(data)||data.length===0)return;
    bibleTranslations=data;
    var cur=bibleTransSelect.value;
    bibleTransSelect.innerHTML='';
    for(var i=0;i<data.length;i++){
      var opt=document.createElement('option');
      opt.value=data[i].id;
      opt.textContent=data[i].name+' ('+data[i].language+')';
      bibleTransSelect.appendChild(opt);
    }
    if(cur){bibleTransSelect.value=cur}
    if(!bibleTransSelect.value&&data.length>0){bibleTransSelect.value=data[0].id}
    currentBibleTrans=bibleTransSelect.value;
    if(currentBibleTrans)localStorage.setItem('bibleTrans',currentBibleTrans);
  });
}

function onBibleTransChange(val){
  currentBibleTrans=val;
  localStorage.setItem('bibleTrans',val);
  cachedBooks=[];
  cachedBooksTransId='';

  /* Re-execute current view with the new translation */
  var refInput=document.getElementById('bibleRefInput').value.trim();
  var searchInput=document.getElementById('bibleSearchInput').value.trim();
  var navTitle=bibleNavTitle.textContent||'';

  /* If viewing a chapter (nav stack has books + chapters entry) */
  if(bibleNavStack.length>=2&&bibleNavStack[bibleNavStack.length-1].type==='chapters'){
    var book=bibleNavStack[bibleNavStack.length-1].book;
    /* Check if we were viewing specific verses (title contains ':') */
    var colIdx=navTitle.indexOf(':');
    if(colIdx>-1){
      /* Re-run the reference lookup */
      if(refInput){lookupRef();return}
    }
    /* Re-show the chapter we were on — parse chapter from title */
    var parts=navTitle.split(' ');
    var ch=parseInt(parts[parts.length-1]);
    if(ch>0){showVerses(book,ch);return}
  }

  /* If a search was active, re-run it */
  if(searchInput&&bibleSearchBox.style.display==='flex'){bibleSearch();return}

  /* If a reference was entered, re-run it */
  if(refInput){lookupRef();return}

  /* Default: show book list */
  bibleNavStack=[];
  showBookList();
}

var cachedBooks=[];
var cachedBooksTransId='';

function showBookList(){
  LOG('showBookList');
  bibleNavStack=[];
  btnBibleBack.style.display='none';
  bibleNavTitle.textContent=t('bible');
  bibleSearchBox.style.display='none';
  bibleContent.innerHTML='';

  if(!currentBibleTrans){
    bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:20px">'+t('bibleSelectTrans')+'</div>';
    return;
  }

  /* Fetch book names from the server for this translation */
  if(cachedBooksTransId===currentBibleTrans&&cachedBooks.length>0){
    renderBookGrid(cachedBooks);
    return;
  }

  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Loading...</div>';
  fetch('/bible/'+encodeURIComponent(currentBibleTrans)+'/books').then(function(r){return r.json()}).then(function(books){
    cachedBooks=books||[];
    cachedBooksTransId=currentBibleTrans;
    renderBookGrid(cachedBooks);
  }).catch(function(){
    /* Fallback to hardcoded English list */
    renderBookGridFallback();
  });
}

function renderBookGrid(books){
  bibleContent.innerHTML='';
  /* OT = book_number < 470 (before Matthew), NT = 470+ */
  var otList=[];var ntList=[];
  for(var i=0;i<books.length;i++){
    if(books[i].number<470){otList.push(books[i])}
    else{ntList.push(books[i])}
  }

  var ot=document.createElement('div');ot.className='bible-ot-label';ot.textContent=t('bibleOT');
  bibleContent.appendChild(ot);
  var otGrid=document.createElement('div');otGrid.className='bible-book-grid';
  for(var i=0;i<otList.length;i++){
    var btn=document.createElement('button');btn.className='bible-book-btn';
    btn.textContent=otList[i].shortName;btn.dataset.book=otList[i].shortName;
    btn.title=otList[i].longName;
    btn.onclick=function(){showChapters(this.dataset.book)};
    otGrid.appendChild(btn);
  }
  bibleContent.appendChild(otGrid);

  var nt=document.createElement('div');nt.className='bible-nt-label';nt.style.marginTop='16px';nt.textContent=t('bibleNT');
  bibleContent.appendChild(nt);
  var ntGrid=document.createElement('div');ntGrid.className='bible-book-grid';
  for(var j=0;j<ntList.length;j++){
    var btn2=document.createElement('button');btn2.className='bible-book-btn';
    btn2.textContent=ntList[j].shortName;btn2.dataset.book=ntList[j].shortName;
    btn2.title=ntList[j].longName;
    btn2.onclick=function(){showChapters(this.dataset.book)};
    ntGrid.appendChild(btn2);
  }
  bibleContent.appendChild(ntGrid);
  bibleContent.scrollTop=0;
}

function renderBookGridFallback(){
  bibleContent.innerHTML='';
  var ot=document.createElement('div');ot.className='bible-ot-label';ot.textContent=t('bibleOT');
  bibleContent.appendChild(ot);
  var otGrid=document.createElement('div');otGrid.className='bible-book-grid';
  for(var i=0;i<otBooks.length;i++){
    var btn=document.createElement('button');btn.className='bible-book-btn';
    btn.textContent=otBooks[i];btn.dataset.book=otBooks[i];
    btn.onclick=function(){showChapters(this.dataset.book)};
    otGrid.appendChild(btn);
  }
  bibleContent.appendChild(otGrid);
  var nt=document.createElement('div');nt.className='bible-nt-label';nt.style.marginTop='16px';nt.textContent=t('bibleNT');
  bibleContent.appendChild(nt);
  var ntGrid=document.createElement('div');ntGrid.className='bible-book-grid';
  for(var j=0;j<ntBooks.length;j++){
    var btn2=document.createElement('button');btn2.className='bible-book-btn';
    btn2.textContent=ntBooks[j];btn2.dataset.book=ntBooks[j];
    btn2.onclick=function(){showChapters(this.dataset.book)};
    ntGrid.appendChild(btn2);
  }
  bibleContent.appendChild(ntGrid);
  bibleContent.scrollTop=0;
}

function showChapters(book){
  LOG('showChapters: '+book);
  if(!currentBibleTrans){bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleSelectTrans')+'</div>';return}
  bibleNavStack=[{type:'books'}];
  btnBibleBack.style.display='';
  /* Show long name in title if available */
  var displayName=book;
  for(var b=0;b<cachedBooks.length;b++){if(cachedBooks[b].shortName===book){displayName=cachedBooks[b].longName;break}}
  bibleNavTitle.textContent=displayName;
  bibleSearchBox.style.display='none';
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Loading...</div>';

  fetch('/bible/'+encodeURIComponent(currentBibleTrans)+'/'+encodeURIComponent(book)+'/1').then(function(r){return r.json()}).then(function(data){
    /* Get chapter count from cached books data, fallback to hardcoded */
    var maxCh=getBookChapterCount(book);
    for(var b=0;b<cachedBooks.length;b++){if(cachedBooks[b].shortName===book&&cachedBooks[b].chapters>0){maxCh=cachedBooks[b].chapters;break}}
    bibleContent.innerHTML='';
    var grid=document.createElement('div');grid.className='bible-chapter-grid';
    for(var c=1;c<=maxCh;c++){
      var btn=document.createElement('button');btn.className='bible-ch-btn';
      btn.textContent=c;btn.dataset.book=book;btn.dataset.ch=c;
      btn.onclick=function(){showVerses(this.dataset.book,parseInt(this.dataset.ch))};
      grid.appendChild(btn);
    }
    bibleContent.appendChild(grid);
    bibleContent.scrollTop=0;
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleLoadFail')+'</div>';
  });
}

var showBibleCopyright=true;
function appendCopyrightFooter(){
  if(!showBibleCopyright)return;
  for(var i=0;i<bibleTranslations.length;i++){
    if(bibleTranslations[i].id===currentBibleTrans&&bibleTranslations[i].copyright){
      var d=document.createElement('div');
      d.style.cssText='margin-top:16px;padding:8px 0;border-top:1px solid #444;font-size:11px;color:#888;line-height:1.4';
      d.textContent=bibleTranslations[i].copyright;
      bibleContent.appendChild(d);
      break;
    }
  }
}

function showVerses(book,chapter){
  LOG('showVerses: '+book+' ch='+chapter);
  if(!currentBibleTrans){bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleSelectTrans')+'</div>';return}
  bibleNavStack=[{type:'books'},{type:'chapters',book:book}];
  btnBibleBack.style.display='';
  bibleNavTitle.textContent=book+' '+chapter;
  bibleSearchBox.style.display='none';
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Loading...</div>';

  fetch('/bible/'+encodeURIComponent(currentBibleTrans)+'/'+encodeURIComponent(book)+'/'+chapter).then(function(r){return r.json()}).then(function(data){
    bibleContent.innerHTML='';
    if(!data||!data.verses||data.verses.length===0){
      bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:20px">'+t('bibleNoVerses')+'</div>';return;
    }
    for(var i=0;i<data.verses.length;i++){
      var v=data.verses[i];
      var div=document.createElement('div');div.className='bible-verse';
      var num=document.createElement('span');num.className='vnum';num.textContent=v.verse;
      div.appendChild(num);
      div.appendChild(document.createTextNode(' '+v.text));
      addVerseSpeakBtn(div,v.text);
      bibleContent.appendChild(div);
    }
    addReadAllBtn();
    appendCopyrightFooter();
    bibleContent.scrollTop=0;
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleLoadFail')+'</div>';
  });
}

function bibleBack(){LOG('bibleBack');
  if(bibleNavStack.length===0){closeBible();return}
  var prev=bibleNavStack.pop();
  if(prev.type==='books')showBookList();
  else if(prev.type==='chapters')showChapters(prev.book);
}

function lookupRef(){LOG('lookupRef');
  var input=document.getElementById('bibleRefInput').value.trim();
  if(!input||!currentBibleTrans)return;
  fetch('/bible/parse?ref='+encodeURIComponent(input)+'&translation='+encodeURIComponent(currentBibleTrans)).then(function(r){return r.json()}).then(function(ref){
    if(!ref||!ref.isValid){bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleBadRef')+'</div>';return}
    if(ref.verseStart>0){
      var versePath=ref.verseStart+(ref.verseEnd>ref.verseStart?'-'+ref.verseEnd:'');
      fetch('/bible/'+encodeURIComponent(currentBibleTrans)+'/'+encodeURIComponent(ref.book)+'/'+ref.chapter+'/'+versePath).then(function(r2){return r2.json()}).then(function(data){
        bibleNavStack=[{type:'books'},{type:'chapters',book:ref.book}];
        btnBibleBack.style.display='';
        bibleNavTitle.textContent=ref.book+' '+ref.chapter+':'+versePath;
        bibleSearchBox.style.display='none';
        bibleContent.innerHTML='';
        var verses=Array.isArray(data)?data:(data&&data.verses?data.verses:[]);
        for(var i=0;i<verses.length;i++){
          var v=verses[i];
          var div=document.createElement('div');div.className='bible-verse';
          var num=document.createElement('span');num.className='vnum';num.textContent=v.verse;
          div.appendChild(num);div.appendChild(document.createTextNode(' '+v.text));
          addVerseSpeakBtn(div,v.text);
          bibleContent.appendChild(div);
        }
        addReadAllBtn();
        appendCopyrightFooter();
      }).catch(function(){});
    }else{
      showVerses(ref.book,ref.chapter);
    }
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleRefError')+'</div>';
  });
}

function toggleBibleSearch(){
  var sb=bibleSearchBox;
  sb.style.display=sb.style.display==='none'?'flex':'none';
  if(sb.style.display==='flex')document.getElementById('bibleSearchInput').focus();
}

function bibleSearch(){LOG('bibleSearch');
  var q=document.getElementById('bibleSearchInput').value.trim();
  if(!q||!currentBibleTrans)return;
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">'+t('bibleSearching')+'</div>';
  bibleNavStack=[{type:'books'}];
  btnBibleBack.style.display='';
  bibleNavTitle.textContent=t('bibleSearch')+': '+q;

  fetch('/bible/search?q='+encodeURIComponent(q)+'&translation='+encodeURIComponent(currentBibleTrans)+'&max=200').then(function(r){return r.json()}).then(function(results){
    bibleContent.innerHTML='';
    if(!results||results.length===0){
      bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:20px">'+t('bibleNoResults')+'</div>';return;
    }
    var countDiv=document.createElement('div');countDiv.style.cssText='color:#888;font-size:12px;padding:4px 0 8px;border-bottom:1px solid #333;margin-bottom:8px';
    countDiv.textContent=t('bibleResults').replace('{0}',results.length+(results.length>=200?'+':''));
    bibleContent.appendChild(countDiv);
    for(var i=0;i<results.length;i++){
      var r=results[i];
      var div=document.createElement('div');div.className='bible-search-result';
      div.dataset.book=r.book;div.dataset.ch=r.chapter;div.dataset.v=r.verse;
      div.onclick=function(){showVerses(this.dataset.book,parseInt(this.dataset.ch))};
      var ref=document.createElement('div');ref.className='bible-search-ref';ref.textContent=r.book+' '+r.chapter+':'+r.verse;
      var txt=document.createElement('div');txt.className='bible-search-text';txt.textContent=r.text;
      div.appendChild(ref);div.appendChild(txt);
      addVerseSpeakBtn(div,r.text);
      bibleContent.appendChild(div);
    }
    addReadAllBtn();
    appendCopyrightFooter();
    bibleContent.scrollTop=0;
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleSearchFail')+'</div>';
  });
}

/* Open a specific Bible ref (from tappable link in subtitles) */
function openBibleRef(book,chapter,verseStart,verseEnd){
  if(!biblePanel.classList.contains('open')){closeAllPanels();biblePanel.classList.add('open')}
  if(bibleTranslations.length===0){
    loadBibleTranslations();
    /* Retry after translations load */
    setTimeout(function(){openBibleRef(book,chapter,verseStart,verseEnd)},1500);
    return;
  }
  var versePath=verseStart+(verseEnd>verseStart?'-'+verseEnd:'');
  bibleNavStack=[{type:'books'},{type:'chapters',book:book}];
  btnBibleBack.style.display='';
  bibleNavTitle.textContent=book+' '+chapter+':'+versePath;
  bibleSearchBox.style.display='none';
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Loading...</div>';

  fetch('/bible/'+encodeURIComponent(currentBibleTrans)+'/'+encodeURIComponent(book)+'/'+chapter+'/'+versePath).then(function(r){return r.json()}).then(function(data){
    bibleContent.innerHTML='';
    var verses=Array.isArray(data)?data:(data&&data.verses?data.verses:[]);
    for(var i=0;i<verses.length;i++){
      var v=verses[i];
      var div=document.createElement('div');div.className='bible-verse';
      var num=document.createElement('span');num.className='vnum';num.textContent=v.verse;
      div.appendChild(num);div.appendChild(document.createTextNode(' '+v.text));
      addVerseSpeakBtn(div,v.text);
      bibleContent.appendChild(div);
    }
    addReadAllBtn();
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">'+t('bibleLoadFail')+'</div>';
  });
}

/* ── Bible Verse TTS ── */
var bibleTtsActive=false;
var bibleTtsId=0;
function speakBibleVerse(text){
  LOG('speakBibleVerse: len='+text.length+' text="'+text.substring(0,60)+'"');
  clearTtsQueue();
  bibleTtsId++;
  synth.cancel();
  if(!synth||!text){LOG('speakBibleVerse BAIL: synth='+!!synth+' text='+(text?'yes':'empty'));return}
  bibleTtsActive=true;
  var myId=bibleTtsId;
  var voices=synth.getVoices();
  LOG('speakBibleVerse: voices='+voices.length+' id='+myId);
  if(voices.length===0){
    /* Voices not loaded — warm up then retry */
    LOG('speakBibleVerse: warming up voices');
    var warm=new SpeechSynthesisUtterance('');
    synth.speak(warm);
    synth.cancel();
    setTimeout(function(){
      if(bibleTtsId!==myId){LOG('speakBibleVerse: warmup superseded');return}
      doSpeakBible(text,myId);
    },250);
    return;
  }
  doSpeakBible(text,myId);
}
function doSpeakBible(text,myId){
  LOG('doSpeakBible: id='+myId+' len='+text.length);
  var utter=new SpeechSynthesisUtterance(text);
  utter.rate=speechRate;
  if(selectedVoice){var voices=synth.getVoices();for(var i=0;i<voices.length;i++){if(voices[i].name===selectedVoice){utter.voice=voices[i];break}}}
  utter.onend=function(){LOG('doSpeakBible onend id='+myId+' current='+bibleTtsId);if(bibleTtsId===myId){bibleTtsActive=false}};
  utter.onerror=function(e){LOG('doSpeakBible onerror id='+myId+' err='+(e&&e.error||'unknown'));if(bibleTtsId===myId){bibleTtsActive=false}};
  synth.speak(utter);
  LOG('doSpeakBible: synth.speak() called, speaking='+synth.speaking+' pending='+synth.pending);
}

function speakBibleVerseServer(text){
  LOG('speakBibleVerseServer: len='+text.length+' text="'+text.substring(0,60)+'"');
  clearTtsQueue();
  synth.cancel();
  bibleTtsActive=true;
  if(!wsRef||wsRef.readyState!==1){LOG('speakBibleVerseServer BAIL: ws not open');bibleTtsActive=false;return}
  /* Bible language codes (ISO 639-3: eng, spa, fra) match FLORES prefixes directly */
  var lang='eng';
  for(var i=0;i<bibleTranslations.length;i++){
    if(bibleTranslations[i].id===currentBibleTrans){
      lang=bibleTranslations[i].language||'eng';
      break;
    }
  }
  LOG('speakBibleVerseServer: sending requestTts lang='+lang);
  wsRef.send(JSON.stringify({type:'requestTts',text:text,language:lang}));
}

function readAllVerses(){
  LOG('readAllVerses called');
  var verseDivs=bibleContent.querySelectorAll('.bible-verse');
  LOG('readAllVerses: .bible-verse count='+verseDivs.length);
  if(verseDivs.length===0){
    verseDivs=bibleContent.querySelectorAll('.bible-search-text');
    LOG('readAllVerses: .bible-search-text count='+verseDivs.length);
  }
  /* Collect each verse as a separate string */
  var verses=[];
  for(var i=0;i<verseDivs.length;i++){
    var text='';
    var nodes=verseDivs[i].childNodes;
    for(var j=0;j<nodes.length;j++){
      if(nodes[j].nodeType===3){text+=nodes[j].textContent}
    }
    text=text.trim();
    if(text)verses.push(text);
  }
  LOG('readAllVerses: verses='+verses.length+' useServer='+useServerTts());
  if(verses.length===0){LOG('readAllVerses: no verses');return}

  clearTtsQueue();
  bibleTtsId++;
  synth.cancel();
  bibleTtsActive=true;

  if(useServerTts()){
    /* Join verses with sentence-ending pause markers for a single ordered TTS request */
    var allText='';
    for(var i=0;i<verses.length;i++){
      var v=verses[i];
      allText+=v;
      /* Ensure sentence-ending punctuation so TTS pauses between verses */
      if(v.length>0&&'.!?;:'.indexOf(v.charAt(v.length-1))===-1)allText+='.';
      allText+='\n';
    }
    speakBibleVerseServer(allText.trim());
  } else {
    /* Queue each verse as a separate utterance — browser TTS pauses between them */
    var myId=bibleTtsId;
    var voices=synth.getVoices();
    if(voices.length===0){
      var warm=new SpeechSynthesisUtterance('');
      synth.speak(warm);synth.cancel();
      setTimeout(function(){
        if(bibleTtsId!==myId)return;
        queueBibleUtterances(verses,myId);
      },250);
    } else {
      queueBibleUtterances(verses,myId);
    }
  }
}

function queueBibleUtterances(verses,myId){
  LOG('queueBibleUtterances: '+verses.length+' verses');
  for(var i=0;i<verses.length;i++){
    var utter=new SpeechSynthesisUtterance(verses[i]);
    utter.rate=speechRate;
    if(selectedVoice){var voices=synth.getVoices();for(var v=0;v<voices.length;v++){if(voices[v].name===selectedVoice){utter.voice=voices[v];break}}}
    if(i===verses.length-1){
      /* Last verse — hide stop when done */
      utter.onend=function(){LOG('readAll onend (last)');if(bibleTtsId===myId){bibleTtsActive=false}};
      utter.onerror=function(){LOG('readAll onerror (last)');if(bibleTtsId===myId){bibleTtsActive=false}};
    }
    synth.speak(utter);
  }
}

function addVerseSpeakBtn(div,text){
  var btn=document.createElement('button');
  btn.className='bible-verse-speak';
  btn.textContent='\u25B6';
  btn.title=t('readVerse');
  btn.onclick=function(e){
    e.stopPropagation();
    LOG('verseSpeakBtn clicked: "'+text.substring(0,40)+'"');
    if(useServerTts()){speakBibleVerseServer(text)}
    else{speakBibleVerse(text)}
  };
  div.appendChild(btn);
}

function addReadAllBtn(){
  var bar=document.createElement('div');bar.className='bible-read-all-bar';
  var btn=document.createElement('button');btn.className='bible-read-all-btn';
  btn.id='btnReadAll';
  btn.textContent=t('readAll');
  btn.onclick=function(){readAllVerses()};
  bar.appendChild(btn);
  var stopBtn=document.createElement('button');stopBtn.className='bible-stop-btn';
  stopBtn.textContent=t('stop');
  stopBtn.onclick=function(){stopBibleTts()};
  bar.appendChild(stopBtn);
  /* Verse translation toggle — only when the user's language differs from the Bible's */
  if(myTransLang&&bibleFloresLang()&&bibleFloresLang()!==myTransLang){
    var txBtn=document.createElement('button');txBtn.className='bible-read-all-btn';
    txBtn.id='btnBibleTx';
    txBtn.textContent=bibleTxOn?t('bibleOriginal'):t('bibleTranslate');
    txBtn.onclick=function(){toggleBibleTranslate()};
    bar.appendChild(txBtn);
  }
  bibleContent.insertBefore(bar,bibleContent.firstChild);
  if(bibleTxOn)translateVisibleVerses();
}

/* ── Bible Verse Translation (server /api/translate, NLLB) ── */
var bibleTxOn=false;
var bibleTxRun=0;
function bibleFloresLang(){
  /* FLORES code of the current Bible translation's language (ISO3 prefix match), or '' */
  var iso3='';
  for(var i=0;i<bibleTranslations.length;i++){
    if(bibleTranslations[i].id===currentBibleTrans){iso3=bibleTranslations[i].language||'';break}
  }
  if(!iso3)return '';
  for(var j=0;j<LANGS.length;j++){
    if(LANGS[j][0].substring(0,iso3.length+1)===iso3+'_')return LANGS[j][0];
  }
  return '';
}
function toggleBibleTranslate(){
  bibleTxOn=!bibleTxOn;
  bibleTxRun++;
  LOG('bibleTranslate toggle: '+bibleTxOn);
  var txBtn=document.getElementById('btnBibleTx');
  if(txBtn)txBtn.textContent=bibleTxOn?t('bibleOriginal'):t('bibleTranslate');
  if(bibleTxOn){translateVisibleVerses()}
  else{
    var done=bibleContent.querySelectorAll('.bible-verse-tx');
    for(var i=0;i<done.length;i++){done[i].parentNode.removeChild(done[i])}
  }
}
function translateVisibleVerses(){
  var src=bibleFloresLang();
  if(!src||!myTransLang||src===myTransLang)return;
  var divs=bibleContent.querySelectorAll('.bible-verse');
  if(divs.length===0)return;
  bibleTxRun++;
  var myRun=bibleTxRun;
  var idx=0;
  function next(){
    if(myRun!==bibleTxRun||!bibleTxOn)return;
    if(idx>=divs.length)return;
    var div=divs[idx];idx++;
    if(div.querySelector('.bible-verse-tx')){next();return}
    /* Verse text = the text nodes only (skips number span, buttons, tx divs) */
    var text='';
    var nodes=div.childNodes;
    for(var j=0;j<nodes.length;j++){if(nodes[j].nodeType===3){text+=nodes[j].textContent}}
    text=text.trim();
    if(!text){next();return}
    var xhr=new XMLHttpRequest();
    xhr.open('POST','/api/translate',true);
    xhr.setRequestHeader('Content-Type','application/json');
    xhr.onload=function(){
      if(myRun!==bibleTxRun||!bibleTxOn)return;
      if(xhr.status===200){
        try{
          var data=JSON.parse(xhr.responseText);
          if(data&&data.text){
            var tx=document.createElement('div');
            tx.className='bible-verse-tx';
            tx.style.cssText='color:#7fb3ff;font-style:italic;margin:2px 0 8px 18px';
            tx.textContent=data.text;
            div.appendChild(tx);
          }
        }catch(e){LOG('bibleTx parse error: '+e)}
      }else{LOG('bibleTx http '+xhr.status)}
      next();
    };
    xhr.onerror=function(){LOG('bibleTx network error');next()};
    xhr.send(JSON.stringify({text:text,sourceLang:src,targetLang:myTransLang}));
  }
  next();
}
function stopBibleTts(){
  LOG('stopBibleTts called');
  bibleTtsActive=false;
  bibleTtsId++;
  synth.cancel();
  clearTtsQueue();
  if(ttsAudio){ttsAudio.pause();ttsAudio.src=''}
  ;
}

/* ── Push-to-Talk for Conversation Rooms ── */
var pttActive=false;
var pttRecorder=null;
var pttChunks=[];
var pttRoomType='';
var roomSourceLang='auto';
var roomMaxSegSec=15;
var roomVadMs=800;
var roomBeamSize=7;
var roomPrompt='';
var roomSpeakers=[];
var roomActiveSpeakerId='';
var roomMode='online';
var roomAudioSource='local'; /* 'web' = the host broadcasts their browser mic into the room */
var roomWebMicRaw=false;     /* true = disable browser echo-cancel/NS/AGC (PA/soundboard feed) */
var roomDisplay=null; /* per-room display template: {bgColor,fgColor,fontFamily,fontBold,offeredLanguages,...} */

/* ── Web-mic broadcast (host streams continuous PCM to the room's engine) ──
   bcWant = user intent (survives WS reconnects, drives auto-resume);
   bcActive = capture pipeline actually running. */
var bcWant=false,bcActive=false;
var bcCtx=null,bcStream=null,bcNode=null,bcAnalyser=null,bcRaf=null;

function toggleBroadcast(){
  if(bcWant||bcActive){stopBroadcast(true)}
  else{
    bcWant=true;
    updateBroadcastUi('starting');
    if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'broadcastStart'}))}
    else{LOG('broadcastStart deferred — WS not open')}
  }
}

/* Server's answer to broadcastStart / a takeover / our stop ack. */
function handleBroadcastState(msg){
  if(msg.active){
    LOG('Broadcast granted by server');
    startBroadcastCapture();
  }else{
    if(msg.error==='taken-over'){
      LOG('Broadcast taken over by another device');
      showRoomError(t('bcTakenOver'));
      bcWant=false;
    }else if(msg.error){
      LOG('Broadcast rejected: '+msg.error);
      showRoomError(t('bcRejected'));
      bcWant=false;
    }
    stopBroadcastCapture();
    updateBroadcastUi('off');
  }
}

function startBroadcastCapture(){
  if(bcActive)return;
  if(!(navigator.mediaDevices&&navigator.mediaDevices.getUserMedia)||!window.AudioContext||!window.AudioWorkletNode){
    LOG('Broadcast unsupported (needs HTTPS + a modern browser)');
    showRoomError(t('bcUnsupported'));
    bcWant=false;updateBroadcastUi('off');
    return;
  }
  var proc=!roomWebMicRaw; /* processed capture for laptop/phone mics; raw for PA feeds */
  navigator.mediaDevices.getUserMedia({audio:{channelCount:1,echoCancellation:proc,noiseSuppression:proc,autoGainControl:proc}})
    .then(function(stream){
      if(!bcWant){stream.getTracks().forEach(function(tr){tr.stop()});return}
      bcStream=stream;
      bcCtx=new AudioContext();
      return bcCtx.audioWorklet.addModule('/js/mic-worklet.js?v=2.7.11').then(function(){
        var src=bcCtx.createMediaStreamSource(stream);
        bcNode=new AudioWorkletNode(bcCtx,'mic-downsampler');
        bcAnalyser=bcCtx.createAnalyser();
        bcAnalyser.fftSize=512;
        src.connect(bcAnalyser);
        src.connect(bcNode);
        /* A worklet with no downstream connection may be skipped by the graph —
           pull it via a muted gain so process() always runs. */
        var mute=bcCtx.createGain();
        mute.gain.value=0;
        bcNode.connect(mute);
        mute.connect(bcCtx.destination);
        /* Format header: magic ETMC, version 1, format 0 = pcm16/16k/mono */
        var hdr=new Uint8Array([0x45,0x54,0x4D,0x43,1,0,0,0]);
        if(wsRef&&wsRef.readyState===1){wsRef.send(hdr.buffer)}
        bcNode.port.onmessage=function(e){
          if(bcActive&&wsRef&&wsRef.readyState===1){wsRef.send(e.data)}
        };
        bcActive=true;
        updateBroadcastUi('on');
        startBcMeter();
        /* the room was waiting for THIS mic — reflect the handover instantly
           (the readiness notifier confirms 'ready' a few seconds later) */
        if(document.getElementById('rs-stt'))rsSetLine('stt',t('rsPreparing'));
        LOG('Broadcast capture running (device rate='+bcCtx.sampleRate+'Hz, processed='+proc+')');
      });
    })
    .catch(function(err){
      LOG('Broadcast mic failed: '+err);
      showRoomError(t('bcMicFailed'));
      bcWant=false;
      if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'broadcastStop'}))}
      stopBroadcastCapture();
      updateBroadcastUi('off');
    });
}

function stopBroadcast(tellServer){
  bcWant=false;
  if(tellServer&&wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'broadcastStop'}))}
  stopBroadcastCapture();
  updateBroadcastUi('off');
}

function stopBroadcastCapture(){
  bcActive=false;
  if(bcRaf){cancelAnimationFrame(bcRaf);bcRaf=null}
  if(bcNode){try{bcNode.port.onmessage=null;bcNode.disconnect()}catch(e){}bcNode=null}
  bcAnalyser=null;
  if(bcStream){try{bcStream.getTracks().forEach(function(tr){tr.stop()})}catch(e){}bcStream=null}
  if(bcCtx){try{bcCtx.close()}catch(e){}bcCtx=null}
}

function updateBroadcastUi(state){
  var btn=document.getElementById('hcBroadcast');
  if(!btn)return;
  if(state==='on'){btn.style.background='#e74c3c';btn.textContent='● '+t('bcStop')}
  else if(state==='starting'){btn.style.background='#e67e22';btn.textContent=t('bcStarting')}
  else{btn.style.background='#7c9cf7';btn.textContent='🎙 '+t('bcStart')}
  var meter=document.getElementById('hcBcMeter');
  if(meter&&state!=='on'){meter.style.width='0%'}
}

/* Level meter: the operator must SEE audio flowing — a dead mic that looks
   live is undebuggable mid-service. Runs only while the host panel is open. */
function startBcMeter(){
  var buf=null;
  function tick(){
    if(!bcActive||!bcAnalyser)return;
    var meter=document.getElementById('hcBcMeter');
    if(meter){
      if(!buf)buf=new Uint8Array(bcAnalyser.fftSize);
      bcAnalyser.getByteTimeDomainData(buf);
      var peak=0;
      for(var i=0;i<buf.length;i++){var d=Math.abs(buf[i]-128);if(d>peak)peak=d}
      var pct=Math.min(100,Math.round(peak/1.28));
      meter.style.width=pct+'%';
      meter.style.background=pct>90?'#e74c3c':(pct<5?'#e67e22':'#27ae60');
    }
    bcRaf=requestAnimationFrame(tick);
  }
  bcRaf=requestAnimationFrame(tick);
}

/* Language offered in this room? (no display template / empty list = all) */
function isLangOffered(code){
  if(!roomDisplay||!roomDisplay.offeredLanguages||roomDisplay.offeredLanguages.length===0)return true;
  for(var i=0;i<roomDisplay.offeredLanguages.length;i++){
    if(roomDisplay.offeredLanguages[i]===code)return true;
  }
  return false;
}

/* Apply the room's display template to the subtitle view.
   Deliberately does NOT touch fontSize — that stays a per-device preference. */
function applyRoomDisplay(){
  if(!roomDisplay)return;
  try{
    var cont=document.getElementById('container');
    var lns=document.getElementById('lines');
    if(roomDisplay.bgColor){
      document.body.style.background=roomDisplay.bgColor;
      if(cont)cont.style.background=roomDisplay.bgColor;
    }
    if(lns){
      if(roomDisplay.fgColor)lns.style.color=roomDisplay.fgColor;
      if(roomDisplay.fontFamily)lns.style.fontFamily=roomDisplay.fontFamily;
      if(roomDisplay.fontBold)lns.style.fontWeight='600';
    }
    LOG('applyRoomDisplay: bg='+roomDisplay.bgColor+' fg='+roomDisplay.fgColor+' offered='+(roomDisplay.offeredLanguages||[]).length);
  }catch(e){LOG('applyRoomDisplay error: '+e)}
}

function initPushToTalk(){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  if(document.getElementById('ptt-btn'))return; /* Already initialized */
  var roomId=roomMatch[1];
  /* Fetch room info — include clientId so server tells us if we're the host */
  var url='/api/rooms/'+encodeURIComponent(roomId);
  if(myClientId)url+='?clientId='+encodeURIComponent(myClientId);
  var xhr=new XMLHttpRequest();
  xhr.open('GET',url,true);
  xhr.onload=function(){
    if(xhr.status===404){
      /* Room no longer exists — clean up localStorage and redirect to lobby */
      try{
        var myRooms=JSON.parse(localStorage.getItem('myRooms')||'[]');
        myRooms=myRooms.filter(function(r){return r.id!==roomId});
        localStorage.setItem('myRooms',JSON.stringify(myRooms));
      }catch(e){}
      location.href='/lobby.html';
      return;
    }
    if(xhr.status===200){
      try{
        var room=JSON.parse(xhr.responseText);
        pttRoomType=room.type||'';
        pttMode=room.pttMode||'hold';
        roomSourceLang=room.sourceLang||'auto';
        roomMaxSegSec=room.maxSegmentSec||15;
        roomVadMs=room.vadSilenceMs||800;
        roomBeamSize=room.beamSize||7;
        roomPrompt=room.initialPrompt||'';
        roomSpeakers=room.speakers||[];
        roomActiveSpeakerId=room.activeSpeakerId||'';
        roomMode=room.mode||'online';
        roomAudioSource=room.audioSource||'local';
        roomWebMicRaw=!!room.webMicRaw;
        roomDisplay=room.display||null;
        applyRoomDisplay();
        if(roomDisplay&&roomDisplay.offeredLanguages&&roomDisplay.offeredLanguages.length>0){
          populateTransLangSelect();
          var lpOpen=document.getElementById('langPicker');
          if(lpOpen&&lpOpen.classList.contains('open'))renderLangList('');
        }
        if(room.isHost){isHost=true;showHostControls()}
        if(pttRoomType==='dictation'){initDictationView()}
        /* Conversation: everyone gets PTT. Conference: no mic (audio from desktop). */
        else if(pttRoomType==='conversation'){
          createPttButton();
          autoAssignDisplayName();
          initParticipantBar();
          loadRoomMembers();
        }else{
          /* Conference: no dock bar, reduce bottom padding */
          var cont=document.getElementById('container');
          if(cont)cont.style.paddingBottom='8px';
          var lines=document.getElementById('lines');
          if(lines)lines.style.paddingBottom='8px';
        }
      }catch(e){LOG('Room info parse error: '+e)}
    }
  };
  xhr.send();
}

function createPttButton(){
  /* Create a fixed bottom dock: text input at bottom, mic overlays right side */
  var dock=document.getElementById('roomControlsDock');
  if(!dock){
    dock=document.createElement('div');
    dock.id='roomControlsDock';
    dock.style.cssText='position:fixed;bottom:0;left:0;right:0;padding:6px 12px;background:#1a1a2e;border-top:1px solid #333;z-index:90;display:flex;flex-direction:column;align-items:center';
    document.body.appendChild(dock);
  }
  /* Hide dock while language picker is open */
  var lp=document.getElementById('langPicker');
  if(lp&&lp.classList.contains('open')){dock.style.display='none'}

  var label=document.createElement('div');
  label.id='ptt-label';
  label.style.cssText='display:none';
  label.textContent=pttMode==='toggle'?'Tap to speak':'Hold to speak';
  dock.appendChild(label);

  /* Row with text input and mic button overlapping on the right */
  var row=document.createElement('div');
  row.id='pttChatRow';
  row.style.cssText='display:flex;align-items:center;gap:8px;width:100%;max-width:400px';
  dock.appendChild(row);

  var btn=document.createElement('div');
  btn.id='ptt-btn';
  btn.style.cssText='width:48px;height:48px;min-width:48px;border-radius:50%;background:#7c9cf7;display:flex;align-items:center;justify-content:center;cursor:pointer;box-shadow:0 2px 8px rgba(0,0,0,0.4);user-select:none;-webkit-user-select:none;touch-action:none;transition:background 0.15s,transform 0.15s';
  btn.innerHTML='<svg width="24" height="24" viewBox="0 0 24 24" fill="white"><path d="M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm-1-9c0-.55.45-1 1-1s1 .45 1 1v6c0 .55-.45 1-1 1s-1-.45-1-1V5z"/><path d="M17 11c0 2.76-2.24 5-5 5s-5-2.24-5-5H5c0 3.53 2.61 6.43 6 6.92V21h2v-3.08c3.39-.49 6-3.39 6-6.92h-2z"/></svg>';
  row.appendChild(btn);

  /* Padding so scrolled content clears the fixed dock — recalculated dynamically */
  setTimeout(adjustDockPadding,50);

  /* Touch events for mobile */
  btn.addEventListener('touchstart',function(e){
    e.preventDefault();
    if(pttMode==='toggle'){if(!pttActive)startRecording(btn,label);else stopRecording(btn,label)}
    else{startRecording(btn,label)}
  },false);
  btn.addEventListener('touchend',function(e){
    e.preventDefault();
    if(pttMode!=='toggle')stopRecording(btn,label);
  },false);
  btn.addEventListener('touchcancel',function(e){e.preventDefault();cancelRecording(btn,label)},false);
  /* Mouse events for desktop testing */
  btn.addEventListener('mousedown',function(e){
    e.preventDefault();
    if(pttMode==='toggle'){if(!pttActive)startRecording(btn,label);else stopRecording(btn,label)}
    else{startRecording(btn,label)}
  },false);
  btn.addEventListener('mouseup',function(e){
    e.preventDefault();
    if(pttMode!=='toggle')stopRecording(btn,label);
  },false);
  btn.addEventListener('mouseleave',function(e){if(pttActive&&pttMode!=='toggle')cancelRecording(btn,label)},false);

  /* If a readiness 'preparing' message already arrived before the button existed, reflect it now */
  setPttEnabled(_sttReady);

  /* Insert text chat area at the bottom of the dock (below PTT) */
  initTextChat();
}

/* ── Engine-readiness indicator (transient: shown while loading, removed once ready) ── */
var _sttReady=true;            /* mic gating — disabled while the speech engine loads */
var _sttSafetyTimer=null;      /* fail-safe: never leave the mic permanently disabled */

function rsEl(){
  var e=document.getElementById('roomStatusInd');
  if(!e){
    e=document.createElement('div');
    e.id='roomStatusInd';
    e.style.cssText='position:fixed;top:46px;left:50%;transform:translateX(-50%);background:#2a2a44;color:#fff;padding:7px 14px;border-radius:8px;font-size:13px;line-height:1.5;z-index:200;box-shadow:0 2px 10px rgba(0,0,0,.45);text-align:center;max-width:90%;transition:opacity .3s';
    document.body.appendChild(e);
  }
  return e;
}
function rsSetLine(scope,text){
  var e=rsEl();
  var line=document.getElementById('rs-'+scope);
  if(!line){line=document.createElement('div');line.id='rs-'+scope;e.appendChild(line)}
  line.textContent=text;
  e.style.opacity='1';
}
function rsRemoveLine(scope){
  var line=document.getElementById('rs-'+scope);
  if(line&&line.parentNode)line.parentNode.removeChild(line);
  var e=document.getElementById('roomStatusInd');
  if(e&&!e.firstChild){
    e.style.opacity='0';
    setTimeout(function(){if(e&&!e.firstChild&&e.parentNode)e.parentNode.removeChild(e)},350);
  }
}
function setPttEnabled(on){
  var btn=document.getElementById('ptt-btn');
  if(btn){btn.style.opacity=on?'1':'0.4';btn.style.pointerEvents=on?'auto':'none'}
}
function handleRoomStatus(msg){
  var scope=msg.scope||'stt',state=msg.state||'';
  if(scope==='stt'){
    if(state==='preparing'){
      _sttReady=false;setPttEnabled(false);
      /* Web-mic rooms aren't "loading" — they're honestly waiting for a broadcaster.
         Tell the host it's THEIR move; tell listeners what's happening. */
      if(roomAudioSource==='web'&&!bcActive){
        rsSetLine('stt',isHost?t('rsWaitMicHost'):t('rsWaitMic'));
      }else{
        rsSetLine('stt',t('rsPreparing'));
      }
      if(_sttSafetyTimer)clearTimeout(_sttSafetyTimer);
      /* If a 'ready' message is ever lost, re-enable after 90s so the mic can't be trapped */
      _sttSafetyTimer=setTimeout(function(){_sttReady=true;setPttEnabled(true);rsRemoveLine('stt')},90000);
    }else if(state==='ready'){
      _sttReady=true;setPttEnabled(true);
      if(_sttSafetyTimer){clearTimeout(_sttSafetyTimer);_sttSafetyTimer=null}
      rsSetLine('stt',t('rsReady'));
      setTimeout(function(){rsRemoveLine('stt')},1500);
    }
  }else if(scope==='translation'){
    if(state==='preparing')rsSetLine('trans',t('rsTransWarming'));
    else if(state==='ready')rsRemoveLine('trans');
  }
}

function startRecording(btn,label){
  if(!_sttReady){if(label)label.textContent=t('rsPreparing');return}
  if(pttActive)return;
  if(!navigator.mediaDevices||!navigator.mediaDevices.getUserMedia){
    label.textContent=t('micUnavailable');
    return;
  }
  pttActive=true;
  pttChunks=[];
  btn.style.background='#e74c3c';
  btn.style.transform='scale(1.15)';
  btn.classList.add('ptt-recording');
  label.textContent=t('recording');
  localRecording=true;updateSpeakingUI();
  if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'startSpeaking'}))}

  navigator.mediaDevices.getUserMedia({audio:{sampleRate:16000,channelCount:1,echoCancellation:true,noiseSuppression:true}})
    .then(function(stream){
      /* Use audio/webm if available, else audio/ogg, else browser default */
      var mimeType='';
      if(typeof MediaRecorder.isTypeSupported==='function'){
        if(MediaRecorder.isTypeSupported('audio/webm;codecs=opus'))mimeType='audio/webm;codecs=opus';
        else if(MediaRecorder.isTypeSupported('audio/webm'))mimeType='audio/webm';
        else if(MediaRecorder.isTypeSupported('audio/ogg;codecs=opus'))mimeType='audio/ogg;codecs=opus';
        else if(MediaRecorder.isTypeSupported('audio/ogg'))mimeType='audio/ogg';
      }
      LOG('PTT: using mimeType='+(mimeType||'(default)'));
      var opts=mimeType?{mimeType:mimeType}:{};
      pttRecorder=new MediaRecorder(stream,opts);
      pttRecorder.ondataavailable=function(e){
        if(e.data&&e.data.size>0)pttChunks.push(e.data);
      };
      pttRecorder.onstop=function(){
        /* Stop all tracks to release mic */
        stream.getTracks().forEach(function(t){t.stop()});
        if(pttChunks.length>0){
          sendAudioToServer();
        }
      };
      pttRecorder.start(); /* no timeslice — produces one complete WebM file on stop */
    })
    .catch(function(err){
      LOG('Mic access denied: '+err);
      pttActive=false;
      btn.style.background='#7c9cf7';
      btn.style.transform='';
      label.textContent=t('micDenied');
      var dl=pttMode==='toggle'?'Tap to speak':'Hold to speak';
      setTimeout(function(){label.textContent=dl},2000);
    });
}

function stopRecording(btn,label){
  if(!pttActive)return;
  pttActive=false;
  btn.style.background='#7c9cf7';
  btn.style.transform='';
  btn.classList.remove('ptt-recording');
  label.textContent=t('sending');
  localRecording=false;updateSpeakingUI();
  if(pttRecorder&&pttRecorder.state==='recording'){
    pttRecorder.stop();
  }
  var defaultLabel=pttMode==='toggle'?'Tap to speak':'Hold to speak';
  setTimeout(function(){label.textContent=defaultLabel},2000);
}

function cancelRecording(btn,label){
  pttActive=false;
  pttChunks=[];
  btn.style.background='#7c9cf7';
  btn.style.transform='';
  btn.classList.remove('ptt-recording');
  label.textContent=pttMode==='toggle'?'Tap to speak':'Hold to speak';
  localRecording=false;updateSpeakingUI();
  if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'stopSpeaking'}))}
  if(pttRecorder&&pttRecorder.state==='recording'){
    pttRecorder.ondataavailable=null;
    pttRecorder.onstop=function(){
      try{pttRecorder.stream.getTracks().forEach(function(t){t.stop()})}catch(e){}
    };
    pttRecorder.stop();
  }
}

function sendAudioToServer(){
  if(!wsRef||wsRef.readyState!==1){LOG('WS not connected for PTT');return}
  /* Send WebM/Opus directly — server converts with FFmpeg (smaller, saves phone CPU) */
  var blob=new Blob(pttChunks,{type:pttChunks[0].type||'audio/webm'});
  pttChunks=[];
  blob.arrayBuffer().then(function(buf){
    wsRef.send(buf);
    LOG('PTT: sent '+buf.byteLength+' bytes ('+blob.type+')');
  });
}

/* PTT init is now triggered by the 'welcome' WebSocket message (needs client ID for host check) */

/* ── Dictation view: private room rendered as an editor ─────────────────
   The mic is the existing web-mic broadcast (same button id the host panel
   uses, so toggleBroadcast/updateBroadcastUi/startBcMeter drive it as-is).
   Translation is the normal per-client room pipeline: pick a language on
   the picker (or 'No translation') and commits arrive accordingly. */
function initDictationView(){
  if(document.getElementById('dictWrap'))return;
  var cont=document.getElementById('container');
  if(cont)cont.style.display='none';
  var w=document.createElement('div');
  w.id='dictWrap';
  w.style.cssText='position:fixed;top:40px;left:0;right:0;bottom:0;display:flex;flex-direction:column;padding:12px;background:#12122a;z-index:50';
  w.innerHTML=
    '<div style="display:flex;gap:8px;align-items:center;margin-bottom:8px">'+
      '<button id="hcBroadcast" style="flex:1;padding:12px;border:none;border-radius:8px;background:#7c9cf7;color:#fff;font-size:15px;font-weight:600;cursor:pointer">🎙 '+t('bcStart')+'</button>'+
      '<button id="dictCopy" style="padding:12px 16px;border:1px solid #555;border-radius:8px;background:#252540;color:#ccc;font-size:14px;cursor:pointer">'+t('dictCopy')+'</button>'+
      '<button id="dictClear" style="padding:12px 16px;border:1px solid #555;border-radius:8px;background:#252540;color:#ccc;font-size:14px;cursor:pointer">'+t('clear')+'</button>'+
      '<button id="dictDone" style="padding:12px 16px;border:none;border-radius:8px;background:#e74c3c;color:#fff;font-size:14px;cursor:pointer">'+t('dictDone')+'</button>'+
    '</div>'+
    '<div style="display:flex;gap:8px;align-items:center;margin-bottom:8px">'+
      '<label for="dictOutLang" style="color:#888;font-size:13px;white-space:nowrap">'+t('dictOutLang')+'</label>'+
      '<select id="dictOutLang" style="flex:1;padding:8px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:14px"></select>'+
    '</div>'+
    '<div id="hcBcMeterWrap" style="height:8px;background:#333;border-radius:4px;margin-bottom:8px;overflow:hidden"><div id="hcBcMeter" style="height:100%;width:0%;background:#27ae60;transition:width 0.1s"></div></div>'+
    '<textarea id="dictText" spellcheck="false" style="flex:1;width:100%;box-sizing:border-box;resize:none;padding:14px;border-radius:10px;border:1px solid #444;background:#1a1a2e;color:#fff;font-size:17px;line-height:1.6;outline:none"></textarea>';
  document.body.appendChild(w);
  populateDictOutLang();
  document.getElementById('dictOutLang').addEventListener('change',function(){setTransLang(this.value)});
  document.getElementById('hcBroadcast').addEventListener('click',toggleBroadcast);
  document.getElementById('dictCopy').addEventListener('click',function(){
    var ta=document.getElementById('dictText');
    ta.select();
    if(navigator.clipboard&&navigator.clipboard.writeText){
      navigator.clipboard.writeText(ta.value).then(function(){flashDictBtn('dictCopy',t('dictCopied'))});
    }else{
      try{document.execCommand('copy');flashDictBtn('dictCopy',t('dictCopied'))}catch(e){}
    }
  });
  document.getElementById('dictClear').addEventListener('click',function(){
    document.getElementById('dictText').value='';
  });
  document.getElementById('dictDone').addEventListener('click',function(){
    stopBroadcast(true);
    var roomMatch=location.search.match(/[?&]room=([^&]+)/);
    if(roomMatch){
      var xhr=new XMLHttpRequest();
      xhr.open('DELETE','/api/rooms/'+encodeURIComponent(roomMatch[1])+'?clientId='+encodeURIComponent(myClientId),true);
      xhr.onload=function(){location.href='/lobby.html'};
      xhr.onerror=function(){location.href='/lobby.html'};
      xhr.send();
    }else{location.href='/lobby.html'}
  });
}
/* Dictation output-language dropdown — same source as the room picker
   (LANGS from /api/languages), '' = raw transcript in the spoken language.
   The equivalent of the desktop tray dictation's output-language submenu. */
function populateDictOutLang(){
  var sel=document.getElementById('dictOutLang');
  if(!sel)return;
  sel.innerHTML='';
  var raw=document.createElement('option');
  raw.value='';
  raw.textContent=t('noTranslation');
  sel.appendChild(raw);
  for(var i=0;i<LANGS.length;i++){
    var opt=document.createElement('option');
    opt.value=LANGS[i][0];
    opt.textContent=LANGS[i][1]+' ('+LANGS[i][2]+')';
    sel.appendChild(opt);
  }
  sel.value=myTransLang||'';
  if(sel.selectedIndex<0)sel.selectedIndex=0;
}
function flashDictBtn(id,txt){
  var b=document.getElementById(id);
  if(!b)return;
  var old=b.textContent;
  b.textContent=txt;
  setTimeout(function(){b.textContent=old},1200);
}

/* ── Room Governance ── */
var isHost=false;
var pttMode='hold';
var roomMembers=[];
var virtualMembers=[];
var participantBarExpanded=false;
var activeVirtualMemberId='';
var transcriptCache=[];

/* Speaker colour palette — distinct, readable on dark backgrounds */
var speakerColors=['#5bc0eb','#fde74c','#9bc53d','#e55934','#c77dff','#fa7921','#2ec4b6','#ff6b6b','#48bfe3','#f4a261','#80ed99','#e0aaff'];
var speakerColorMap={};
var nextColorIndex=0;
function getSpeakerColor(name){
  if(!name)return '';
  if(speakerColorMap[name])return speakerColorMap[name];
  speakerColorMap[name]=speakerColors[nextColorIndex%speakerColors.length];
  nextColorIndex++;
  return speakerColorMap[name];
}

/* Error/notification display for room events */
function showRoomError(msg){
  var el=document.getElementById('roomNotification');
  if(!el){
    el=document.createElement('div');
    el.id='roomNotification';
    el.style.cssText='position:fixed;top:50%;left:50%;transform:translate(-50%,-50%);background:rgba(0,0,0,0.9);color:#fff;padding:24px 32px;border-radius:12px;font-size:16px;z-index:999;text-align:center';
    document.body.appendChild(el);
  }
  el.textContent=msg;
  el.style.display='block';
}

/* Auto-reclaim host via stored token, or fall back to admin PIN.
   Retries a few times: right after a page refresh the server may still list the
   PREVIOUS connection as the live host until its socket is reaped, so the first
   claim can be rejected even though we hold the valid token. */
function tryClaimHost(attempt){
  attempt=attempt||0;
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch||!myClientId)return;
  var roomId=roomMatch[1];
  try{
    var myRooms=JSON.parse(localStorage.getItem('myRooms')||'[]');
    var mine=null;
    for(var i=0;i<myRooms.length;i++){if(myRooms[i].id===roomId){mine=myRooms[i];break}}
    var token=(mine&&mine.hostToken)?mine.hostToken:'';
    var pin=sessionStorage.getItem('adminPin')||'';
    if(!token&&!pin)return;
    var retry=function(){
      if(attempt<4&&!isHost){
        LOG('Host claim not accepted yet, retrying ('+(attempt+1)+'/4)');
        setTimeout(function(){tryClaimHost(attempt+1)},2500);
      }
    };
    var xhr=new XMLHttpRequest();
    xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/claim-host',true);
    xhr.setRequestHeader('Content-Type','application/json');
    xhr.onload=function(){
      var claimed=false;
      if(xhr.status===200){
        try{
          var res=JSON.parse(xhr.responseText);
          if(res.ok){claimed=true;isHost=true;LOG('Host reclaimed');showHostControls()}
        }catch(e){}
      }
      if(!claimed)retry();
    };
    xhr.onerror=retry;
    var body={clientId:myClientId};
    if(token)body.hostToken=token;
    if(pin)body.pin=pin;
    xhr.send(JSON.stringify(body));
  }catch(e){LOG('tryClaimHost error: '+e)}
}

/* Display name: reuse stored name, or generate "GuestNNNN" on first join */
function autoAssignDisplayName(){
  var stored=ss('displayName')||'';
  var name=stored||('Guest'+Math.floor(1000+Math.random()*9000));
  if(!stored)ssSet('displayName',name);
  if(wsRef&&wsRef.readyState===1){
    wsRef.send(JSON.stringify({type:'setDisplayName',name:name}));
  }
  /* Populate the settings field */
  var inp=document.getElementById('displayNameInput');
  if(inp)inp.value=name;
  /* Delay member load to give server time to process the name */
  setTimeout(loadRoomMembers,500);
}

function saveDisplayName(val){
  var name=val.trim();
  if(!name)return;
  ssSet('displayName',name);
  if(wsRef&&wsRef.readyState===1){
    wsRef.send(JSON.stringify({type:'setDisplayName',name:name}));
  }
}

/* Participant bar — uses the static HTML element, just show/hide */
function initParticipantBar(){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  var bar=document.getElementById('participantBar');
  var details=document.getElementById('participantDetails');
  if(!bar)return;
  bar.style.display='flex';

  bar.addEventListener('click',function(e){
    e.stopPropagation();
    participantBarExpanded=!participantBarExpanded;
    details.style.display=participantBarExpanded?'block':'none';
    document.getElementById('participantBarArrow').innerHTML=participantBarExpanded?'&#9650;':'&#9660;';
  });
  /* Event delegation for kick/remove buttons inside details */
  details.addEventListener('click',function(e){
    e.stopPropagation();
    var target=e.target;
    if(target.classList.contains('kick-btn')){
      e.preventDefault();
      kickMember(target.dataset.id);
    }else if(target.classList.contains('vm-kick-btn')){
      e.preventDefault();
      removeVirtualMember(target.dataset.id);
    }
  });
  updateParticipantBar();
}

function updateParticipantBar(){
  var title=document.getElementById('participantBarTitle');
  if(!title)return;
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  var roomName='Room';
  var total=roomMembers.length+virtualMembers.length;
  title.textContent=roomName+' ('+total+')';
  renderParticipantDetails();
}

function renderParticipantDetails(){
  var details=document.getElementById('participantDetails');
  if(!details)return;
  details.innerHTML='';
  /* Real members */
  for(var i=0;i<roomMembers.length;i++){
    var m=roomMembers[i];
    var chip=document.createElement('div');
    chip.style.cssText='display:flex;align-items:center;justify-content:space-between;padding:6px 0;border-bottom:1px solid #333';
    var nameSpan='<span style="color:#fff">'+escHtml(dispName(m.displayName)||t('guestLabel'))+'</span>';
    var kick='';
    if(isHost&&m.clientId!==myClientId){
      kick='<span class="kick-btn" data-id="'+m.clientId+'" style="color:#e74c3c;cursor:pointer;font-size:14px;padding:6px 12px;border:1px solid #e74c3c;border-radius:6px;min-width:32px;text-align:center">x</span>';
    }
    chip.innerHTML=nameSpan+kick;
    details.appendChild(chip);
  }
  /* Virtual members */
  for(var j=0;j<virtualMembers.length;j++){
    var vm=virtualMembers[j];
    var vChip=document.createElement('div');
    vChip.style.cssText='display:flex;align-items:center;justify-content:space-between;padding:6px 0;border-bottom:1px solid #333';
    var vName='<span style="color:#aaa;font-style:italic">'+escHtml(dispName(vm.name)||t('guestLabel'))+' '+escHtml(t('vmShared'))+'</span>';
    var vKick='';
    if(isHost){
      vKick='<span class="vm-kick-btn" data-id="'+vm.id+'" style="color:#e74c3c;cursor:pointer;font-size:14px;padding:6px 12px;border:1px solid #e74c3c;border-radius:6px;min-width:32px;text-align:center">x</span>';
    }
    vChip.innerHTML=vName+vKick;
    details.appendChild(vChip);
  }
}

function escHtml(s){return s?s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'):''}

function loadRoomMembers(){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  var xhr=new XMLHttpRequest();
  xhr.open('GET','/api/rooms/'+encodeURIComponent(roomMatch[1])+'/members',true);
  xhr.onload=function(){
    if(xhr.status===200){
      try{
        var data=JSON.parse(xhr.responseText);
        roomMembers=[];virtualMembers=[];
        for(var i=0;i<data.length;i++){
          if(data[i]['virtual']){
            /* Normalize to {id, name, language} format expected by rendering code */
            virtualMembers.push({id:data[i].clientId,name:data[i].displayName||'Guest',language:data[i].language||''});
          } else {
            roomMembers.push(data[i]);
          }
        }
        updateParticipantBar();
        updateVirtualMemberPtt();
      }catch(e){}
    }
  };
  xhr.send();
}

function addRoomMember(msg){
  roomMembers.push({clientId:msg.clientId,displayName:msg.displayName||'Guest',language:msg.language||''});
  updateParticipantBar();
}
function removeRoomMember(clientId){
  roomMembers=roomMembers.filter(function(m){return m.clientId!==clientId});
  updateParticipantBar();
}
function updateRoomMember(msg){
  for(var i=0;i<roomMembers.length;i++){
    if(roomMembers[i].clientId===msg.clientId){
      roomMembers[i].displayName=msg.displayName||'Guest';
      break;
    }
  }
  updateParticipantBar();
}
function addVirtualMemberToRoom(msg){
  virtualMembers.push({id:msg.id,name:msg.name,language:msg.language||''});
  updateParticipantBar();
  updateVirtualMemberPtt();
}
function removeVirtualMemberFromRoom(vmId){
  virtualMembers=virtualMembers.filter(function(vm){return vm.id!==vmId});
  if(activeVirtualMemberId===vmId)activeVirtualMemberId='';
  updateParticipantBar();
  updateVirtualMemberPtt();
}

/* ── Speaking indicator ── */
var activeSpeakers={};
var speakingTimers={};
var localRecording=false;

function handleSpeakingIndicator(msg){
  var cid=msg.clientId||'';
  LOG('speaking indicator: cid='+cid+' active='+msg.active+' name='+(msg.displayName||'?')+' myId='+myClientId);
  if(!cid)return;
  if(msg.active){
    activeSpeakers[cid]=dispName(msg.displayName)||t('guestLabel');
    if(speakingTimers[cid])clearTimeout(speakingTimers[cid]);
    speakingTimers[cid]=setTimeout(function(){clearSpeaker(cid);updateSpeakingUI()},30000);
  }else{
    clearSpeaker(cid);
  }
  updateSpeakingUI();
}

function clearSpeaker(cid){
  delete activeSpeakers[cid];
  if(speakingTimers[cid]){clearTimeout(speakingTimers[cid]);delete speakingTimers[cid]}
}
function clearSpeakerByName(name){
  for(var cid in activeSpeakers){
    if(activeSpeakers.hasOwnProperty(cid)&&activeSpeakers[cid]===name){clearSpeaker(cid)}
  }
}

function updateSpeakingUI(){
  var banner=document.getElementById('speakingBanner');
  var nameEl=document.getElementById('speakingName');
  if(!banner||!nameEl)return;
  var otherNames=[];
  for(var cid in activeSpeakers){
    if(activeSpeakers.hasOwnProperty(cid)&&cid!==myClientId){
      otherNames.push(activeSpeakers[cid]);
    }
  }
  if(localRecording){
    nameEl.textContent=t('recording');
    banner.style.display='flex';
    banner.style.borderBottomColor='#e74c3c';
  }else if(otherNames.length>0){
    nameEl.textContent=otherNames.join(', ')+' speaking...';
    banner.style.display='flex';
    banner.style.borderBottomColor='#e74c3c';
  }else{
    banner.style.display='none';
  }
}

/* Host controls */
function showHostControls(){
  /* If the waiting-for-mic banner rendered before host status arrived,
     upgrade the listener wording to the host call-to-action. */
  if(roomAudioSource==='web'&&!bcActive&&document.getElementById('rs-stt')){
    rsSetLine('stt',t('rsWaitMicHost'));
  }
  if(document.getElementById('hostGearBtn'))return;
  var btn=document.createElement('button');
  btn.id='hostGearBtn';
  btn.title=t('hostControls');
  btn.innerHTML='<img src="/img/icon.png" style="height:20px;width:20px;border-radius:50%;vertical-align:middle">';
  btn.addEventListener('click',toggleHostPanel);
  var toolbar=document.getElementById('toolbar');
  if(toolbar)toolbar.appendChild(btn);
}

function toggleHostPanel(){
  var panel=document.getElementById('hostPanel');
  if(panel){panel.remove();return}
  closeAllPanels();
  panel=document.createElement('div');
  panel.id='hostPanel';
  panel.style.cssText='position:fixed;top:50px;right:8px;background:#1e1e3a;border:1px solid #444;border-radius:10px;padding:12px;z-index:200;width:240px;max-height:calc(100vh - 60px);overflow-y:auto;box-shadow:0 4px 20px rgba(0,0,0,0.6)';
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  var roomId=roomMatch?roomMatch[1]:'';
  var hostHtml=
    '<div style="color:#fff;font-weight:600;margin-bottom:12px">'+t('hostControls')+'</div>'+
    '<button id="hcEndRoom" style="width:100%;padding:10px;border:none;border-radius:8px;background:#e74c3c;color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:8px">'+t('endRoom')+'</button>'+
    '<button id="hcClear" style="width:100%;padding:10px;border:none;border-radius:8px;background:#555;color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:8px">✕ '+t('clearCaptions')+'</button>'+
    '<button id="hcAdmin" style="width:100%;padding:10px;border:1px solid #7c9cf7;border-radius:8px;background:transparent;color:#7c9cf7;font-size:14px;cursor:pointer;margin-bottom:8px">⚙ '+t('hostAdmin')+'</button>';
  if(pttRoomType==='conference'){
    var isPaused=window._roomPaused||false;
    hostHtml+='<button id="hcPauseBtn" style="width:100%;padding:10px;border:none;border-radius:8px;background:'+(isPaused?'#e74c3c':'#27ae60')+';color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:8px">'+(isPaused?'\u23F8 Paused':'\u25B6 Playing')+'</button>';
    if(roomAudioSource==='web'){
      /* Web-mic room: THIS device is the microphone. Button + live level meter. */
      hostHtml+='<button id="hcBroadcast" style="width:100%;padding:10px;border:none;border-radius:8px;background:'+(bcActive?'#e74c3c':'#7c9cf7')+';color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:4px">'+(bcActive?'\u25CF '+t('bcStop'):'\uD83C\uDF99 '+t('bcStart'))+'</button>'+
        '<div id="hcBcMeterWrap" style="height:8px;background:#333;border-radius:4px;margin-bottom:8px;overflow:hidden"><div id="hcBcMeter" style="height:100%;width:0%;background:#27ae60;transition:width 0.1s"></div></div>';
    }
  }
  if(pttRoomType!=='conference'){
    hostHtml+=
      '<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px"><span style="color:#ccc;font-size:13px">Lock Room</span><div id="hcLockToggle" class="hc-toggle" style="width:40px;height:22px;background:#444;border-radius:11px;position:relative;cursor:pointer;transition:background 0.2s"><div style="width:18px;height:18px;background:#fff;border-radius:50%;position:absolute;top:2px;left:2px;transition:transform 0.2s"></div></div></div>'+
      '<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px"><span style="color:#ccc;font-size:13px">PTT: Tap to toggle</span><div id="hcPttToggle" class="hc-toggle" style="width:40px;height:22px;background:#444;border-radius:11px;position:relative;cursor:pointer;transition:background 0.2s"><div style="width:18px;height:18px;background:#fff;border-radius:50%;position:absolute;top:2px;left:2px;transition:transform 0.2s"></div></div></div>'+
      '<button id="hcAddGuest" style="width:100%;padding:10px;border:1px solid #7c9cf7;border-radius:8px;background:transparent;color:#7c9cf7;font-size:14px;cursor:pointer">+ '+t('addGuest')+'</button>';
  }
  panel.innerHTML=hostHtml;

  /* Pipeline controls for conference rooms */
  if(pttRoomType==='conference'){
    /* Speaker selector — only when the room's template defines speakers */
    var spkHtml='';
    if(roomSpeakers.length>0){
      spkHtml='<label style="color:#888;font-size:11px">'+t('hostSpeaker')+'</label>'+
        '<select id="hcSpeaker" style="width:100%;padding:6px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:13px;margin-bottom:8px;box-sizing:border-box">'+
        '<option value="">'+t('hostSpeakerDefault')+'</option>';
      for(var si=0;si<roomSpeakers.length;si++){
        var sp=roomSpeakers[si];
        spkHtml+='<option value="'+sp.id+'"'+(sp.id===roomActiveSpeakerId?' selected':'')+'>'+sp.name+'</option>';
      }
      spkHtml+='</select>';
    }
    var modeHtml='<label style="color:#888;font-size:11px">'+t('hostMode')+'</label>'+
      '<select id="hcMode" style="width:100%;padding:6px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:13px;margin-bottom:8px;box-sizing:border-box">'+
      '<option value="online"'+(roomMode!=='offline'?' selected':'')+'>'+t('hostModeOnline')+'</option>'+
      '<option value="offline"'+(roomMode==='offline'?' selected':'')+'>'+t('hostModeOffline')+'</option>'+
      '</select>';
    var pipeHtml='<div style="border-top:1px solid #444;margin-top:12px;padding-top:12px">'+
      '<div style="color:#aaa;font-size:12px;font-weight:600;margin-bottom:8px">Pipeline</div>'+
      spkHtml+modeHtml+
      '<label style="color:#888;font-size:11px">'+t('hostSpeakerLang')+'</label>'+
      '<select id="hcPipeLang" style="width:100%;padding:6px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:13px;margin-bottom:8px;box-sizing:border-box">'+
      '<option value="auto">'+t('autoDetect')+'</option>'+
      '<option value="ca">Catalan</option><option value="es">Spanish</option><option value="en">English</option>'+
      '<option value="fr">French</option><option value="de">German</option><option value="it">Italian</option>'+
      '<option value="pt">Portuguese</option><option value="nl">Dutch</option><option value="ru">Russian</option>'+
      '<option value="zh">Chinese</option><option value="ja">Japanese</option><option value="ko">Korean</option>'+
      '<option value="ar">Arabic</option></select>'+
      '<button id="hcPipeApply" style="width:100%;padding:8px;border:none;border-radius:8px;background:#7c9cf7;color:#1a1a2e;font-size:13px;font-weight:600;cursor:pointer">'+t('hostApply')+'</button>'+
      '<button id="hcPipeReset" style="width:100%;padding:8px;border:none;border-radius:8px;background:#e67e22;color:#fff;font-size:13px;font-weight:600;cursor:pointer;margin-top:8px">\u21BB '+t('pipeReset')+'</button>'+
      '<div id="hcPipeStatus" style="color:#888;font-size:11px;margin-top:4px;text-align:center"></div>'+
      '</div>';
    panel.innerHTML+=pipeHtml;
  }

  document.body.appendChild(panel);

  var _hcAdm=document.getElementById('hcAdmin');
  if(_hcAdm)_hcAdm.addEventListener('click',function(){window.open('/admin.html','_blank')});

  /* Pre-select pipeline values from room state */
  var pipeLang=document.getElementById('hcPipeLang');
  if(pipeLang&&roomSourceLang){pipeLang.value=roomSourceLang}

  /* Replace the hardcoded fallback options with the ACTIVE engine's real
     language list (/api/stt-languages) — one source of truth, no drift.
     Shows native names; keeps "auto" first and the current selection. */
  populateSttLangs(pipeLang,true);

  document.getElementById('hcEndRoom').addEventListener('click',function(){
    var xhr=new XMLHttpRequest();
    xhr.open('DELETE','/api/rooms/'+encodeURIComponent(roomId)+'?clientId='+encodeURIComponent(myClientId),true);
    xhr.onload=function(){location.href='/lobby.html'};
    xhr.send();
  });

  var clearBtn=document.getElementById('hcClear');
  if(clearBtn){
    clearBtn.addEventListener('click',function(){
      /* Host clears every listener's captions in this room back to empty */
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/clear',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.send(JSON.stringify({requestingClientId:myClientId}));
    });
  }

  var pauseBtn=document.getElementById('hcPauseBtn');
  if(pauseBtn){
    pauseBtn.addEventListener('click',function(){
      var newPaused=!window._roomPaused;
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/pause',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.send(JSON.stringify({paused:newPaused,requestingClientId:myClientId}));
    });
  }

  var bcBtn=document.getElementById('hcBroadcast');
  if(bcBtn){bcBtn.addEventListener('click',toggleBroadcast)}

  var lockToggle=document.getElementById('hcLockToggle');
  if(lockToggle){
    lockToggle.addEventListener('click',function(){
      var isOn=this.style.background==='rgb(124, 156, 247)';
      var newVal=!isOn;
      this.style.background=newVal?'#7c9cf7':'#444';
      this.children[0].style.transform=newVal?'translateX(18px)':'translateX(0)';
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/lock',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.send(JSON.stringify({locked:newVal,requestingClientId:myClientId}));
    });
  }

  var pttToggle=document.getElementById('hcPttToggle');
  if(pttToggle){
    if(pttMode==='toggle'){pttToggle.style.background='#7c9cf7';pttToggle.children[0].style.transform='translateX(18px)'}
    pttToggle.addEventListener('click',function(){
      var isOn=this.style.background==='rgb(124, 156, 247)';
      var newMode=isOn?'hold':'toggle';
      this.style.background=isOn?'#444':'#7c9cf7';
      this.children[0].style.transform=isOn?'translateX(0)':'translateX(18px)';
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/ptt-mode',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.send(JSON.stringify({mode:newMode,requestingClientId:myClientId}));
    });
  }

  var hcAddGuest=document.getElementById('hcAddGuest');
  if(hcAddGuest){
    hcAddGuest.addEventListener('click',function(){
      panel.remove();
      showAddGuestPrompt(roomId);
    });
  }

  /* Pipeline control events (conference only) */
  if(pttRoomType==='conference'){

    var hcReset=document.getElementById('hcPipeReset');
    if(hcReset)hcReset.addEventListener('click',function(){
      var st=document.getElementById('hcPipeStatus');
      if(st){st.textContent=t('pipeResetting');st.style.color='#e67e22'}
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/control/pipeline/reset',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.onload=function(){
        try{
          var res=JSON.parse(xhr.responseText);
          if(res.ok){if(st){st.textContent=t('pipeResetOk');st.style.color='#4f4'}}
          else{if(st){st.textContent=res.error||t('resetFailed');st.style.color='#f44'}}
        }catch(e){if(st){st.textContent='Error';st.style.color='#f44'}}
      };
      xhr.onerror=function(){if(st){st.textContent=t('netError');st.style.color='#f44'}};
      xhr.send(JSON.stringify({roomId:roomId,clientId:myClientId}));
    });

    var hcApply=document.getElementById('hcPipeApply');
    if(hcApply)hcApply.addEventListener('click',function(){
      var params={roomId:roomId,clientId:myClientId};
      var lang=document.getElementById('hcPipeLang');
      if(lang)params.language=lang.value;
      /* Speaker + mode: only send when changed (each change restarts the backend) */
      var spSel=document.getElementById('hcSpeaker');
      if(spSel&&spSel.value!==roomActiveSpeakerId)params.speakerId=spSel.value;
      var mdSel=document.getElementById('hcMode');
      if(mdSel&&mdSel.value!==roomMode)params.mode=mdSel.value;
      var st=document.getElementById('hcPipeStatus');
      if(st)st.textContent=t('applying');
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/control/pipeline',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.onload=function(){
        try{
          var res=JSON.parse(xhr.responseText);
          if(res.ok){if(st)st.textContent=t('applied').replace('{0}',res.changed);st.style.color='#4f4';if(params.language)roomSourceLang=params.language;if(params.speakerId!==undefined)roomActiveSpeakerId=params.speakerId;if(params.mode)roomMode=params.mode}
          else{if(st){st.textContent=res.error||t('failed');st.style.color='#f44'}}
        }catch(e){if(st){st.textContent=t('errorLabel');st.style.color='#f44'}}
      };
      xhr.onerror=function(){if(st){st.textContent=t('netError');st.style.color='#f44'}};
      xhr.send(JSON.stringify(params));
    });
  }
}

function showAddGuestPrompt(roomId){
  var overlay=document.createElement('div');
  overlay.id='addGuestOverlay';
  overlay.style.cssText='position:fixed;inset:0;background:rgba(0,0,0,0.85);z-index:300;display:flex;align-items:center;justify-content:center;flex-direction:column;padding:24px';
  overlay.innerHTML='<div style="color:#fff;font-size:18px;margin-bottom:16px">'+t('addGuest')+'</div>'+
    '<input id="guestNameInput" type="text" placeholder="'+t('guestName')+'" tabindex="1" style="padding:10px 14px;border-radius:8px;border:1px solid #555;background:#252540;color:#fff;font-size:16px;width:200px;outline:none;margin-bottom:12px">'+
    '<select id="guestLangSelect" tabindex="2" style="padding:10px 14px;border-radius:8px;border:1px solid #555;background:#252540;color:#fff;font-size:14px;width:224px;outline:none;margin-bottom:16px"></select>'+
    '<button id="guestAddBtn" tabindex="3" style="padding:10px 32px;border:none;border-radius:10px;background:#7c9cf7;color:#1a1a2e;font-size:16px;font-weight:600;cursor:pointer">'+t('guestAdd')+'</button>'+
    '<button id="guestCancelBtn" tabindex="4" style="margin-top:10px;padding:8px 24px;border:none;border-radius:8px;background:#333;color:#ccc;font-size:14px;cursor:pointer">'+t('cancel')+'</button>';
  document.body.appendChild(overlay);
  /* Populate language select */
  var sel=document.getElementById('guestLangSelect');
  for(var i=0;i<LANGS.length;i++){
    var opt=document.createElement('option');opt.value=LANGS[i][0];
    opt.textContent=LANGS[i][1]+' ('+LANGS[i][2]+')';
    sel.appendChild(opt);
  }
  var nameInput=document.getElementById('guestNameInput');
  var addBtn=document.getElementById('guestAddBtn');
  document.getElementById('guestCancelBtn').addEventListener('click',function(){overlay.remove()});
  function doAddGuest(){
    var name=nameInput.value.trim();
    var lang=sel.value;
    if(!name){nameInput.focus();return}
    var xhr=new XMLHttpRequest();
    xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/virtual-members',true);
    xhr.setRequestHeader('Content-Type','application/json');
    xhr.onload=function(){overlay.remove();loadRoomMembers()};
    xhr.send(JSON.stringify({name:name,language:lang,requestingClientId:myClientId}));
  }
  addBtn.addEventListener('click',doAddGuest);
  nameInput.addEventListener('keydown',function(e){if(e.key==='Enter'){e.preventDefault();sel.focus()}});
  sel.addEventListener('keydown',function(e){if(e.key==='Enter'){e.preventDefault();addBtn.focus()}});
  addBtn.addEventListener('keydown',function(e){if(e.key==='Enter'){e.preventDefault();doAddGuest()}});
  nameInput.focus();
}

function kickMember(clientId){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  var xhr=new XMLHttpRequest();
  xhr.open('POST','/api/rooms/'+encodeURIComponent(roomMatch[1])+'/kick',true);
  xhr.setRequestHeader('Content-Type','application/json');
  xhr.send(JSON.stringify({clientId:clientId,requestingClientId:myClientId}));
}

function removeVirtualMember(vmId){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  var xhr=new XMLHttpRequest();
  xhr.open('DELETE','/api/rooms/'+encodeURIComponent(roomMatch[1])+'/virtual-members/'+encodeURIComponent(vmId)+'?requestingClientId='+encodeURIComponent(myClientId),true);
  xhr.send();
}

/* Virtual member PTT identity selector — inserted into the dock above the PTT label */
function updateVirtualMemberPtt(){
  var existing=document.getElementById('vm-ptt-area');
  if(existing)existing.remove();
  if(virtualMembers.length===0)return;
  /* Only show on shared devices (host with virtual members) */
  if(!isHost)return;
  var dock=document.getElementById('roomControlsDock');
  if(!dock)return;
  var area=document.createElement('div');
  area.id='vm-ptt-area';
  area.style.cssText='display:flex;flex-wrap:wrap;gap:6px;align-items:center;justify-content:center;margin-bottom:8px;pointer-events:auto';
  /* Self identity */
  var selfBtn=document.createElement('div');
  selfBtn.className='vm-identity-btn';
  selfBtn.dataset.vmid='';
  selfBtn.style.cssText='padding:6px 14px;border-radius:20px;font-size:13px;cursor:pointer;border:2px solid '+(activeVirtualMemberId===''?'#7c9cf7':'#444')+';color:#fff;background:#252540';
  selfBtn.textContent=t('youLabel');
  area.appendChild(selfBtn);
  /* Virtual member identities */
  for(var i=0;i<virtualMembers.length;i++){
    var vm=virtualMembers[i];
    var vmBtn=document.createElement('div');
    vmBtn.className='vm-identity-btn';
    vmBtn.dataset.vmid=vm.id;
    vmBtn.style.cssText='padding:6px 14px;border-radius:20px;font-size:13px;cursor:pointer;border:2px solid '+(activeVirtualMemberId===vm.id?'#7c9cf7':'#444')+';color:#fff;background:#252540';
    vmBtn.textContent=escHtml(vm.name);
    area.appendChild(vmBtn);
  }
  /* Insert at the top of the dock (before the label) */
  var pttLabel=document.getElementById('ptt-label');
  if(pttLabel)dock.insertBefore(area,pttLabel);
  else dock.insertBefore(area,dock.firstChild);
  /* Click handlers */
  var btns=area.querySelectorAll('.vm-identity-btn');
  for(var j=0;j<btns.length;j++){
    btns[j].addEventListener('click',function(){
      activeVirtualMemberId=this.dataset.vmid;
      if(wsRef&&wsRef.readyState===1){
        wsRef.send(JSON.stringify({type:'speakAs',virtualMemberId:activeVirtualMemberId}));
      }
      updateVirtualMemberPtt();
      rerenderTranscript();
    });
  }
  /* Recalculate container padding to account for taller dock */
  adjustDockPadding();
}

/* Get the FLORES language code for the currently active identity */
function getActiveIdentityLang(){
  if(activeVirtualMemberId===''){
    /* Self — use the client's chosen translation language (JS var, not localStorage — avoids cross-tab issues) */
    return myTransLang||'eng_Latn';
  }
  for(var i=0;i<virtualMembers.length;i++){
    if(virtualMembers[i].id===activeVirtualMemberId)return virtualMembers[i].language||'eng_Latn';
  }
  return 'eng_Latn';
}

/* Re-render the entire transcript in the active identity's language (shared-device only) */
function rerenderTranscript(){
  if(transcriptCache.length===0)return;
  var activeLang=getActiveIdentityLang();

  /* Check if any cached messages need translation for the active language */
  var needTranslation=[];
  for(var i=0;i<transcriptCache.length;i++){
    var c=transcriptCache[i];
    if(c.translations){
      if(!c.translations[activeLang]&&c.sourceLang&&c.sourceLang!==activeLang){needTranslation.push(i)}
    } else if(c.text&&c.sourceLang&&c.sourceLang!==activeLang){
      needTranslation.push(i);
    }
  }

  /* Render immediately with what we have */
  doRender(activeLang);

  /* Fetch missing translations in background, then re-render */
  if(needTranslation.length>0){
    var pending=needTranslation.length;
    for(var j=0;j<needTranslation.length;j++){
      (function(idx){
        var c=transcriptCache[idx];
        var originalText=c.text||(c.translations?c.translations[c.sourceLang]||c.translations[Object.keys(c.translations)[0]]||'':'');
        if(!originalText){pending--;if(pending===0)doRender(activeLang);return}
        fetch('/api/translate',{
          method:'POST',
          headers:{'Content-Type':'application/json'},
          body:JSON.stringify({text:originalText,sourceLang:c.sourceLang,targetLang:activeLang})
        }).then(function(res){return res.json()}).then(function(data){
          if(data.text){
            if(!c.translations){c.translations={};c.translations[c.sourceLang]=c.text}
            c.translations[activeLang]=data.text;
          }
        }).catch(function(){}).finally(function(){
          pending--;
          if(pending===0)doRender(activeLang);
        });
      })(needTranslation[j]);
    }
  }
}

function doRender(activeLang){
  while(lines.children.length>1)lines.removeChild(lines.children[1]);
  for(var i=0;i<transcriptCache.length;i++){
    var c=transcriptCache[i];
    var displayText='';
    var ttsLang=activeLang;
    if(c.translations){
      displayText=c.translations[activeLang]||c.translations[c.sourceLang]||c.translations[Object.keys(c.translations)[0]]||'';
      if(!c.translations[activeLang])ttsLang=c.sourceLang||activeLang;
    } else {
      displayText=c.text||'';
      ttsLang=c.ttsLang||c.sourceLang||activeLang;
    }
    var tag=buildTag(c.lang||'',c.time||'');
    var el=document.createElement('div');
    el.className='line';
    el.style.fontSize=fontSize+'px';el.style.fontFamily=fontFamily;el.style.fontWeight=isBold?'bold':'normal';
    el.style.color=textColor;
    el.innerHTML='';
    if(tag){el.appendChild(document.createTextNode(tag))}
    if(c.speaker){
      var spk=document.createElement('span');
      spk.style.fontWeight='600';
      spk.style.color=getSpeakerColor(c.speaker);
      spk.textContent=c.speaker+': ';
      el.appendChild(spk);
    }
    el.appendChild(document.createTextNode(displayText));
    if(c.speaker){addLineSpeakBtn(el,displayText,ttsLang)}
    insertLine(el);
  }
  autoScroll();
}

function adjustDockPadding(){
  var dock=document.getElementById('roomControlsDock');
  var cont=document.getElementById('container');
  if(dock&&cont){
    cont.style.paddingBottom=(dock.offsetHeight+8)+'px';
  }
}

function updatePttLabel(){
  var label=document.getElementById('ptt-label');
  if(label)label.textContent=pttMode==='toggle'?'Tap to speak':'Hold to speak';
}

/* Text chat (Conversation rooms) — inserted into the pttChatRow before the mic button */
function initTextChat(){
  if(pttRoomType!=='conversation')return;
  if(document.getElementById('chatInput'))return;
  var row=document.getElementById('pttChatRow');
  if(!row)return;
  var input=document.createElement('textarea');
  input.id='chatInput';
  input.rows=1;
  input.placeholder=t('typeMessage');
  input.style.cssText='flex:1;padding:10px 14px;border-radius:18px;border:1px solid #444;background:#252540;color:#fff;font-size:14px;outline:none;min-width:0;resize:none;overflow:hidden;max-height:120px;line-height:1.4;font-family:inherit';
  function autoResize(){input.style.height='auto';input.style.height=Math.min(input.scrollHeight,120)+'px'}
  input.addEventListener('input',autoResize);
  /* Insert before the mic button */
  var pttBtn=document.getElementById('ptt-btn');
  row.insertBefore(input,pttBtn);
  function sendChat(){
    var text=input.value.trim();
    if(!text)return;
    if(wsRef&&wsRef.readyState===1){
      wsRef.send(JSON.stringify({type:'chatMessage',text:text}));
    }
    input.value='';
    autoResize();
  }
  input.addEventListener('keydown',function(e){if(e.key==='Enter'&&!e.shiftKey){e.preventDefault();sendChat()}});
  setTimeout(adjustDockPadding,50);
}

/* Chapter counts by book (standard Protestant canon) */
function getBookChapterCount(book){
  var counts={Gen:50,Exod:40,Lev:27,Num:36,Deut:34,Josh:24,Judg:21,Ruth:4,'1Sam':31,'2Sam':24,'1Kgs':22,'2Kgs':25,'1Chr':29,'2Chr':36,Ezra:10,Neh:13,Esth:10,Job:42,Ps:150,Prov:31,Eccl:12,Song:8,Isa:66,Jer:52,Lam:5,Ezek:48,Dan:12,Hos:14,Joel:3,Amos:9,Obad:1,Jonah:4,Mic:7,Nah:3,Hab:3,Zeph:3,Hag:2,Zech:14,Mal:4,Matt:28,Mark:16,Luke:24,John:21,Acts:28,Rom:16,'1Cor':16,'2Cor':13,Gal:6,Eph:6,Phil:4,Col:4,'1Thess':5,'2Thess':3,'1Tim':6,'2Tim':4,Titus:3,Phlm:1,Heb:13,Jas:5,'1Pet':5,'2Pet':3,'1John':5,'2John':1,'3John':1,Jude:1,Rev:22};
  return counts[book]||50;
}
