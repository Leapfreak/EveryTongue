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
    sending:'Sending...',cmdSent:' command sent',cmdFail:'Failed to send command',
    liveRun:'Live: RUNNING',stopped:'Status: STOPPED',
    noServer:'Unable to reach server',checking:'Checking...',
    dfltVoice:'Default',title:'Every Tongue',
    bold:'Bold',font:'Font',style:'Style',voice:'Voice',speed:'Speed',color:'Text Color',
    slow:'Slow',normal:'Normal',fast:'Fast',vfast:'Very Fast',
    start:'Start',stop:'Stop',restart:'Restart',clear:'Clear',
    saveTranscript:'Save Transcript',transLang:'Translation',remote:'Remote Control',settings:'Settings',readAloud:'Read aloud',keepScreen:'Keep screen on',scrollDir:'Scroll Direction',scrollUp:'Bottom-up (newest at bottom)',scrollDown:'Top-down (newest at top)',tags:'Tags',tagOff:'Off',tagLang:'Language',tagTime:'Time',tagBoth:'Language + Time',bible:'Bible',bibleOT:'Old Testament',bibleNT:'New Testament',bibleSearch:'Search',bibleNoResults:'No results found',bibleSelectTrans:'Select a translation',cloudVoice:'Every Tongue Voices',ttsBehind:'{0} behind \u2014 tap to skip',readAll:'Read All',readVerse:'Read',chooseLang:'Choose your language',lpPopular:'Popular',lpAll:'All Languages',searchLangs:'Search languages...',noTranslation:'No translation',browseAll:'Browse All',adminLabel:'Administrator',adminPin:'PIN',adminBad:'Invalid PIN',adminOk:'Admin access granted'};
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

/* ── Language data: [nllbCode, nativeName, englishName, bcp47Prefix] ── */
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
            /* Re-populate transLangSelect */
            populateTransLangSelect();
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
}
function hideLangPicker(){
  document.getElementById('langPicker').classList.remove('open');
  var dock=document.getElementById('roomControlsDock');
  if(dock){dock.style.display='flex';setTimeout(adjustDockPadding,50)}
}
var voiceManuallySet=!!ss('voice');
function pickLang(code){
  LOG('pickLang: '+code);
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
function autoSelectVoiceForLang(nllbCode){
  var bcp=nllbToBcp47Lookup(nllbCode);
  if(!bcp){LOG('autoSelectVoice: no BCP47 mapping for '+nllbCode);return}
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
      for(var j=0;j<LANGS.length;j++){
        if(LANGS[j][0]===POPULAR_LANGS[p]){list.appendChild(createLangItem(LANGS[j]));break}
      }
    }
    var allLabel=document.createElement('div');allLabel.className='lp-section-label';
    allLabel.textContent=t('lpAll');list.appendChild(allLabel);
  }
  for(var i=0;i<LANGS.length;i++){
    var l=LANGS[i];
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
  ssRemove('langChosen');
  /* Navigate to lobby */
  window.location.href=window.location.origin+'/lobby.html';
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
  var joinUrl=location.origin+'/index.html?room='+encodeURIComponent(roomId);
  document.getElementById('roomQrImg').src='/api/rooms/'+encodeURIComponent(roomId)+'/qr';
  var qrUrlEl=document.getElementById('roomQrUrl');qrUrlEl.innerHTML='<a href="'+joinUrl+'" style="color:#7c9cf7;text-decoration:underline">'+joinUrl+'</a>';
}
function toggleRoomQr(){
  var overlay=document.getElementById('roomQrOverlay');
  _roomQrVisible=!_roomQrVisible;
  overlay.style.display=_roomQrVisible?'flex':'none';
}
initRoomShareButton();

/* Populate transLangSelect dropdown dynamically from LANGS */
function populateTransLangSelect(){
  var sel=document.getElementById('transLangSelect');
  sel.innerHTML='';
  for(var i=0;i<LANGS.length;i++){
    var opt=document.createElement('option');opt.value=LANGS[i][0];
    opt.textContent=LANGS[i][1]+' ('+LANGS[i][2]+')';
    sel.appendChild(opt);
  }
  var saved=ss('transLang')||'';
  for(var j=0;j<sel.options.length;j++){if(sel.options[j].value===saved){sel.selectedIndex=j;break}}
}
populateTransLangSelect();

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
}
populateVoices();
if(synth.onvoiceschanged!==undefined)synth.onvoiceschanged=populateVoices;

