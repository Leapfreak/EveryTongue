/* Transcription Tools - Subtitle Client
   ES5 compatible (except async/await for Wake Lock API) */

/* ── i18n ── */
var T={};
(function(){
  var lang=(navigator.language||'en').toLowerCase();
  var lc=lang.split('-')[0];
  var tr={
    en:{connecting:'Connecting...',connected:'Connected',disconnected:'Disconnected - reconnecting...',
        wakeTitle:'Keep Screen On',wakeDesc:'A secure connection is needed (one-time setup):',
        stepTap:'Tap the button below',stepWarn:'You will see a warning page \u2014 this is normal',
        stepAdv:'Tap "Advanced"',stepProceed:'Tap "Proceed to {0}"',
        stepAccept:'Tap "Accept the Risk and Continue"',
        stepDetails:'Tap "Show Details"',stepVisit:'Tap "visit this website"',
        stepRetry:'Tap the screen wake button again',
        openSecure:'Open Secure Page',cancel:'Cancel',
        sending:'Sending...',cmdSent:' command sent',cmdFail:'Failed to send command',
        liveRun:'Live: RUNNING',simRun:'Simulation: RUNNING',stopped:'Status: STOPPED',
        noServer:'Unable to reach server',checking:'Checking...',
        dfltVoice:'Default',title:'Live Subtitles',
        bold:'Bold',font:'Font',style:'Style',voice:'Voice',speed:'Speed',color:'Text Color',
        slow:'Slow',normal:'Normal',fast:'Fast',vfast:'Very Fast',
        start:'Start',stop:'Stop',restart:'Restart',simulate:'Simulate',clear:'Clear',
        saveTranscript:'Save Transcript',transLang:'Translation',remote:'Remote Control',settings:'Settings',readAloud:'Read aloud',keepScreen:'Keep screen on',scrollDir:'Scroll Direction',scrollUp:'Bottom-up (newest at bottom)',scrollDown:'Top-down (newest at top)',tags:'Tags',tagOff:'Off',tagLang:'Language',tagTime:'Time',tagBoth:'Language + Time',bible:'Bible',bibleOT:'Old Testament',bibleNT:'New Testament',bibleSearch:'Search',bibleNoResults:'No results found',bibleSelectTrans:'Select a translation',serverTts:'Server TTS',ttsBehind:'{0} behind \u2014 tap to skip',readAll:'Read All',readVerse:'Read',chooseLang:'Choose your language',lpPopular:'Popular',lpAll:'All Languages',searchLangs:'Search languages...',noTranslation:'No translation',browseAll:'Browse All',adminLabel:'Administrator',adminPin:'PIN',adminBad:'Invalid PIN',adminOk:'Admin access granted'},
    es:{connecting:'Conectando...',connected:'Conectado',disconnected:'Desconectado - reconectando...',
        wakeTitle:'Mantener Pantalla',wakeDesc:'Se necesita conexi\u00f3n segura (configuraci\u00f3n \u00fanica):',
        stepTap:'Toca el bot\u00f3n de abajo',stepWarn:'Ver\u00e1s una advertencia \u2014 es normal',
        stepAdv:'Toca "Avanzado"',stepProceed:'Toca "Continuar a {0}"',
        stepAccept:'Toca "Aceptar el riesgo y continuar"',
        stepDetails:'Toca "Mostrar detalles"',stepVisit:'Toca "visitar este sitio web"',
        stepRetry:'Toca el bot\u00f3n de pantalla de nuevo',
        openSecure:'Abrir P\u00e1gina Segura',cancel:'Cancelar',
        sending:'Enviando...',cmdSent:' comando enviado',cmdFail:'Error al enviar comando',
        liveRun:'En vivo: ACTIVO',simRun:'Simulaci\u00f3n: ACTIVA',stopped:'Estado: DETENIDO',
        noServer:'No se puede conectar',checking:'Comprobando...',
        dfltVoice:'Predeterminado',title:'Subt\u00edtulos en Vivo',
        bold:'Negrita',font:'Fuente',style:'Estilo',voice:'Voz',speed:'Velocidad',color:'Color de texto',
        slow:'Lento',normal:'Normal',fast:'R\u00e1pido',vfast:'Muy R\u00e1pido',
        start:'Iniciar',stop:'Detener',restart:'Reiniciar',simulate:'Simular',clear:'Limpiar',
        saveTranscript:'Guardar Transcripci\u00f3n',transLang:'Traducci\u00f3n',remote:'Control Remoto',settings:'Ajustes',readAloud:'Leer en voz alta',keepScreen:'Mantener pantalla',scrollDir:'Direcci\u00f3n de desplazamiento',scrollUp:'Abajo-arriba (reciente abajo)',scrollDown:'Arriba-abajo (reciente arriba)',tags:'Etiquetas',tagOff:'Desactivado',tagLang:'Idioma',tagTime:'Hora',tagBoth:'Idioma + Hora',bible:'Biblia',bibleOT:'Antiguo Testamento',bibleNT:'Nuevo Testamento',bibleSearch:'Buscar',bibleNoResults:'Sin resultados',bibleSelectTrans:'Selecciona una traducci\u00f3n',serverTts:'TTS del servidor',ttsBehind:'{0} atr\u00e1s \u2014 toca para saltar',readAll:'Leer todo',readVerse:'Leer',chooseLang:'Elige tu idioma',lpPopular:'Populares',lpAll:'Todos los idiomas',searchLangs:'Buscar idiomas...',noTranslation:'Sin traducci\u00f3n',browseAll:'Ver todos',adminLabel:'Administrador',adminPin:'PIN',adminBad:'PIN incorrecto',adminOk:'Acceso de administrador concedido'},
    fr:{connecting:'Connexion...',connected:'Connect\u00e9',disconnected:'D\u00e9connect\u00e9 - reconnexion...',
        wakeTitle:'\u00c9cran Allum\u00e9',wakeDesc:'Connexion s\u00e9curis\u00e9e requise (une seule fois) :',
        stepTap:'Appuyez sur le bouton ci-dessous',stepWarn:'Un avertissement s\'affichera \u2014 c\'est normal',
        stepAdv:'Appuyez sur "Avanc\u00e9"',stepProceed:'Appuyez sur "Continuer vers {0}"',
        stepAccept:'Appuyez sur "Accepter le risque"',
        stepDetails:'Appuyez sur "Afficher les d\u00e9tails"',stepVisit:'Appuyez sur "acc\u00e9der \u00e0 ce site"',
        stepRetry:'Appuyez \u00e0 nouveau sur le bouton veille',
        openSecure:'Ouvrir Page S\u00e9curis\u00e9e',cancel:'Annuler',
        sending:'Envoi...',cmdSent:' commande envoy\u00e9e',cmdFail:'Erreur d\'envoi',
        liveRun:'En direct : ACTIF',simRun:'Simulation : ACTIVE',stopped:'\u00c9tat : ARR\u00caT\u00c9',
        noServer:'Serveur inaccessible',checking:'V\u00e9rification...',
        dfltVoice:'Par d\u00e9faut',title:'Sous-titres en Direct',
        bold:'Gras',font:'Police',style:'Style',voice:'Voix',speed:'Vitesse',color:'Couleur du texte',
        slow:'Lent',normal:'Normal',fast:'Rapide',vfast:'Tr\u00e8s Rapide',
        start:'D\u00e9marrer',stop:'Arr\u00eater',restart:'Red\u00e9marrer',simulate:'Simuler',clear:'Effacer',
        saveTranscript:'Enregistrer',transLang:'Traduction',remote:'T\u00e9l\u00e9commande',settings:'Param\u00e8tres',readAloud:'Lire \u00e0 voix haute',keepScreen:'\u00c9cran allum\u00e9',scrollDir:'Direction du d\u00e9filement',scrollUp:'Bas en haut (r\u00e9cent en bas)',scrollDown:'Haut en bas (r\u00e9cent en haut)',tags:'\u00c9tiquettes',tagOff:'D\u00e9sactiv\u00e9',tagLang:'Langue',tagTime:'Heure',tagBoth:'Langue + Heure',bible:'Bible',bibleOT:'Old Testament',bibleNT:'New Testament',bibleSearch:'Search',bibleNoResults:'No results found',bibleSelectTrans:'Select a translation',serverTts:'TTS serveur',ttsBehind:'{0} en retard \u2014 appuyez pour sauter',readAll:'Tout lire',readVerse:'Lire',chooseLang:'Choisissez votre langue',lpPopular:'Populaires',lpAll:'Toutes les langues',searchLangs:'Rechercher...',noTranslation:'Pas de traduction',browseAll:'Parcourir tout',adminLabel:'Administrateur',adminPin:'PIN',adminBad:'PIN incorrect',adminOk:'Acc\u00e8s admin accord\u00e9'},
    de:{connecting:'Verbinde...',connected:'Verbunden',disconnected:'Getrennt - verbinde erneut...',
        wakeTitle:'Bildschirm An',wakeDesc:'Sichere Verbindung erforderlich (einmalig):',
        stepTap:'Tippen Sie auf den Button unten',stepWarn:'Sie sehen eine Warnung \u2014 das ist normal',
        stepAdv:'Tippen Sie auf "Erweitert"',stepProceed:'Tippen Sie auf "Weiter zu {0}"',
        stepAccept:'Tippen Sie auf "Risiko akzeptieren"',
        stepDetails:'Tippen Sie auf "Details anzeigen"',stepVisit:'Tippen Sie auf "Website besuchen"',
        stepRetry:'Tippen Sie erneut auf den Wach-Button',
        openSecure:'Sichere Seite \u00d6ffnen',cancel:'Abbrechen',
        sending:'Sende...',cmdSent:' Befehl gesendet',cmdFail:'Befehl fehlgeschlagen',
        liveRun:'Live: L\u00c4UFT',simRun:'Simulation: L\u00c4UFT',stopped:'Status: GESTOPPT',
        noServer:'Server nicht erreichbar',checking:'Pr\u00fcfe...',
        dfltVoice:'Standard',title:'Live-Untertitel',
        bold:'Fett',font:'Schrift',style:'Stil',voice:'Stimme',speed:'Geschwindigkeit',color:'Textfarbe',
        slow:'Langsam',normal:'Normal',fast:'Schnell',vfast:'Sehr Schnell',
        start:'Starten',stop:'Stoppen',restart:'Neustarten',simulate:'Simulieren',clear:'L\u00f6schen',
        saveTranscript:'Speichern',transLang:'\u00dcbersetzung',remote:'Fernsteuerung',settings:'Einstellungen',readAloud:'Vorlesen',keepScreen:'Bildschirm an',scrollDir:'Scrollrichtung',scrollUp:'Unten nach oben (neueste unten)',scrollDown:'Oben nach unten (neueste oben)',tags:'Tags',tagOff:'Aus',tagLang:'Sprache',tagTime:'Zeit',tagBoth:'Sprache + Zeit',bible:'Bibel',bibleOT:'Altes Testament',bibleNT:'Neues Testament',bibleSearch:'Suchen',bibleNoResults:'Keine Ergebnisse',bibleSelectTrans:'W\u00e4hle eine \u00dcbersetzung',serverTts:'Server-TTS',ttsBehind:'{0} zur\u00fcck \u2014 tippen zum \u00fcberspringen',readAll:'Alles vorlesen',readVerse:'Vorlesen',chooseLang:'W\u00e4hle deine Sprache',lpPopular:'Beliebt',lpAll:'Alle Sprachen',searchLangs:'Sprachen suchen...',noTranslation:'Keine \u00dcbersetzung',browseAll:'Alle durchsuchen',adminLabel:'Administrator',adminPin:'PIN',adminBad:'Falscher PIN',adminOk:'Adminzugang gew\u00e4hrt'},
    ca:{connecting:'Connectant...',connected:'Connectat',disconnected:'Desconnectat - reconnectant...',
        wakeTitle:'Mantenir Pantalla',wakeDesc:'Cal connexi\u00f3 segura (configuraci\u00f3 \u00fanica):',
        stepTap:'Toca el bot\u00f3 de sota',stepWarn:'Veur\u00e0s un av\u00eds \u2014 \u00e9s normal',
        stepAdv:'Toca "Avan\u00e7at"',stepProceed:'Toca "Continuar a {0}"',
        stepAccept:'Toca "Acceptar el risc i continuar"',
        stepDetails:'Toca "Mostrar detalls"',stepVisit:'Toca "visitar aquest lloc"',
        stepRetry:'Toca el bot\u00f3 de pantalla de nou',
        openSecure:'Obrir P\u00e0gina Segura',cancel:'Cancel\u00b7lar',
        sending:'Enviant...',cmdSent:' comanda enviada',cmdFail:'Error en enviar',
        liveRun:'En directe: ACTIU',simRun:'Simulaci\u00f3: ACTIVA',stopped:'Estat: ATURAT',
        noServer:'No es pot connectar',checking:'Comprovant...',
        dfltVoice:'Per defecte',title:'Subt\u00edtols en Directe',
        bold:'Negreta',font:'Tipus de lletra',style:'Estil',voice:'Veu',speed:'Velocitat',color:'Color del text',
        slow:'Lent',normal:'Normal',fast:'R\u00e0pid',vfast:'Molt R\u00e0pid',
        start:'Iniciar',stop:'Aturar',restart:'Reiniciar',simulate:'Simular',clear:'Netejar',
        saveTranscript:'Desar Transcripci\u00f3',transLang:'Traducci\u00f3',remote:'Control Remot',settings:'Ajustos',readAloud:'Llegir en veu alta',keepScreen:'Mantenir pantalla',scrollDir:'Direcci\u00f3 de despla\u00e7ament',scrollUp:'Baix a dalt (recent a baix)',scrollDown:'Dalt a baix (recent a dalt)',tags:'Etiquetes',tagOff:'Desactivat',tagLang:'Idioma',tagTime:'Hora',tagBoth:'Idioma + Hora',bible:'B\u00edblia',bibleOT:'Antic Testament',bibleNT:'Nou Testament',bibleSearch:'Cercar',bibleNoResults:'Sense resultats',bibleSelectTrans:'Selecciona una traducci\u00f3',serverTts:'TTS del servidor',ttsBehind:'{0} endarrere \u2014 toca per saltar',readAll:'Llegir-ho tot',readVerse:'Llegir',chooseLang:'Tria el teu idioma',lpPopular:'Populars',lpAll:'Tots els idiomes',searchLangs:'Cercar idiomes...',noTranslation:'Sense traducci\u00f3',browseAll:'Veure tots',adminLabel:'Administrador',adminPin:'PIN',adminBad:'PIN incorrecte',adminOk:'Acc\u00e9s d\'administrador concedit'},
    pt:{connecting:'Conectando...',connected:'Conectado',disconnected:'Desconectado - reconectando...',
        wakeTitle:'Manter Tela Ligada',wakeDesc:'Conex\u00e3o segura necess\u00e1ria (apenas uma vez):',
        stepTap:'Toque no bot\u00e3o abaixo',stepWarn:'Voc\u00ea ver\u00e1 um aviso \u2014 isso \u00e9 normal',
        stepAdv:'Toque em "Avan\u00e7ado"',stepProceed:'Toque em "Prosseguir para {0}"',
        stepAccept:'Toque em "Aceitar o risco e continuar"',
        stepDetails:'Toque em "Mostrar detalhes"',stepVisit:'Toque em "visitar este site"',
        stepRetry:'Toque no bot\u00e3o de tela novamente',
        openSecure:'Abrir P\u00e1gina Segura',cancel:'Cancelar',
        sending:'Enviando...',cmdSent:' comando enviado',cmdFail:'Falha ao enviar',
        liveRun:'Ao vivo: ATIVO',simRun:'Simula\u00e7\u00e3o: ATIVA',stopped:'Status: PARADO',
        noServer:'N\u00e3o foi poss\u00edvel conectar',checking:'Verificando...',
        dfltVoice:'Padr\u00e3o',title:'Legendas ao Vivo',
        bold:'Negrito',font:'Fonte',style:'Estilo',voice:'Voz',speed:'Velocidade',color:'Cor do texto',
        slow:'Lento',normal:'Normal',fast:'R\u00e1pido',vfast:'Muito R\u00e1pido',
        start:'Iniciar',stop:'Parar',restart:'Reiniciar',simulate:'Simular',clear:'Limpar',
        saveTranscript:'Salvar Transcri\u00e7\u00e3o',transLang:'Tradu\u00e7\u00e3o',remote:'Controle Remoto',settings:'Configura\u00e7\u00f5es',readAloud:'Ler em voz alta',keepScreen:'Manter tela ligada',scrollDir:'Dire\u00e7\u00e3o de rolagem',scrollUp:'Baixo para cima (recente embaixo)',scrollDown:'Cima para baixo (recente em cima)',tags:'Etiquetas',tagOff:'Desativado',tagLang:'Idioma',tagTime:'Hora',tagBoth:'Idioma + Hora',bible:'B\u00edblia',bibleOT:'Antigo Testamento',bibleNT:'Novo Testamento',bibleSearch:'Pesquisar',bibleNoResults:'Sem resultados',bibleSelectTrans:'Selecione uma tradu\u00e7\u00e3o',serverTts:'TTS do servidor',ttsBehind:'{0} atr\u00e1s \u2014 toque para pular',readAll:'Ler tudo',readVerse:'Ler',chooseLang:'Escolha seu idioma',lpPopular:'Populares',lpAll:'Todos os idiomas',searchLangs:'Pesquisar idiomas...',noTranslation:'Sem tradu\u00e7\u00e3o',browseAll:'Ver todos',adminLabel:'Administrador',adminPin:'PIN',adminBad:'PIN incorreto',adminOk:'Acesso de admin concedido'},
    ja:{connecting:'\u63a5\u7d9a\u4e2d...',connected:'\u63a5\u7d9a\u6e08\u307f',disconnected:'\u5207\u65ad - \u518d\u63a5\u7d9a\u4e2d...',
        wakeTitle:'\u753b\u9762\u3092\u70b9\u706f',wakeDesc:'\u5b89\u5168\u306a\u63a5\u7d9a\u304c\u5fc5\u8981\u3067\u3059\uff08\u521d\u56de\u306e\u307f\uff09:',
        stepTap:'\u4e0b\u306e\u30dc\u30bf\u30f3\u3092\u30bf\u30c3\u30d7',stepWarn:'\u8b66\u544a\u304c\u8868\u793a\u3055\u308c\u307e\u3059 \u2014 \u6b63\u5e38\u3067\u3059',
        stepAdv:'"\u8a73\u7d30\u8a2d\u5b9a"\u3092\u30bf\u30c3\u30d7',stepProceed:'"{0}\u306b\u30a2\u30af\u30bb\u30b9"\u3092\u30bf\u30c3\u30d7',
        stepAccept:'"\u30ea\u30b9\u30af\u3092\u627f\u8afe\u3057\u3066\u7d9a\u884c"\u3092\u30bf\u30c3\u30d7',
        stepDetails:'"\u8a73\u7d30\u3092\u8868\u793a"\u3092\u30bf\u30c3\u30d7',stepVisit:'"\u3053\u306e\u30b5\u30a4\u30c8\u3092\u8a2a\u554f"\u3092\u30bf\u30c3\u30d7',
        stepRetry:'\u753b\u9762\u70b9\u706f\u30dc\u30bf\u30f3\u3092\u518d\u5ea6\u30bf\u30c3\u30d7',
        openSecure:'\u5b89\u5168\u306a\u30da\u30fc\u30b8\u3092\u958b\u304f',cancel:'\u30ad\u30e3\u30f3\u30bb\u30eb',
        sending:'\u9001\u4fe1\u4e2d...',cmdSent:'\u30b3\u30de\u30f3\u30c9\u9001\u4fe1\u6e08\u307f',cmdFail:'\u30b3\u30de\u30f3\u30c9\u9001\u4fe1\u5931\u6557',
        liveRun:'\u30e9\u30a4\u30d6: \u5b9f\u884c\u4e2d',simRun:'\u30b7\u30df\u30e5\u30ec\u30fc\u30b7\u30e7\u30f3: \u5b9f\u884c\u4e2d',stopped:'\u30b9\u30c6\u30fc\u30bf\u30b9: \u505c\u6b62',
        noServer:'\u30b5\u30fc\u30d0\u30fc\u306b\u63a5\u7d9a\u3067\u304d\u307e\u305b\u3093',checking:'\u78ba\u8a8d\u4e2d...',
        dfltVoice:'\u30c7\u30d5\u30a9\u30eb\u30c8',title:'\u30e9\u30a4\u30d6\u5b57\u5e55',
        bold:'\u592a\u5b57',font:'\u30d5\u30a9\u30f3\u30c8',style:'\u30b9\u30bf\u30a4\u30eb',voice:'\u97f3\u58f0',speed:'\u901f\u5ea6',color:'\u6587\u5b57\u8272',
        slow:'\u9045\u3044',normal:'\u666e\u901a',fast:'\u901f\u3044',vfast:'\u3068\u3066\u3082\u901f\u3044',
        start:'\u958b\u59cb',stop:'\u505c\u6b62',restart:'\u518d\u958b',simulate:'\u30b7\u30df\u30e5\u30ec\u30fc\u30b7\u30e7\u30f3',clear:'\u30af\u30ea\u30a2',
        saveTranscript:'\u4fdd\u5b58',transLang:'\u7ffb\u8a33',remote:'\u30ea\u30e2\u30fc\u30c8',settings:'\u8a2d\u5b9a',readAloud:'\u8aad\u307f\u4e0a\u3052',keepScreen:'\u753b\u9762\u70b9\u706f',scrollDir:'\u30b9\u30af\u30ed\u30fc\u30eb\u65b9\u5411',scrollUp:'\u4e0b\u304b\u3089\u4e0a (\u6700\u65b0\u304c\u4e0b)',scrollDown:'\u4e0a\u304b\u3089\u4e0b (\u6700\u65b0\u304c\u4e0a)',tags:'\u30bf\u30b0',tagOff:'\u30aa\u30d5',tagLang:'\u8a00\u8a9e',tagTime:'\u6642\u523b',tagBoth:'\u8a00\u8a9e + \u6642\u523b',bible:'\u8056\u66f8',bibleOT:'\u65e7\u7d04\u8056\u66f8',bibleNT:'\u65b0\u7d04\u8056\u66f8',bibleSearch:'\u691c\u7d22',bibleNoResults:'\u7d50\u679c\u306a\u3057',bibleSelectTrans:'\u7ffb\u8a33\u3092\u9078\u629e',serverTts:'\u30b5\u30fc\u30d0\u30fcTTS',ttsBehind:'{0}\u904e\u53bb \u2014 \u30bf\u30c3\u30d7\u3067\u30b9\u30ad\u30c3\u30d7',readAll:'\u3059\u3079\u3066\u8aad\u3080',readVerse:'\u8aad\u3080',chooseLang:'\u8a00\u8a9e\u3092\u9078\u629e',lpPopular:'\u4eba\u6c17',lpAll:'\u3059\u3079\u3066\u306e\u8a00\u8a9e',searchLangs:'\u8a00\u8a9e\u3092\u691c\u7d22...',noTranslation:'\u7ffb\u8a33\u306a\u3057',browseAll:'\u3059\u3079\u3066\u8868\u793a',adminLabel:'\u7ba1\u7406\u8005',adminPin:'PIN',adminBad:'PIN\u304c\u9055\u3044\u307e\u3059',adminOk:'\u7ba1\u7406\u8005\u30a2\u30af\u30bb\u30b9\u8a31\u53ef'},
    zh:{connecting:'\u8fde\u63a5\u4e2d...',connected:'\u5df2\u8fde\u63a5',disconnected:'\u5df2\u65ad\u5f00 - \u91cd\u65b0\u8fde\u63a5...',
        wakeTitle:'\u4fdd\u6301\u5c4f\u5e55\u5e38\u4eae',wakeDesc:'\u9700\u8981\u5b89\u5168\u8fde\u63a5\uff08\u4ec5\u9700\u4e00\u6b21\uff09:',
        stepTap:'\u70b9\u51fb\u4e0b\u65b9\u6309\u94ae',stepWarn:'\u60a8\u5c06\u770b\u5230\u8b66\u544a\u9875\u9762 \u2014 \u8fd9\u662f\u6b63\u5e38\u7684',
        stepAdv:'\u70b9\u51fb"\u9ad8\u7ea7"',stepProceed:'\u70b9\u51fb"\u7ee7\u7eed\u8bbf\u95ee{0}"',
        stepAccept:'\u70b9\u51fb"\u63a5\u53d7\u98ce\u9669\u5e76\u7ee7\u7eed"',
        stepDetails:'\u70b9\u51fb"\u663e\u793a\u8be6\u60c5"',stepVisit:'\u70b9\u51fb"\u8bbf\u95ee\u6b64\u7f51\u7ad9"',
        stepRetry:'\u518d\u6b21\u70b9\u51fb\u5c4f\u5e55\u5e38\u4eae\u6309\u94ae',
        openSecure:'\u6253\u5f00\u5b89\u5168\u9875\u9762',cancel:'\u53d6\u6d88',
        sending:'\u53d1\u9001\u4e2d...',cmdSent:'\u547d\u4ee4\u5df2\u53d1\u9001',cmdFail:'\u53d1\u9001\u5931\u8d25',
        liveRun:'\u76f4\u64ad: \u8fd0\u884c\u4e2d',simRun:'\u6a21\u62df: \u8fd0\u884c\u4e2d',stopped:'\u72b6\u6001: \u5df2\u505c\u6b62',
        noServer:'\u65e0\u6cd5\u8fde\u63a5\u670d\u52a1\u5668',checking:'\u68c0\u67e5\u4e2d...',
        dfltVoice:'\u9ed8\u8ba4',title:'\u5b9e\u65f6\u5b57\u5e55',
        bold:'\u7c97\u4f53',font:'\u5b57\u4f53',style:'\u6837\u5f0f',voice:'\u8bed\u97f3',speed:'\u901f\u5ea6',color:'\u6587\u5b57\u989c\u8272',
        slow:'\u6162',normal:'\u6b63\u5e38',fast:'\u5feb',vfast:'\u975e\u5e38\u5feb',
        start:'\u5f00\u59cb',stop:'\u505c\u6b62',restart:'\u91cd\u542f',simulate:'\u6a21\u62df',clear:'\u6e05\u9664',
        saveTranscript:'\u4fdd\u5b58',transLang:'\u7ffb\u8bd1',remote:'\u8fdc\u7a0b\u63a7\u5236',settings:'\u8bbe\u7f6e',readAloud:'\u6717\u8bfb',keepScreen:'\u4fdd\u6301\u5c4f\u5e55',scrollDir:'\u6eda\u52a8\u65b9\u5411',scrollUp:'\u4ece\u4e0b\u5f80\u4e0a (\u6700\u65b0\u5728\u4e0b)',scrollDown:'\u4ece\u4e0a\u5f80\u4e0b (\u6700\u65b0\u5728\u4e0a)',tags:'\u6807\u7b7e',tagOff:'\u5173\u95ed',tagLang:'\u8bed\u8a00',tagTime:'\u65f6\u95f4',tagBoth:'\u8bed\u8a00 + \u65f6\u95f4',bible:'\u5723\u7ecf',bibleOT:'\u65e7\u7ea6',bibleNT:'\u65b0\u7ea6',bibleSearch:'\u641c\u7d22',bibleNoResults:'\u6ca1\u6709\u7ed3\u679c',bibleSelectTrans:'\u9009\u62e9\u8bd1\u672c',serverTts:'\u670d\u52a1\u5668TTS',ttsBehind:'{0}\u6761\u843d\u540e \u2014 \u70b9\u51fb\u8df3\u8fc7',readAll:'\u5168\u90e8\u6717\u8bfb',readVerse:'\u6717\u8bfb',chooseLang:'\u9009\u62e9\u4f60\u7684\u8bed\u8a00',lpPopular:'\u70ed\u95e8',lpAll:'\u6240\u6709\u8bed\u8a00',searchLangs:'\u641c\u7d22\u8bed\u8a00...',noTranslation:'\u4e0d\u7ffb\u8bd1',browseAll:'\u6d4f\u89c8\u5168\u90e8',adminLabel:'\u7ba1\u7406\u5458',adminPin:'PIN',adminBad:'PIN\u9519\u8bef',adminOk:'\u7ba1\u7406\u5458\u8bbf\u95ee\u5df2\u6388\u6743'}
  };
  if(lang.indexOf('zh')===0)T=tr.zh;
  else T=tr[lc]||tr.en;
})();
function t(k){return T[k]||k}

