/* Every Tongue - Subtitle Client
   ES5 compatible (except async/await for Wake Lock API) */

function LOG(msg){try{console.log('[TT] '+msg)}catch(e){}}

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
/* Fetch server-side locale (overlays English defaults) */
(function(){
  try{
    var xhr=new XMLHttpRequest();
    xhr.open('GET','/api/locale',true);
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
}
var voiceManuallySet=!!localStorage.getItem('voice');
function pickLang(code){
  LOG('pickLang: '+code);
  localStorage.setItem('transLang',code);
  localStorage.setItem('langChosen','true');
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
      localStorage.setItem('voice',voices[i].name);
      voiceSelect.value=voices[i].name;
      voiceManuallySet=false; /* auto-set, not manual */
      return;
    }
  }
  LOG('autoSelectVoice: no voice found for '+bcp+', keeping default');
  selectedVoice='';
  localStorage.setItem('voice','');
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
  localStorage.removeItem('langChosen');
  /* Navigate to clean URL without query params (e.g. ?bibleLang=) so we reach the landing page */
  window.location.href=window.location.origin+window.location.pathname;
}

/* Populate transLangSelect dropdown dynamically from LANGS */
function populateTransLangSelect(){
  var sel=document.getElementById('transLangSelect');
  sel.innerHTML='';
  for(var i=0;i<LANGS.length;i++){
    var opt=document.createElement('option');opt.value=LANGS[i][0];
    opt.textContent=LANGS[i][1]+' ('+LANGS[i][2]+')';
    sel.appendChild(opt);
  }
  var saved=localStorage.getItem('transLang')||'';
  for(var j=0;j<sel.options.length;j++){if(sel.options[j].value===saved){sel.selectedIndex=j;break}}
}
populateTransLangSelect();

/* ── DOM references ── */
var fontSize=28;
var currentEl=null;
var speakEnabled=false;
var selectedVoice='';
var speechRate=1;
var synth=window.speechSynthesis;
var lines=document.getElementById('lines');
var container=document.getElementById('container');
var statusEl=document.getElementById('status');
var panel=document.getElementById('panel');
var btnSpeak=document.getElementById('btnSpeak');
var voiceSelect=document.getElementById('voiceSelect');
var rateSelect=document.getElementById('rateSelect');

/* ── Restore saved preferences ── */
if(localStorage.getItem('voice'))selectedVoice=localStorage.getItem('voice');
if(localStorage.getItem('rate')){speechRate=parseFloat(localStorage.getItem('rate'));rateSelect.value=localStorage.getItem('rate')}
if(localStorage.getItem('speak')==='true'){speakEnabled=true;btnSpeak.classList.add('active');btnSpeak.innerHTML='&#128266; '+t('readAloud')}

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
    localStorage.setItem('serverTts','true');
    selectedVoice='';
    localStorage.setItem('voice','');
    voiceManuallySet=true; /* user explicitly chose cloud */
  }else{
    serverTtsActive=false;
    localStorage.setItem('serverTts','false');
    selectedVoice=val;
    localStorage.setItem('voice',val);
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
  localStorage.setItem('speak',speakEnabled);
  LOG('toggleSpeak → '+speakEnabled);
  if(speakEnabled){btnSpeak.classList.add('active');btnSpeak.innerHTML='&#128266; '+t('readAloud')}
  else{btnSpeak.classList.remove('active');btnSpeak.innerHTML='&#128264; '+t('readAloud');synth.cancel();clearTtsQueue()}
}

function speak(text){
  if(!speakEnabled||!synth||!text){LOG('speak SKIP: enabled='+speakEnabled+' synth='+!!synth+' text='+(text?text.substring(0,30):'(empty)'));return}
  if(serverTtsActive){LOG('speak SKIP: serverTts active');return}
  LOG('speak: "'+text.substring(0,50)+'"');
  var utter=new SpeechSynthesisUtterance(text);
  utter.rate=speechRate;
  if(selectedVoice){var voices=synth.getVoices();for(var i=0;i<voices.length;i++){if(voices[i].name===selectedVoice){utter.voice=voices[i];break}}}
  synth.speak(utter);
}