/* ── Panel toggle ── */
function closeAllPanels(){panel.style.display='none';adminPanel.style.display='none';if(adminPollTimer){clearInterval(adminPollTimer);adminPollTimer=null}}
function togglePanel(){if(panel.style.display==='block'){panel.style.display='none'}else{closeAllPanels();panel.style.display='block'}}
document.addEventListener('click',function(e){if(!panel.contains(e.target)&&!adminPanel.contains(e.target)&&!document.getElementById('toolbar').contains(e.target)){closeAllPanels()}})

function toggleSpeak(){
  speakEnabled=!speakEnabled;
  ssSet('speak',speakEnabled);
  LOG('toggleSpeak → '+speakEnabled);
  if(speakEnabled){btnSpeak.classList.add('active');btnSpeak.innerHTML='&#128266; '+t('readAloud')}
  else{btnSpeak.classList.remove('active');btnSpeak.innerHTML='&#128264; '+t('readAloud');synth.cancel();clearTtsQueue()}
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

/* NLLB language code to BCP47 prefix — built dynamically from LANGS */
function nllbToBcp47Lookup(nllb){
  for(var i=0;i<LANGS.length;i++){if(LANGS[i][0]===nllb)return LANGS[i][3]}
  return '';
}

function hasBrowserVoiceForLang(){
  var transLang=myTransLang||'';
  if(!transLang)return true; /* original language — browser usually has it */
  var bcp=nllbToBcp47Lookup(transLang);
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
  var blob=new Blob([text],{type:'text/plain;charset=utf-8'});
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
    if(currentEl){currentEl.remove();currentEl=null}
    ws.send(JSON.stringify({type:'setLanguage',language:myTransLang||'',lastId:lastCommitId}));
  };
  ws.onclose=function(){LOG('WS closed');statusEl.textContent=t('disconnected');statusBar.className='disconnected';wsRef=null;setTimeout(connect,2000)};
  ws.onerror=function(){LOG('WS error');ws.close()};
  ws.onmessage=function(e){
    try{var msg=JSON.parse(e.data);
      if(msg.type==='commit'){
        /* Commit arrival clears speaking indicator for that speaker */
        if(msg.speaker){clearSpeakerByName(msg.speaker);updateSpeakingUI()}
        var id=msg.id||0;
        if(id>lastCommitId){
          lastCommitId=id;
          if(msg.translations){
            /* Shared-device message with all translations */
            transcriptCache.push({id:id,speaker:msg.speaker||'',time:msg.time||'',lang:msg.lang||'',sourceLang:msg.sourceLang||'',translations:msg.translations});
            var activeLang=getActiveIdentityLang();
            var displayText=msg.translations[activeLang]||msg.translations[msg.sourceLang]||msg.translations[Object.keys(msg.translations)[0]]||'';
            addCommitted(displayText,msg.lang||'',msg.time||'',null,msg.speaker||'',activeLang);
          } else {
            /* Normal single-language message — text is in my language (server translated for me) */
            var textLang=myTransLang||msg.sourceLang||'';
            transcriptCache.push({id:id,speaker:msg.speaker||'',time:msg.time||'',text:msg.text||'',lang:msg.lang||'',sourceLang:msg.sourceLang||'',ttsLang:textLang});
            addCommitted(msg.text,msg.lang||'',msg.time||'',msg.refs||null,msg.speaker||'',textLang);
          }
        }
      }
      else if(msg.type==='update')updateCurrent(msg.text);
      else if(msg.type==='clear'){LOG('WS clear');if(currentEl){currentEl.remove();currentEl=null}while(lines.children.length>1)lines.removeChild(lines.children[1]);lastCommitId=0;transcriptCache=[];clearTtsQueue();autoScroll()}
      else if(msg.type==='tts'){handleTtsMessage(msg)}
      else if(msg.type==='welcome'){myClientId=msg.clientId||'';LOG('My client ID: '+myClientId);initPushToTalk();tryClaimHost()}
      else if(msg.type==='pong'){}
      else if(msg.type==='error'){showRoomError(msg.message||'Error')}
      else if(msg.type==='roomClosed'){showRoomError('Room has ended');setTimeout(function(){location.href='/lobby.html'},3000)}
      else if(msg.type==='kicked'){showRoomError('You have been removed');setTimeout(function(){location.href='/lobby.html'},3000)}
      else if(msg.type==='roomLocked'){LOG('Room locked: '+msg.locked)}
      else if(msg.type==='pttModeChanged'){pttMode=msg.mode||'hold';updatePttLabel()}
      else if(msg.type==='memberJoined'){addRoomMember(msg)}
      else if(msg.type==='memberLeft'){removeRoomMember(msg.clientId)}
      else if(msg.type==='memberUpdated'){updateRoomMember(msg)}
      else if(msg.type==='virtualMemberAdded'){addVirtualMemberToRoom(msg)}
      else if(msg.type==='virtualMemberRemoved'){removeVirtualMemberFromRoom(msg.id)}
      else if(msg.type==='speaking'){handleSpeakingIndicator(msg)}
      else{LOG('WS unknown msg type: '+msg.type)}
    }catch(ex){LOG('WS msg error: '+ex+' data='+String(e.data).substring(0,100))}
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
document.getElementById('btnAdmin').title=t('remote');
document.getElementById('btnSettings').title=t('settings');
document.getElementById('lblSpeak').textContent=t('readAloud');
if(!speakEnabled){btnSpeak.innerHTML='&#128264; '+t('readAloud')}
document.getElementById('lblFont').textContent=t('font');
document.getElementById('lblStyle').textContent=t('style');
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
document.getElementById('lpTitle').textContent=t('chooseLang');
document.getElementById('lpSearch').placeholder=t('searchLangs');
document.getElementById('lpSkip').textContent=t('noTranslation');
document.getElementById('lpAdminToggle').textContent=t('adminLabel');
document.getElementById('lpAdminPin').placeholder=t('adminPin');

/* ── Admin access control ── */
localStorage.removeItem('isAdmin');
var isAdmin=sessionStorage.getItem('isAdmin')==='true';
var hasAdminPin=false;

function toggleAdminLogin(){
  var form=document.getElementById('lpAdminForm');
  form.style.display=form.style.display==='none'?'flex':'none';
  if(form.style.display==='flex')document.getElementById('lpAdminPin').focus();
}
function verifyAdminPin(){
  var pin=document.getElementById('lpAdminPin').value;
  var msg=document.getElementById('lpAdminMsg');
  if(!pin){msg.textContent=t('adminBad');return}
  fetch('/api/admin/verify?pin='+encodeURIComponent(pin)).then(function(r){return r.json()}).then(function(d){
    if(d.ok){
      sessionStorage.setItem('isAdmin','true');
      isAdmin=true;
      msg.style.color='#4f4';msg.textContent=t('adminOk');
      document.getElementById('btnAdmin').style.display='';
      document.getElementById('lpAdminPin').value='';
    }else{
      msg.style.color='#f44';msg.textContent=t('adminBad');
    }
  }).catch(function(){msg.style.color='#f44';msg.textContent=t('adminBad')});
}
/* Show admin button if already authenticated, hide admin section on picker if no PIN configured */
function checkAdminAccess(){
  if(isAdmin){document.getElementById('btnAdmin').style.display=''}
  fetch('/api/config').then(function(r){return r.json()}).then(function(cfg){
    hasAdminPin=cfg.hasAdminPin;
    if(!hasAdminPin){document.getElementById('lpAdmin').style.display='none'}
    else{document.getElementById('lpAdmin').style.display=''}
  }).catch(function(){});
}
checkAdminAccess();
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
  /* Show language picker on page load — skip if desktop app passed ?bibleLang= */
  var _qs=new URLSearchParams(window.location.search);
  if(!_qs.get('bibleLang')){showLangPicker()}
})();