/* ── Language data: [nllbCode, nativeName, englishName, bcp47Prefix] ── */
var LANGS=[
['afr_Latn','Afrikaans','Afrikaans','af'],
['amh_Ethi','\u12A0\u121B\u122D\u129B','Amharic','am'],
['arb_Arab','\u0627\u0644\u0639\u0631\u0628\u064A\u0629','Arabic','ar'],
['hye_Armn','\u0540\u0561\u0575\u0565\u0580\u0565\u0576','Armenian','hy'],
['azj_Latn','Az\u0259rbaycan','Azerbaijani','az'],
['eus_Latn','Euskara','Basque','eu'],
['bel_Cyrl','\u0411\u0435\u043B\u0430\u0440\u0443\u0441\u043A\u0430\u044F','Belarusian','be'],
['ben_Beng','\u09AC\u09BE\u0982\u09B2\u09BE','Bengali','bn'],
['bos_Latn','Bosanski','Bosnian','bs'],
['bul_Cyrl','\u0411\u044A\u043B\u0433\u0430\u0440\u0441\u043A\u0438','Bulgarian','bg'],
['cat_Latn','Catal\u00E0','Catalan','ca'],
['ces_Latn','\u010Ce\u0161tina','Czech','cs'],
['zho_Hans','\u4E2D\u6587','Chinese','zh'],
['hrv_Latn','Hrvatski','Croatian','hr'],
['dan_Latn','Dansk','Danish','da'],
['nld_Latn','Nederlands','Dutch','nl'],
['eng_Latn','English','English','en'],
['est_Latn','Eesti','Estonian','et'],
['fin_Latn','Suomi','Finnish','fi'],
['fra_Latn','Fran\u00E7ais','French','fr'],
['glg_Latn','Galego','Galician','gl'],
['kat_Geor','\u10E5\u10D0\u10E0\u10D7\u10E3\u10DA\u10D8','Georgian','ka'],
['deu_Latn','Deutsch','German','de'],
['ell_Grek','\u0395\u03BB\u03BB\u03B7\u03BD\u03B9\u03BA\u03AC','Greek','el'],
['guj_Gujr','\u0A97\u0AC1\u0A9C\u0AB0\u0ABE\u0AA4\u0AC0','Gujarati','gu'],
['hat_Latn','Krey\u00F2l Ayisyen','Haitian Creole','ht'],
['hau_Latn','Hausa','Hausa','ha'],
['heb_Hebr','\u05E2\u05D1\u05E8\u05D9\u05EA','Hebrew','he'],
['hin_Deva','\u0939\u093F\u0928\u094D\u0926\u0940','Hindi','hi'],
['hun_Latn','Magyar','Hungarian','hu'],
['isl_Latn','\u00CDslenska','Icelandic','is'],
['ind_Latn','Bahasa Indonesia','Indonesian','id'],
['ita_Latn','Italiano','Italian','it'],
['jpn_Jpan','\u65E5\u672C\u8A9E','Japanese','ja'],
['jav_Latn','Basa Jawa','Javanese','jv'],
['kan_Knda','\u0C95\u0CA8\u0CCD\u0CA8\u0CA1','Kannada','kn'],
['kaz_Cyrl','\u049A\u0430\u0437\u0430\u049B','Kazakh','kk'],
['khm_Khmr','\u1781\u17D2\u1798\u17C2\u179A','Khmer','km'],
['kor_Hang','\uD55C\uAD6D\uC5B4','Korean','ko'],
['lao_Laoo','\u0EA5\u0EB2\u0EA7','Lao','lo'],
['lvs_Latn','Latvie\u0161u','Latvian','lv'],
['lit_Latn','Lietuvi\u0173','Lithuanian','lt'],
['ltz_Latn','L\u00EBtzebuergesch','Luxembourgish','lb'],
['mkd_Cyrl','\u041C\u0430\u043A\u0435\u0434\u043E\u043D\u0441\u043A\u0438','Macedonian','mk'],
['zsm_Latn','Bahasa Melayu','Malay','ms'],
['mal_Mlym','\u0D2E\u0D32\u0D2F\u0D3E\u0D33\u0D02','Malayalam','ml'],
['mlt_Latn','Malti','Maltese','mt'],
['mri_Latn','Te Reo M\u0101ori','Maori','mi'],
['mar_Deva','\u092E\u0930\u093E\u0920\u0940','Marathi','mr'],
['khk_Cyrl','\u041C\u043E\u043D\u0433\u043E\u043B','Mongolian','mn'],
['mya_Mymr','\u1019\u103C\u1014\u103A\u1019\u102C','Myanmar','my'],
['npi_Deva','\u0928\u0947\u092A\u093E\u0932\u0940','Nepali','ne'],
['nob_Latn','Norsk','Norwegian','no'],
['pes_Arab','\u0641\u0627\u0631\u0633\u06CC','Persian','fa'],
['pol_Latn','Polski','Polish','pl'],
['por_Latn','Portugu\u00EAs','Portuguese','pt'],
['pan_Guru','\u0A2A\u0A70\u0A1C\u0A3E\u0A2C\u0A40','Punjabi','pa'],
['ron_Latn','Rom\u00E2n\u0103','Romanian','ro'],
['rus_Cyrl','\u0420\u0443\u0441\u0441\u043A\u0438\u0439','Russian','ru'],
['srp_Cyrl','\u0421\u0440\u043F\u0441\u043A\u0438','Serbian','sr'],
['sna_Latn','Shona','Shona','sn'],
['snd_Arab','\u0633\u0646\u068C\u064A','Sindhi','sd'],
['sin_Sinh','\u0DC3\u0DD2\u0D82\u0DC4\u0DBD','Sinhala','si'],
['slk_Latn','Sloven\u010Dina','Slovak','sk'],
['slv_Latn','Sloven\u0161\u010Dina','Slovenian','sl'],
['som_Latn','Soomaali','Somali','so'],
['spa_Latn','Espa\u00F1ol','Spanish','es'],
['sun_Latn','Basa Sunda','Sundanese','su'],
['swh_Latn','Kiswahili','Swahili','sw'],
['swe_Latn','Svenska','Swedish','sv'],
['tgl_Latn','Tagalog','Filipino','tl'],
['tgk_Cyrl','\u0422\u043E\u04B7\u0438\u043A\u04E3','Tajik','tg'],
['tam_Taml','\u0BA4\u0BAE\u0BBF\u0BB4\u0BCD','Tamil','ta'],
['tat_Cyrl','\u0422\u0430\u0442\u0430\u0440','Tatar','tt'],
['tel_Telu','\u0C24\u0C46\u0C32\u0C41\u0C17\u0C41','Telugu','te'],
['tha_Thai','\u0E20\u0E32\u0E29\u0E32\u0E44\u0E17\u0E22','Thai','th'],
['tur_Latn','T\u00FCrk\u00E7e','Turkish','tr'],
['tuk_Latn','T\u00FCrkmen','Turkmen','tk'],
['ukr_Cyrl','\u0423\u043A\u0440\u0430\u0457\u043D\u0441\u044C\u043A\u0430','Ukrainian','uk'],
['urd_Arab','\u0627\u0631\u062F\u0648','Urdu','ur'],
['uzn_Latn','O\u2018zbek','Uzbek','uz'],
['vie_Latn','Ti\u1EBFng Vi\u1EC7t','Vietnamese','vi'],
['cym_Latn','Cymraeg','Welsh','cy'],
['yor_Latn','Yor\u00F9b\u00E1','Yoruba','yo'],
['zul_Latn','isiZulu','Zulu','zu']
];