/* ── Server TTS (hybrid playback) ── */
var serverTtsActive=localStorage.getItem('serverTts')==='true';
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
  var transLang=localStorage.getItem('transLang')||'';
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
  LOG('handleTtsMessage: url='+msg.url+' speakEnabled='+speakEnabled+' useServer='+useServerTts());
  if(!speakEnabled)return;
  if(!useServerTts())return;
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
function addCommitted(text,lang,time,refs){
  var el;
  if(currentEl){el=currentEl;currentEl=null;
    if(scrollMode==='down'&&el.parentNode){el.parentNode.removeChild(el);insertLine(el)}
  }else{el=document.createElement('div');insertLine(el)}
  var tag=buildTag(lang,time);
  if(refs&&refs.length>0){
    el.innerHTML='';
    if(tag){var tagSpan=document.createElement('span');tagSpan.textContent=tag;el.appendChild(tagSpan)}
    renderTextWithRefs(el,text,refs);
  }else{
    el.textContent=tag+text;
  }
  el.className='line';
  el.style.fontSize=fontSize+'px';el.style.fontFamily=fontFamily;el.style.fontWeight=isBold?'bold':'normal';
  el.style.color='#ffdd57';
  el.dataset.highlighted='1';
  setTimeout(function(){el.style.color=textColor;delete el.dataset.highlighted},5000);
  autoScroll();
  speak(text);
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
function setTransLang(lang){
  LOG('setTransLang: '+lang);
  localStorage.setItem('transLang',lang);
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
  ws.onopen=function(){LOG('WS connected');statusEl.textContent=t('connected');statusEl.className='connected';
    if(currentEl){currentEl.remove();currentEl=null}
    var lang=localStorage.getItem('transLang')||'';
    ws.send(JSON.stringify({type:'setLanguage',language:lang,lastId:lastCommitId}));
  };
  ws.onclose=function(){LOG('WS closed');statusEl.textContent=t('disconnected');statusEl.className='disconnected';wsRef=null;setTimeout(connect,2000)};
  ws.onerror=function(){LOG('WS error');ws.close()};
  ws.onmessage=function(e){
    try{var msg=JSON.parse(e.data);
      if(msg.type==='commit'){
        var id=msg.id||0;
        if(id>lastCommitId){lastCommitId=id;addCommitted(msg.text,msg.lang||'',msg.time||'',msg.refs||null)}
      }
      else if(msg.type==='update')updateCurrent(msg.text);
      else if(msg.type==='clear'){LOG('WS clear');if(currentEl){currentEl.remove();currentEl=null}while(lines.children.length>1)lines.removeChild(lines.children[1]);lastCommitId=0;clearTtsQueue();autoScroll()}
      else if(msg.type==='tts'){handleTtsMessage(msg)}
      else if(msg.type==='pong'){}
      else{LOG('WS unknown msg type: '+msg.type)}
    }catch(ex){LOG('WS parse error: '+ex)}
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
  var tl=localStorage.getItem('transLang')||'';
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

function initPushToTalk(){
  var roomMatch=location.search.match(/[?&]room=([^&]+)/);
  if(!roomMatch)return;
  var roomId=roomMatch[1];
  /* Fetch room info to check type */
  var xhr=new XMLHttpRequest();
  xhr.open('GET','/api/rooms/'+encodeURIComponent(roomId),true);
  xhr.onload=function(){
    if(xhr.status===200){
      try{
        var room=JSON.parse(xhr.responseText);
        pttRoomType=room.type||'';
        if(pttRoomType==='conversation'){
          createPttButton();
        }
      }catch(e){LOG('Room info parse error: '+e)}
    }
  };
  xhr.send();
}

function createPttButton(){
  var btn=document.createElement('div');
  btn.id='ptt-btn';
  btn.style.cssText='position:fixed;bottom:24px;left:50%;transform:translateX(-50%);width:72px;height:72px;border-radius:50%;background:#7c9cf7;display:flex;align-items:center;justify-content:center;cursor:pointer;z-index:90;box-shadow:0 4px 16px rgba(0,0,0,0.4);user-select:none;-webkit-user-select:none;touch-action:none;transition:background 0.15s,transform 0.15s';
  btn.innerHTML='<svg width="32" height="32" viewBox="0 0 24 24" fill="white"><path d="M12 14c1.66 0 3-1.34 3-3V5c0-1.66-1.34-3-3-3S9 3.34 9 5v6c0 1.66 1.34 3 3 3zm-1-9c0-.55.45-1 1-1s1 .45 1 1v6c0 .55-.45 1-1 1s-1-.45-1-1V5z"/><path d="M17 11c0 2.76-2.24 5-5 5s-5-2.24-5-5H5c0 3.53 2.61 6.43 6 6.92V21h2v-3.08c3.39-.49 6-3.39 6-6.92h-2z"/></svg>';
  document.body.appendChild(btn);

  var label=document.createElement('div');
  label.id='ptt-label';
  label.style.cssText='position:fixed;bottom:104px;left:50%;transform:translateX(-50%);color:#aaa;font-size:12px;z-index:90;text-align:center;pointer-events:none;opacity:0.8';
  label.textContent='Hold to speak';
  document.body.appendChild(label);

  /* Touch events for mobile */
  btn.addEventListener('touchstart',function(e){e.preventDefault();startRecording(btn,label)},false);
  btn.addEventListener('touchend',function(e){e.preventDefault();stopRecording(btn,label)},false);
  btn.addEventListener('touchcancel',function(e){e.preventDefault();cancelRecording(btn,label)},false);
  /* Mouse events for desktop testing */
  btn.addEventListener('mousedown',function(e){e.preventDefault();startRecording(btn,label)},false);
  btn.addEventListener('mouseup',function(e){e.preventDefault();stopRecording(btn,label)},false);
  btn.addEventListener('mouseleave',function(e){if(pttActive)cancelRecording(btn,label)},false);
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
  btn.style.transform='translateX(-50%) scale(1.15)';
  label.textContent='Recording...';

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
      pttRecorder.start(100); /* collect in 100ms chunks */
    })
    .catch(function(err){
      LOG('Mic access denied: '+err);
      pttActive=false;
      btn.style.background='#7c9cf7';
      btn.style.transform='translateX(-50%)';
      label.textContent='Mic access denied';
      setTimeout(function(){label.textContent='Hold to speak'},2000);
    });
}

function stopRecording(btn,label){
  if(!pttActive)return;
  pttActive=false;
  btn.style.background='#7c9cf7';
  btn.style.transform='translateX(-50%)';
  label.textContent='Sending...';
  if(pttRecorder&&pttRecorder.state==='recording'){
    pttRecorder.stop();
  }
  setTimeout(function(){label.textContent='Hold to speak'},2000);
}

function cancelRecording(btn,label){
  pttActive=false;
  pttChunks=[];
  btn.style.background='#7c9cf7';
  btn.style.transform='translateX(-50%)';
  label.textContent='Hold to speak';
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

/* Init PTT after page load */
initPushToTalk();

/* Chapter counts by book (standard Protestant canon) */
function getBookChapterCount(book){
  var counts={Gen:50,Exod:40,Lev:27,Num:36,Deut:34,Josh:24,Judg:21,Ruth:4,'1Sam':31,'2Sam':24,'1Kgs':22,'2Kgs':25,'1Chr':29,'2Chr':36,Ezra:10,Neh:13,Esth:10,Job:42,Ps:150,Prov:31,Eccl:12,Song:8,Isa:66,Jer:52,Lam:5,Ezek:48,Dan:12,Hos:14,Joel:3,Amos:9,Obad:1,Jonah:4,Mic:7,Nah:3,Hab:3,Zeph:3,Hag:2,Zech:14,Mal:4,Matt:28,Mark:16,Luke:24,John:21,Acts:28,Rom:16,'1Cor':16,'2Cor':13,Gal:6,Eph:6,Phil:4,Col:4,'1Thess':5,'2Thess':3,'1Tim':6,'2Tim':4,Titus:3,Phlm:1,Heb:13,Jas:5,'1Pet':5,'2Pet':3,'1John':5,'2John':1,'3John':1,Jude:1,Rev:22};
  return counts[book]||50;
}