/* ── Keep screen on (Wake Lock) ── */
var wakeLockObj=null;
var wakeActive=false;
var btnWake=document.getElementById('btnWake');
btnWake.title=t('keepScreen');

function setWakeActive(on){
  wakeActive=on;
  if(on){btnWake.classList.add('active')}else{btnWake.classList.remove('active')}
}

async function acquireWakeLock(){
  if(window.isSecureContext&&'wakeLock' in navigator){
    try{
      wakeLockObj=await navigator.wakeLock.request('screen');
      setWakeActive(true);
      wakeLockObj.addEventListener('release',function(){wakeLockObj=null;if(wakeActive)acquireWakeLock()});
      return;
    }catch(e){}
  }
  showCertSetup();
}

function releaseWakeLock(){
  wakeActive=false;
  if(wakeLockObj){try{wakeLockObj.release()}catch(e){}wakeLockObj=null}
  setWakeActive(false);
}

function toggleWakeLock(){if(wakeActive)releaseWakeLock();else acquireWakeLock()}

function showCertSetup(){
  var hp=parseInt(location.port||80)+1;
  var url='https://'+location.hostname+':'+hp;
  var d=document.createElement('div');
  d.style.cssText='position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,0.95);z-index:1000;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:24px;color:#fff;font-size:16px;line-height:1.6';
  var c=document.createElement('div');c.style.cssText='max-width:400px;text-align:left';
  var h=document.createElement('h2');h.style.cssText='color:#ffdd57;margin-bottom:16px;text-align:center';h.textContent=t('wakeTitle');c.appendChild(h);
  var p0=document.createElement('p');p0.style.cssText='margin-bottom:16px;text-align:center';p0.textContent=t('wakeDesc');c.appendChild(p0);
  var isFF=navigator.userAgent.indexOf('Firefox')>-1;
  var isSafari=/Safari/.test(navigator.userAgent)&&!/Chrome/.test(navigator.userAgent);
  var steps;
  if(isFF){steps=[t('stepTap'),t('stepWarn'),t('stepAdv'),t('stepAccept'),t('stepRetry')]}
  else if(isSafari){steps=[t('stepTap'),t('stepWarn'),t('stepDetails'),t('stepVisit'),t('stepRetry')]}
  else{steps=[t('stepTap'),t('stepWarn'),t('stepAdv'),t('stepProceed').replace('{0}',location.hostname),t('stepRetry')]}
  for(var i=0;i<steps.length;i++){var s=document.createElement('p');s.style.cssText='margin-bottom:8px;padding-left:8px';s.textContent=(i+1)+'. '+steps[i];c.appendChild(s)}
  var br=document.createElement('div');br.style.cssText='text-align:center;margin-top:20px';
  var a1=document.createElement('a');a1.href=url;a1.textContent=t('openSecure');a1.style.cssText='display:inline-block;background:#47f;color:#fff;padding:14px 28px;border-radius:8px;text-decoration:none;font-size:18px;margin-bottom:16px';br.appendChild(a1);
  br.appendChild(document.createElement('br'));
  var b2=document.createElement('button');b2.textContent=t('cancel');b2.style.cssText='background:#333;color:#aaa;border:1px solid #555;padding:8px 20px;border-radius:6px;font-size:14px;cursor:pointer;margin-top:8px';b2.onclick=function(){d.remove()};br.appendChild(b2);
  c.appendChild(br);d.appendChild(c);document.body.appendChild(d);
}