/* Popular languages shown at top of picker */
var POPULAR_LANGS=['eng_Latn','spa_Latn','fra_Latn','por_Latn','deu_Latn','zho_Hans','arb_Arab','rus_Cyrl','hin_Deva','kor_Hang','jpn_Jpan','ita_Latn'];

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
function pickLang(code){
  localStorage.setItem('transLang',code);
  localStorage.setItem('langChosen','true');
  hideLangPicker();
  setTransLang(code);
  /* sync dropdown */
  var sel=document.getElementById('transLangSelect');
  for(var i=0;i<sel.options.length;i++){if(sel.options[i].value===code){sel.selectedIndex=i;break}}
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
  localStorage.removeItem('langChosen');
  location.reload();
}

/* Populate transLangSelect dropdown dynamically from LANGS */
(function(){
  var sel=document.getElementById('transLangSelect');
  for(var i=0;i<LANGS.length;i++){
    var opt=document.createElement('option');opt.value=LANGS[i][0];
    opt.textContent=LANGS[i][1]+' ('+LANGS[i][2]+')';
    sel.appendChild(opt);
  }
  var saved=localStorage.getItem('transLang')||'';
  for(var j=0;j<sel.options.length;j++){if(sel.options[j].value===saved){sel.selectedIndex=j;break}}
})();

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
  var defOpt=document.createElement('option');defOpt.value='';defOpt.textContent=t('dfltVoice');voiceSelect.appendChild(defOpt);
  for(var i=0;i<voices.length;i++){
    var v=voices[i];
    var opt=document.createElement('option');opt.value=v.name;
    opt.textContent=v.name+(v.lang?' ('+v.lang+')':'');
    if(v.name===selectedVoice)opt.selected=true;
    voiceSelect.appendChild(opt);
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
  if(speakEnabled){btnSpeak.classList.add('active');btnSpeak.innerHTML='&#128266; '+t('readAloud')}
  else{btnSpeak.classList.remove('active');btnSpeak.innerHTML='&#128264; '+t('readAloud');synth.cancel();clearTtsQueue()}
}

