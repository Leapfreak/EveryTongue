/* Insert the three-page-IA locale keys into en/es/ca packs at alphabetical anchors. */
const fs = require('fs');

const KEYS = {
    en: {
        admBlock:
            '  "web.admBootstrap": "No admin PIN is set — anyone can open this page. Set a PIN below now.",\n' +
            '  "web.admEnter": "Enter",\n' +
            '  "web.admLive": "Live session",\n' +
            '  "web.admQrBtn": "QR",\n' +
            '  "web.admQrPermanent": "Permanent — print this. It always joins the current room made from this template.",\n' +
            '  "web.admRefresh": "Refresh",\n' +
            '  "web.admSub": "Every Tongue",\n' +
            '  "web.admTitle": "Server Administration",\n',
        hostAdmin: '  "web.hostAdmin": "Admin",\n',
        join:
            '  "web.joinUnknown": "This QR code isn’t valid for this server any more — please ask for a new one.",\n' +
            '  "web.joinWaiting": "The service hasn’t started yet — this page will join automatically as soon as it begins.",\n',
        lbQr: '  "web.lbQrPermanent": "Permanent — print this. It always joins the current room made from this template.",\n'
    },
    es: {
        admBlock:
            '  "web.admBootstrap": "No hay PIN de administrador — cualquiera puede abrir esta página. Establezca un PIN ahora.",\n' +
            '  "web.admEnter": "Entrar",\n' +
            '  "web.admLive": "Sesión en directo",\n' +
            '  "web.admQrBtn": "QR",\n' +
            '  "web.admQrPermanent": "Permanente — imprímalo. Siempre lleva a la sala actual creada desde esta plantilla.",\n' +
            '  "web.admRefresh": "Actualizar",\n' +
            '  "web.admSub": "Every Tongue",\n' +
            '  "web.admTitle": "Administración del servidor",\n',
        hostAdmin: '  "web.hostAdmin": "Administración",\n',
        join:
            '  "web.joinUnknown": "Este código QR ya no es válido en este servidor — pida uno nuevo.",\n' +
            '  "web.joinWaiting": "El servicio aún no ha comenzado — esta página se unirá automáticamente en cuanto empiece.",\n',
        lbQr: '  "web.lbQrPermanent": "Permanente — imprímalo. Siempre lleva a la sala actual creada desde esta plantilla.",\n'
    },
    ca: {
        admBlock:
            '  "web.admBootstrap": "No hi ha PIN d\'administrador — qualsevol pot obrir aquesta pàgina. Establiu un PIN ara.",\n' +
            '  "web.admEnter": "Entra",\n' +
            '  "web.admLive": "Sessió en directe",\n' +
            '  "web.admQrBtn": "QR",\n' +
            '  "web.admQrPermanent": "Permanent — imprimiu-lo. Sempre porta a la sala actual creada des d\'aquesta plantilla.",\n' +
            '  "web.admRefresh": "Actualitza",\n' +
            '  "web.admSub": "Every Tongue",\n' +
            '  "web.admTitle": "Administració del servidor",\n',
        hostAdmin: '  "web.hostAdmin": "Administració",\n',
        join:
            '  "web.joinUnknown": "Aquest codi QR ja no és vàlid en aquest servidor — demaneu-ne un de nou.",\n' +
            '  "web.joinWaiting": "El servei encara no ha començat — aquesta pàgina s\'hi unirà automàticament quan comenci.",\n',
        lbQr: '  "web.lbQrPermanent": "Permanent — imprimiu-lo. Sempre porta a la sala actual creada des d\'aquesta plantilla.",\n'
    }
};

function insertBefore(src, anchor, block, label, file) {
    const i = src.indexOf(anchor);
    if (i < 0) throw new Error(anchor + ' not found in ' + file);
    console.log(file + ': ' + label);
    return src.slice(0, i) + block + src.slice(i);
}

for (const lang of ['en', 'es', 'ca']) {
    const file = 'locales/' + lang + '.json';
    let s = fs.readFileSync(file, 'utf8');
    const K = KEYS[lang];
    s = insertBefore(s, '  "web.adminBad"', K.admBlock, 'adm block', file);
    s = insertBefore(s, '  "web.hostApply"', K.hostAdmin, 'hostAdmin', file);
    s = insertBefore(s, '  "web.keepScreen"', K.join, 'join keys', file);
    s = insertBefore(s, '  "web.lbTypeConference"', K.lbQr, 'lbQrPermanent', file);
    JSON.parse(s); // validate before writing
    fs.writeFileSync(file, s);
}
console.log('locale keys inserted + validated');