document.addEventListener('visibilitychange',function(){
  if(document.visibilityState==='visible'&&wakeActive&&!wakeLockObj)acquireWakeLock();
});

/* ── Admin remote control ── */
var adminPanel=document.getElementById('adminPanel');
var adminStatus=document.getElementById('adminStatus');
var adminPollTimer=null;
function toggleAdmin(){
  if(adminPanel.style.display==='block'){adminPanel.style.display='none';if(adminPollTimer){clearInterval(adminPollTimer);adminPollTimer=null}}
  else{closeAllPanels();adminPanel.style.display='block';pollStatus();adminPollTimer=setInterval(pollStatus,3000)}
}
function sendCommand(action){
  LOG('sendCommand: '+action);
  adminStatus.textContent=t('sending');
  fetch('/api/control?action='+action).then(function(r){return r.json()}).then(function(d){
    adminStatus.textContent=action+t('cmdSent');
    setTimeout(function(){closeAllPanels()},600);
  }).catch(function(){adminStatus.textContent=t('cmdFail')});
}
function pollStatus(){
  fetch('/api/control?action=status').then(function(r){return r.json()}).then(function(d){
    if(d.live){adminStatus.textContent=t('liveRun');adminStatus.style.color='#4f4'}
    else{adminStatus.textContent=t('stopped');adminStatus.style.color='#f44'}
    if(d.inputLang){document.getElementById('inputLangSelect').value=d.inputLang}
  }).catch(function(){adminStatus.textContent=t('noServer');adminStatus.style.color='#888'});
}
function setInputLang(lang){
  if(wsRef&&wsRef.readyState===1){wsRef.send(JSON.stringify({type:'setInputLanguage',language:lang}))}
}
function requestTune(){
  adminStatus.textContent='Fetching stats...';
  fetch('/api/control?action=tune').then(function(r){return r.json()}).then(function(d){
    if(d.error){adminStatus.textContent=d.error;return}
    var msg='TUNING RECOMMENDATIONS\n\n';
    for(var i=0;i<d.tips.length;i++){msg+=d.tips[i]+'\n'}
    msg+='\nSession: '+d.commits+' commits, '+d.hallucinations+' hallucinations filtered';
    msg+='\nAvg duration: '+d.durAvg+'s, Max: '+d.durMax+'s';
    if(d.wpsAvg>0){msg+='\nSpeaking rate: '+d.wpsAvg+' words/sec'}
    msg+='\n\nCurrent: Max Segment='+d.currentMaxSeg+'s, VAD Silence='+d.currentVadSilence+'ms';
    msg+='\nSuggested: Max Segment='+d.suggestedMaxSeg+'s, VAD Silence='+d.suggestedVadSilence+'ms';
    if(d.suggestedMaxSeg!==d.currentMaxSeg||d.suggestedVadSilence!==d.currentVadSilence){
      msg+='\n\nApply suggested values?';
      if(confirm(msg)){
        fetch('/api/control?action=setsliders&maxSeg='+d.suggestedMaxSeg+'&vadSilence='+d.suggestedVadSilence).then(function(r){return r.json()}).then(function(r){
          if(r.ok){adminStatus.textContent='Sliders updated!';adminStatus.style.color='#4f4'}
          else{adminStatus.textContent='Failed to apply'}
        }).catch(function(){adminStatus.textContent='Failed to apply'});
      }else{adminStatus.textContent='Tune cancelled'}
    }else{alert(msg);adminStatus.textContent='No changes needed'}
  }).catch(function(){adminStatus.textContent='Failed to fetch stats'});
}