function speak(text){
  if(!speakEnabled||!synth||!text)return;
  if(serverTtsActive)return; /* server TTS handles playback via tts message */
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

/* NLLB language code to BCP47 prefix for browser voice matching */
var nllbToBcp47={
  eng_Latn:'en',spa_Latn:'es',fra_Latn:'fr',deu_Latn:'de',cat_Latn:'ca',
  por_Latn:'pt',ita_Latn:'it',jpn_Jpan:'ja',zho_Hans:'zh',kor_Hang:'ko',
  arb_Arab:'ar',hin_Deva:'hi',rus_Cyrl:'ru',nld_Latn:'nl',pol_Latn:'pl',
  tur_Latn:'tr',swe_Latn:'sv',ukr_Cyrl:'uk',vie_Latn:'vi',tha_Thai:'th'
};

function hasBrowserVoiceForLang(){
  var transLang=localStorage.getItem('transLang')||'';
  if(!transLang)return true; /* original language — browser usually has it */
  var bcp=nllbToBcp47[transLang];
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

function toggleServerTts(){
  serverTtsActive=!serverTtsActive;
  localStorage.setItem('serverTts',serverTtsActive);
  var btn=document.getElementById('btnServerTts');
  if(serverTtsActive){btn.classList.add('active')}
  else{btn.classList.remove('active');clearTtsQueue()}
}

function handleTtsMessage(msg){
  if(!speakEnabled)return;
  if(!useServerTts())return;
  synth.cancel(); /* stop any browser TTS */
  enqueueTts(msg.url);
}

function enqueueTts(url){
  ttsQueue.push(url);
  updateTtsSkipIndicator();
  if(!ttsPlaying)playNextTts();
}

function playNextTts(){
  if(ttsQueue.length===0){ttsPlaying=false;updateTtsSkipIndicator();return}
  ttsPlaying=true;
  var url=ttsQueue.shift();
  updateTtsSkipIndicator();
  if(!ttsAudio){ttsAudio=new Audio()}
  ttsAudio.src=url;
  ttsAudio.playbackRate=speechRate;
  ttsAudio.onended=function(){playNextTts()};
  ttsAudio.onerror=function(){playNextTts()};
  ttsAudio.play().catch(function(){playNextTts()});
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
  var proto=location.protocol==='https:'?'wss:':'ws:';
  var ws=new WebSocket(proto+'//'+location.host+'/ws');
  wsRef=ws;
  ws.onopen=function(){statusEl.textContent=t('connected');statusEl.className='connected';
    if(currentEl){currentEl.remove();currentEl=null}
    var lang=localStorage.getItem('transLang')||'';
    ws.send(JSON.stringify({type:'setLanguage',language:lang,lastId:lastCommitId}));
  };
  ws.onclose=function(){statusEl.textContent=t('disconnected');statusEl.className='disconnected';wsRef=null;setTimeout(connect,2000)};
  ws.onerror=function(){ws.close()};
  ws.onmessage=function(e){
    try{var msg=JSON.parse(e.data);
      if(msg.type==='commit'){
        var id=msg.id||0;
        if(id>lastCommitId){lastCommitId=id;addCommitted(msg.text,msg.lang||'',msg.time||'',msg.refs||null)}
      }
      else if(msg.type==='update')updateCurrent(msg.text);
      else if(msg.type==='clear'){if(currentEl){currentEl.remove();currentEl=null}while(lines.children.length>1)lines.removeChild(lines.children[1]);lastCommitId=0;clearTtsQueue();autoScroll()}
      else if(msg.type==='tts'){handleTtsMessage(msg)}
      else if(msg.type==='pong'){}
    }catch(ex){}
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
document.getElementById('lblTransLang').textContent=t('transLang');
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
document.getElementById('btnBrowseLangs').innerHTML='&#127760; '+t('browseAll');
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
document.getElementById('lblServerTts').textContent=t('serverTts');
document.getElementById('btnServerTts').textContent=t('serverTts');
if(serverTtsActive)document.getElementById('btnServerTts').classList.add('active');

/* ── Fetch config and apply dynamic colors, then connect ── */
(function(){
  fetch('/api/config').then(function(r){return r.json()}).then(function(cfg){
    if(cfg.bgColor){document.documentElement.style.setProperty('--bg-color',cfg.bgColor)}
    if(cfg.fgColor){
      document.documentElement.style.setProperty('--fg-color',cfg.fgColor);
      if(!localStorage.getItem('textColor')){textColor=cfg.fgColor;document.getElementById('colorPicker').value=cfg.fgColor}
    }
  }).catch(function(){});
  connect();
  /* Show language picker on first visit */
  if(!localStorage.getItem('langChosen')){showLangPicker()}
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
  adminStatus.textContent=t('sending');
  fetch('/api/control?action='+action).then(function(r){return r.json()}).then(function(d){
    adminStatus.textContent=action+t('cmdSent');
    setTimeout(function(){closeAllPanels()},600);
  }).catch(function(){adminStatus.textContent=t('cmdFail')});
}
function pollStatus(){
  fetch('/api/control?action=status').then(function(r){return r.json()}).then(function(d){
    if(d.live){adminStatus.textContent=t('liveRun');adminStatus.style.color='#4f4'}
    else if(d.sim){adminStatus.textContent=t('simRun');adminStatus.style.color='#fa0'}
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
admBtns[2].innerHTML='&#8635; '+t('restart');admBtns[3].innerHTML='&#9881; '+t('simulate');
admBtns[4].innerHTML='&#10060; '+t('clear');

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

function toggleBible(){
  if(biblePanel.classList.contains('open')){biblePanel.classList.remove('open');return}
  closeAllPanels();
  biblePanel.classList.add('open');
  if(bibleTranslations.length===0){loadBibleTranslations()}
  else{showBookList()}
}

function closeBible(){biblePanel.classList.remove('open')}

function getBibleLang(){
  /* Use app translation language if set, otherwise browser language */
  var tl=localStorage.getItem('transLang')||'';
  if(tl){
    for(var i=0;i<LANGS.length;i++){if(LANGS[i][0]===tl)return LANGS[i][3]}
  }
  return (navigator.language||'en').split('-')[0];
}

function loadBibleTranslations(){
  bibleContent.innerHTML='<div style="color:#888;text-align:center;padding:40px">Loading...</div>';
  currentBibleTrans='';
  var lang=getBibleLang();
  fetch('/bible/translations?lang='+encodeURIComponent(lang)).then(function(r){return r.json()}).then(function(data){
    /* If no Bibles match the user's language, fall back to all */
    if(!data||!Array.isArray(data)||data.length===0){
      fetch('/bible/translations').then(function(r2){return r2.json()}).then(function(all){
        if(Array.isArray(all)){populateBibleData(all)}
      }).catch(function(){});
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

function onBibleTransChange(val){
  currentBibleTrans=val;
  localStorage.setItem('bibleTrans',val);
  bibleNavStack=[];
  cachedBooks=[];
  cachedBooksTransId='';
  showBookList();
}

var cachedBooks=[];
var cachedBooksTransId='';

function showBookList(){
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

function showVerses(book,chapter){
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
    bibleContent.scrollTop=0;
  }).catch(function(){
    bibleContent.innerHTML='<div style="color:#f44;text-align:center;padding:20px">Failed to load</div>';
  });
}

function bibleBack(){
  if(bibleNavStack.length===0){closeBible();return}
  var prev=bibleNavStack.pop();
  if(prev.type==='books')showBookList();
  else if(prev.type==='chapters')showChapters(prev.book);
}

function lookupRef(){
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

function bibleSearch(){
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
function speakBibleVerse(text){
  clearTtsQueue();
  synth.cancel();
  if(!synth||!text)return;
  bibleTtsActive=true;
  var utter=new SpeechSynthesisUtterance(text);
  utter.rate=speechRate;
  if(selectedVoice){var voices=synth.getVoices();for(var i=0;i<voices.length;i++){if(voices[i].name===selectedVoice){utter.voice=voices[i];break}}}
  utter.onend=function(){bibleTtsActive=false;hideBibleStopBtn()};
  utter.onerror=function(){bibleTtsActive=false;hideBibleStopBtn()};
  synth.speak(utter);
  showBibleStopBtn();
}

function speakBibleVerseServer(text){
  clearTtsQueue();
  synth.cancel();
  bibleTtsActive=true;
  if(!wsRef||wsRef.readyState!==1){bibleTtsActive=false;return;}
  /* Determine language from the Bible translation's language field */
  var lang='eng_Latn';
  for(var i=0;i<bibleTranslations.length;i++){
    if(bibleTranslations[i].id===currentBibleTrans){
      var bl=bibleTranslations[i].language||'en';
      /* Map ISO 639-1 to NLLB-ish code for edge-tts voice selection */
      var isoToNllb={en:'eng',es:'spa',fr:'fra',de:'deu',ca:'cat',pt:'por',it:'ita',ja:'jpn',zh:'zho',ko:'kor',ar:'arb',hi:'hin',ru:'rus',nl:'nld'};
      var nllb=isoToNllb[bl.substring(0,2)];
      if(nllb)lang=nllb;
      break;
    }
  }
  wsRef.send(JSON.stringify({type:'requestTts',text:text,language:lang}));
  showBibleStopBtn();
}

function readAllVerses(){
  /* Works for both chapter view (.bible-verse) and search results (.bible-search-text) */
  var verseDivs=bibleContent.querySelectorAll('.bible-verse');
  if(verseDivs.length===0){verseDivs=bibleContent.querySelectorAll('.bible-search-text')}
  var allText='';
  for(var i=0;i<verseDivs.length;i++){
    /* Get only text nodes, skip .vnum span and speak button */
    var nodes=verseDivs[i].childNodes;
    for(var j=0;j<nodes.length;j++){
      if(nodes[j].nodeType===3){allText+=nodes[j].textContent}
    }
    allText+=' ';
  }
  allText=allText.trim();
  if(!allText)return;
  if(useServerTts()){speakBibleVerseServer(allText)}
  else{speakBibleVerse(allText)}
}

function addVerseSpeakBtn(div,text){
  var btn=document.createElement('button');
  btn.className='bible-verse-speak';
  btn.textContent='\u25B6';
  btn.title=t('readVerse');
  btn.onclick=function(e){
    e.stopPropagation();
    if(useServerTts()){speakBibleVerseServer(text)}
    else{speakBibleVerse(text)}
    showBibleStopBtn();
  };
  div.appendChild(btn);
}

function addReadAllBtn(){
  var bar=document.createElement('div');bar.className='bible-read-all-bar';
  var btn=document.createElement('button');btn.className='bible-read-all-btn';
  btn.id='btnReadAll';
  btn.textContent=t('readAll');
  btn.onclick=function(){
    readAllVerses();
  };
  bar.appendChild(btn);
  bibleContent.insertBefore(bar,bibleContent.firstChild);
}

/* Floating stop button — created once, shown/hidden as needed */
var btnBibleStop=document.createElement('button');
btnBibleStop.id='btnBibleStop';
btnBibleStop.textContent=t('stop');
btnBibleStop.onclick=function(){stopBibleTts()};
document.body.appendChild(btnBibleStop);

function showBibleStopBtn(){
  btnBibleStop.style.display='block';
}
function hideBibleStopBtn(){
  btnBibleStop.style.display='none';
}
function stopBibleTts(){
  bibleTtsActive=false;
  synth.cancel();
  clearTtsQueue();
  if(ttsAudio){ttsAudio.pause();ttsAudio.src=''}
  hideBibleStopBtn();
}

/* Chapter counts by book (standard Protestant canon) */
function getBookChapterCount(book){
  var counts={Gen:50,Exod:40,Lev:27,Num:36,Deut:34,Josh:24,Judg:21,Ruth:4,'1Sam':31,'2Sam':24,'1Kgs':22,'2Kgs':25,'1Chr':29,'2Chr':36,Ezra:10,Neh:13,Esth:10,Job:42,Ps:150,Prov:31,Eccl:12,Song:8,Isa:66,Jer:52,Lam:5,Ezek:48,Dan:12,Hos:14,Joel:3,Amos:9,Obad:1,Jonah:4,Mic:7,Nah:3,Hab:3,Zeph:3,Hag:2,Zech:14,Mal:4,Matt:28,Mark:16,Luke:24,John:21,Acts:28,Rom:16,'1Cor':16,'2Cor':13,Gal:6,Eph:6,Phil:4,Col:4,'1Thess':5,'2Thess':3,'1Tim':6,'2Tim':4,Titus:3,Phlm:1,Heb:13,Jas:5,'1Pet':5,'2Pet':3,'1John':5,'2John':1,'3John':1,Jude:1,Rev:22};
  return counts[book]||50;
}