/* ── Apply i18n to admin panel ── */
adminStatus.textContent=t('checking');
var admBtns=document.querySelectorAll('#adminPanel button');
admBtns[0].innerHTML='&#9654; '+t('start');admBtns[1].innerHTML='&#9632; '+t('stop');
admBtns[2].innerHTML='&#8635; '+t('restart');admBtns[3].innerHTML='&#10060; '+t('clear');

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
  /* 2. Use app translation language — extract 3-letter code from NLLB code */
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
      bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">No Bibles installed for this language.<br>Use the Download Manager to add Bibles.</div>';
      return;
    }
    populateBibleData(data);
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:40px">Failed to load translations</div>';
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
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Failed to load</div>';
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
      bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:20px">No verses</div>';return;
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
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Failed to load</div>';
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
    if(!ref||!ref.isValid){bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Could not parse reference</div>';return}
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
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Error looking up reference</div>';
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
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Searching...</div>';
  bibleNavStack=[{type:'books'}];
  btnBibleBack.style.display='';
  bibleNavTitle.textContent=t('bibleSearch')+': '+q;

  fetch('/bible/search?q='+encodeURIComponent(q)+'&translation='+encodeURIComponent(currentBibleTrans)+'&max=200').then(function(r){return r.json()}).then(function(results){
    bibleContent.innerHTML='';
    if(!results||results.length===0){
      bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:20px">'+t('bibleNoResults')+'</div>';return;
    }
    var countDiv=document.createElement('div');countDiv.style.cssText='color:#888;font-size:12px;padding:4px 0 8px;border-bottom:1px solid #333;margin-bottom:8px';
    countDiv.textContent=results.length+(results.length>=200?'+':'')+' results';
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
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Search failed</div>';
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
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Failed to load</div>';
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
  /* Bible language codes (ISO 639-3: eng, spa, fra) match NLLB prefixes directly */
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
  bibleContent.insertBefore(bar,bibleContent.firstChild);
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
        if(room.isHost){isHost=true;showHostControls()}
        /* Conversation: everyone gets PTT. Conference: no mic (audio from desktop). */
        if(pttRoomType==='conversation'){
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

  /* Insert text chat area at the bottom of the dock (below PTT) */
  initTextChat();
}

function startRecording(btn,label){
  if(pttActive)return;
  if(!navigator.mediaDevices||!navigator.mediaDevices.getUserMedia){
    label.textContent='Microphone not available';
    return;
  }
  pttActive=true;
  pttChunks=[];
  btn.style.background='#e74c3c';
  btn.style.transform='scale(1.15)';
  btn.classList.add('ptt-recording');
  label.textContent='Recording...';
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
      label.textContent='Mic access denied';
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
  label.textContent='Sending...';
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

/* Auto-reclaim host via stored token */
function tryClaimHost(){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch||!myClientId)return;
  var roomId=roomMatch[1];
  try{
    var myRooms=JSON.parse(localStorage.getItem('myRooms')||'[]');
    var mine=null;
    for(var i=0;i<myRooms.length;i++){if(myRooms[i].id===roomId){mine=myRooms[i];break}}
    if(!mine||!mine.hostToken)return;
    var xhr=new XMLHttpRequest();
    xhr.open('POST','/api/rooms/'+encodeURIComponent(roomId)+'/claim-host',true);
    xhr.setRequestHeader('Content-Type','application/json');
    xhr.onload=function(){
      if(xhr.status===200){
        try{
          var res=JSON.parse(xhr.responseText);
          if(res.ok){isHost=true;LOG('Host reclaimed');showHostControls()}
        }catch(e){}
      }
    };
    xhr.send(JSON.stringify({hostToken:mine.hostToken,clientId:myClientId}));
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
    var nameSpan='<span style="color:#fff">'+escHtml(m.displayName||'Guest')+'</span>';
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
    var vName='<span style="color:#aaa;font-style:italic">'+escHtml(vm.name||'Guest')+' (shared)</span>';
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
    activeSpeakers[cid]=msg.displayName||'Guest';
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
    nameEl.textContent='Recording...';
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
  if(document.getElementById('hostGearBtn'))return;
  var btn=document.createElement('button');
  btn.id='hostGearBtn';
  btn.title='Host Controls';
  btn.innerHTML='<img src="/img/icon.png" style="height:20px;width:20px;border-radius:50%;vertical-align:middle">';
  btn.addEventListener('click',toggleHostPanel);
  var toolbar=document.getElementById('toolbar');
  if(toolbar)toolbar.appendChild(btn);
}

function toggleHostPanel(){
  var panel=document.getElementById('hostPanel');
  if(panel){panel.remove();return}
  panel=document.createElement('div');
  panel.id='hostPanel';
  panel.style.cssText='position:fixed;top:50px;right:8px;background:#1e1e3a;border:1px solid #444;border-radius:10px;padding:16px;z-index:200;min-width:200px;box-shadow:0 4px 20px rgba(0,0,0,0.6)';
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  var roomId=roomMatch?roomMatch[1]:'';
  var hostHtml=
    '<div style="color:#fff;font-weight:600;margin-bottom:12px">Host Controls</div>'+
    '<button id="hcEndRoom" style="width:100%;padding:10px;border:none;border-radius:8px;background:#e74c3c;color:#fff;font-size:14px;font-weight:600;cursor:pointer;margin-bottom:8px">End Room</button>';
  if(pttRoomType!=='conference'){
    hostHtml+=
      '<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:8px"><span style="color:#ccc;font-size:13px">Lock Room</span><div id="hcLockToggle" class="hc-toggle" style="width:40px;height:22px;background:#444;border-radius:11px;position:relative;cursor:pointer;transition:background 0.2s"><div style="width:18px;height:18px;background:#fff;border-radius:50%;position:absolute;top:2px;left:2px;transition:transform 0.2s"></div></div></div>'+
      '<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:12px"><span style="color:#ccc;font-size:13px">PTT: Tap to toggle</span><div id="hcPttToggle" class="hc-toggle" style="width:40px;height:22px;background:#444;border-radius:11px;position:relative;cursor:pointer;transition:background 0.2s"><div style="width:18px;height:18px;background:#fff;border-radius:50%;position:absolute;top:2px;left:2px;transition:transform 0.2s"></div></div></div>'+
      '<button id="hcAddGuest" style="width:100%;padding:10px;border:1px solid #7c9cf7;border-radius:8px;background:transparent;color:#7c9cf7;font-size:14px;cursor:pointer">+ Add Guest</button>';
  }
  panel.innerHTML=hostHtml;

  /* Pipeline controls for conference rooms */
  if(pttRoomType==='conference'){
    var pipeHtml='<div style="border-top:1px solid #444;margin-top:12px;padding-top:12px">'+
      '<div style="color:#aaa;font-size:12px;font-weight:600;margin-bottom:8px">Pipeline</div>'+
      '<label style="color:#888;font-size:11px">Speaker Language</label>'+
      '<select id="hcPipeLang" style="width:100%;padding:6px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:13px;margin-bottom:8px">'+
      '<option value="auto">Auto Detect</option>'+
      '<option value="ca">Catalan</option><option value="es">Spanish</option><option value="en">English</option>'+
      '<option value="fr">French</option><option value="de">German</option><option value="it">Italian</option>'+
      '<option value="pt">Portuguese</option><option value="nl">Dutch</option><option value="ru">Russian</option>'+
      '<option value="zh">Chinese</option><option value="ja">Japanese</option><option value="ko">Korean</option>'+
      '<option value="ar">Arabic</option></select>'+
      '<label style="color:#888;font-size:11px">Max Segment: <span id="hcMaxSegVal">15</span>s</label>'+
      '<input type="range" id="hcMaxSeg" min="5" max="60" value="15" style="width:100%;margin-bottom:8px">'+
      '<label style="color:#888;font-size:11px">VAD Silence: <span id="hcVadVal">800</span>ms</label>'+
      '<input type="range" id="hcVad" min="200" max="2000" step="100" value="800" style="width:100%;margin-bottom:8px">'+
      '<label style="color:#888;font-size:11px">Beam Size</label>'+
      '<select id="hcBeam" style="width:100%;padding:6px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:13px;margin-bottom:8px">'+
      '<option value="1">1</option><option value="3">3</option><option value="5">5</option><option value="7" selected>7</option></select>'+
      '<label style="color:#888;font-size:11px">Initial Prompt</label>'+
      '<input type="text" id="hcPrompt" placeholder="Vocabulary hints" style="width:100%;padding:6px;border-radius:6px;border:1px solid #555;background:#252540;color:#fff;font-size:13px;margin-bottom:8px">'+
      '<button id="hcPipeApply" style="width:100%;padding:8px;border:none;border-radius:8px;background:#7c9cf7;color:#1a1a2e;font-size:13px;font-weight:600;cursor:pointer">Apply</button>'+
      '<div id="hcPipeStatus" style="color:#888;font-size:11px;margin-top:4px;text-align:center"></div>'+
      '</div>';
    panel.innerHTML+=pipeHtml;
  }

  document.body.appendChild(panel);

  /* Pre-select pipeline language from template */
  var pipeLang=document.getElementById('hcPipeLang');
  if(pipeLang&&roomSourceLang){pipeLang.value=roomSourceLang}

  document.getElementById('hcEndRoom').addEventListener('click',function(){
    var xhr=new XMLHttpRequest();
    xhr.open('DELETE','/api/rooms/'+encodeURIComponent(roomId)+'?clientId='+encodeURIComponent(myClientId),true);
    xhr.onload=function(){location.href='/lobby.html'};
    xhr.send();
  });

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
    var hcMaxSeg=document.getElementById('hcMaxSeg');
    var hcVad=document.getElementById('hcVad');
    if(hcMaxSeg)hcMaxSeg.addEventListener('input',function(){document.getElementById('hcMaxSegVal').textContent=this.value});
    if(hcVad)hcVad.addEventListener('input',function(){document.getElementById('hcVadVal').textContent=this.value});

    var hcApply=document.getElementById('hcPipeApply');
    if(hcApply)hcApply.addEventListener('click',function(){
      var params={roomId:roomId,clientId:myClientId};
      var lang=document.getElementById('hcPipeLang');
      if(lang)params.language=lang.value;
      var ms=document.getElementById('hcMaxSeg');
      if(ms)params.maxSegmentSec=parseInt(ms.value,10);
      var vd=document.getElementById('hcVad');
      if(vd)params.vadSilenceMs=parseInt(vd.value,10);
      var bm=document.getElementById('hcBeam');
      if(bm)params.beamSize=parseInt(bm.value,10);
      var pr=document.getElementById('hcPrompt');
      if(pr&&pr.value.trim())params.initialPrompt=pr.value.trim();
      var st=document.getElementById('hcPipeStatus');
      if(st)st.textContent='Applying...';
      var xhr=new XMLHttpRequest();
      xhr.open('POST','/api/control/pipeline',true);
      xhr.setRequestHeader('Content-Type','application/json');
      xhr.onload=function(){
        try{
          var res=JSON.parse(xhr.responseText);
          if(res.ok){if(st)st.textContent='Applied ('+res.changed+' params)';st.style.color='#4f4'}
          else{if(st){st.textContent=res.error||'Failed';st.style.color='#f44'}}
        }catch(e){if(st){st.textContent='Error';st.style.color='#f44'}}
      };
      xhr.onerror=function(){if(st){st.textContent='Network error';st.style.color='#f44'}};
      xhr.send(JSON.stringify(params));
    });
  }
}

function showAddGuestPrompt(roomId){
  var overlay=document.createElement('div');
  overlay.id='addGuestOverlay';
  overlay.style.cssText='position:fixed;inset:0;background:rgba(0,0,0,0.85);z-index:300;display:flex;align-items:center;justify-content:center;flex-direction:column;padding:24px';
  overlay.innerHTML='<div style="color:#fff;font-size:18px;margin-bottom:16px">Add Guest</div>'+
    '<input id="guestNameInput" type="text" placeholder="Name" tabindex="1" style="padding:10px 14px;border-radius:8px;border:1px solid #555;background:#252540;color:#fff;font-size:16px;width:200px;outline:none;margin-bottom:12px">'+
    '<select id="guestLangSelect" tabindex="2" style="padding:10px 14px;border-radius:8px;border:1px solid #555;background:#252540;color:#fff;font-size:14px;width:224px;outline:none;margin-bottom:16px"></select>'+
    '<button id="guestAddBtn" tabindex="3" style="padding:10px 32px;border:none;border-radius:10px;background:#7c9cf7;color:#1a1a2e;font-size:16px;font-weight:600;cursor:pointer">Add</button>'+
    '<button id="guestCancelBtn" tabindex="4" style="margin-top:10px;padding:8px 24px;border:none;border-radius:8px;background:#333;color:#ccc;font-size:14px;cursor:pointer">Cancel</button>';
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
  selfBtn.textContent='You';
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

/* Get the NLLB language code for the currently active identity */
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
  input.placeholder='Type a message...';
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
