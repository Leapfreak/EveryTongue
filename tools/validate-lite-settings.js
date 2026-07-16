/* Runtime validation matrix for the Lite web-settings endpoints (v2.7.19).
   Run against a FRESH config dir:
     EVERYTONGUE_CONFIG_DIR=<scratch> dotnet EveryTongue.Lite.dll   (default PIN 1234)
     node tools/validate-lite-settings.js
   Covers: PIN gates, raw config editor (incl. TemplateStore sync + pinCleared
   bootstrap), template CRUD (incl. unknown-engine 400), Bible catalog/download/
   status polling, and PIN rotation. Exits non-zero on any failure. */
var http = require('http');
var PASS = 0, FAIL = 0, seq = [];
function check(name, cond, detail) {
  if (cond) { PASS++; console.log('PASS  ' + name); }
  else { FAIL++; console.log('FAIL  ' + name + '  -> ' + (detail || '')); }
}
function req(method, path, body, cb) {
  var data = body ? JSON.stringify(body) : null;
  var r = http.request({
    host: 'localhost', port: 5080, path: path, method: method,
    headers: data ? { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(data) } : {}
  }, function (res) {
    var b = '';
    res.on('data', function (c) { b += c; });
    res.on('end', function () {
      var j = null;
      try { j = JSON.parse(b); } catch (e) { }
      cb(res.statusCode, j, b);
    });
  });
  r.on('error', function (e) { cb(0, null, String(e)); });
  if (data) r.write(data);
  r.end();
}
function step(fn) { seq.push(fn); }
function run() {
  var f = seq.shift();
  if (!f) {
    console.log('---');
    console.log('TOTAL: ' + PASS + ' passed, ' + FAIL + ' failed');
    process.exit(FAIL ? 1 : 0);
  }
  f(run);
}

// ── settings core ──
step(function (n) { req('GET', '/api/settings?pin=9999', null, function (c) { check('settings GET wrong pin -> 403', c === 403, c); n(); }); });
step(function (n) { req('GET', '/api/settings?pin=1234', null, function (c, j) { check('settings GET ok + engines', c === 200 && j.sttEngines.length > 0 && j.translationEngines.length > 0, c); n(); }); });
step(function (n) { req('POST', '/api/settings', { pin: '1234', translationBackend: 'deepl' }, function (c, j) { check('settings POST engine change', c === 200 && j.ok, c + ' ' + JSON.stringify(j)); n(); }); });

// ── raw config ──
step(function (n) { req('GET', '/api/settings/rawconfig?pin=9999', null, function (c) { check('rawconfig GET wrong pin -> 403', c === 403, c); n(); }); });
step(function (n) { req('POST', '/api/settings/rawconfig', { pin: '1234', json: '{oops' }, function (c, j) { check('rawconfig POST invalid JSON -> 400', c === 400 && /invalid config JSON/.test(j.error), c); n(); }); });
step(function (n) { req('POST', '/api/settings/rawconfig', { pin: '1234', json: '' }, function (c, j) { check('rawconfig POST empty -> 400', c === 400, c); n(); }); });
step(function (n) {
  req('GET', '/api/settings/rawconfig?pin=1234', null, function (c, j) {
    var cfg = JSON.parse(j.json);
    cfg.SubtitleFontSize = (cfg.SubtitleFontSize || 14) + 1;
    // inject a conference template RAW — exercises the TemplateStore-sync fix
    cfg.ConferenceTemplates = [{ Id: 'rawtpl01', Name: 'Raw Injected', HostingCode: 'raw123', SourceLanguage: 'es', SttBackendKey: 'speechmatics', TranslationBackendKey: '', AudioSource: 'web', DefaultVisibility: 'public' }];
    req('POST', '/api/settings/rawconfig', { pin: '1234', json: JSON.stringify(cfg) }, function (c2, j2) {
      check('rawconfig POST valid edit', c2 === 200 && j2.ok && j2.needsRestart === true, c2 + ' ' + JSON.stringify(j2));
      req('GET', '/api/templates', null, function (c3, j3) {
        check('rawconfig template edit reaches lobby TemplateStore (bug fix)', c3 === 200 && j3.length === 1 && j3[0].name === 'Raw Injected', c3 + ' ' + JSON.stringify(j3));
        n();
      });
    });
  });
});

// ── template editor ──
step(function (n) {
  req('POST', '/api/settings/templates', { pin: '1234', name: 'X', hostingCode: 'y', sttBackend: 'no-such-engine' }, function (c, j) {
    check('template POST unknown sttBackend -> 400', c === 400 && /unknown sttBackend/.test(j.error), c + ' ' + JSON.stringify(j));
    req('GET', '/api/settings/templates?pin=1234', null, function (c2, j2) {
      check('no half-built template left after 400 (bug fix)', c2 === 200 && j2.templates.length === 1, JSON.stringify(j2));
      n();
    });
  });
});
step(function (n) { req('POST', '/api/settings/templates', { pin: '1234', name: 'X', hostingCode: '' }, function (c) { check('template POST missing hostingCode -> 400', c === 400, c); n(); }); });
step(function (n) {
  req('POST', '/api/settings/templates', { pin: '1234', name: 'Val Svc', hostingCode: 'code9', sourceLanguage: 'ca', sttBackend: 'speechmatics', translationBackend: 'google-translate', audioSource: 'web', webMicRaw: true, visibility: 'private', offeredLanguages: ['spa_Latn', 'eng_Latn'] }, function (c, j) {
    check('template POST create', c === 200 && j.ok && !!j.id, c + ' ' + JSON.stringify(j));
    global.tid = j.id; n();
  });
});
step(function (n) {
  req('GET', '/api/settings/templates?pin=1234', null, function (c, j) {
    var tp = j.templates.filter(function (t) { return t.id === global.tid; })[0];
    check('template GET round-trip (offered/raw/private)', tp && tp.webMicRaw === true && tp.visibility === 'private' && tp.offeredLanguages.length === 2, JSON.stringify(tp));
    n();
  });
});
step(function (n) {
  req('POST', '/api/rooms/from-template', { templateId: global.tid, hostingCode: 'WRONG', hostClientId: '' }, function (c) {
    check('hosting wrong code -> 403', c === 403, c);
    req('POST', '/api/rooms/from-template', { templateId: global.tid, hostingCode: 'code9', hostClientId: '' }, function (c2, j2) {
      check('room starts from web-created template', c2 === 201 && !!j2.hostToken, c2);
      n();
    });
  });
});
step(function (n) {
  // Partial update: body omits engine fields — previously-set keys must survive
  req('POST', '/api/settings/templates', { pin: '1234', id: global.tid, name: 'Val Svc 2', hostingCode: 'code9' }, function (c, j) {
    req('GET', '/api/settings/templates?pin=1234', null, function (c2, j2) {
      var tp = j2.templates.filter(function (t) { return t.id === global.tid; })[0];
      check('partial update keeps engine keys (round-2 fix)', c === 200 && tp && tp.sttBackend === 'speechmatics' && tp.translationBackend === 'google-translate', JSON.stringify(tp));
      n();
    });
  });
});
step(function (n) {
  // Concurrency: 6 creates + 6 list GETs in flight together — no 500s, all land
  var done = 0, bad = 0, made = [];
  function fin() { if (++done < 12) return;
    check('12 concurrent template ops, zero errors', bad === 0, bad + ' failed');
    // cleanup the concurrency templates
    var left = made.length;
    if (!left) { n(); return; }
    made.forEach(function (cid) {
      req('POST', '/api/settings/templates/delete', { pin: '1234', id: cid }, function () { if (--left === 0) n(); });
    });
  }
  for (var i = 0; i < 6; i++) {
    req('POST', '/api/settings/templates', { pin: '1234', name: 'Conc ' + i, hostingCode: 'c' + i }, function (c, j) { if (c !== 200) bad++; else made.push(j.id); fin(); });
    req('GET', '/api/settings/templates?pin=1234', null, function (c) { if (c !== 200) bad++; fin(); });
  }
});
step(function (n) { req('POST', '/api/settings/templates/delete', { pin: '1234', id: global.tid }, function (c, j) { check('template delete', c === 200 && j.ok, c); n(); }); });
step(function (n) { req('POST', '/api/settings/templates/delete', { pin: '1234', id: 'nope' }, function (c) { check('template delete unknown -> 404', c === 404, c); n(); }); });

// ── three-page IA: admin page, gated lists, template resolver + QR, control gate ──
step(function (n) {
  req('GET', '/admin.html', null, function (c, j, raw) {
    check('admin page served', c === 200 && raw.indexOf('Server Administration') >= 0, c);
    req('GET', '/js/admin.js', null, function (c2) { check('admin.js served', c2 === 200, c2); n(); });
  });
});
step(function (n) {
  // Set a creator code, then: lists 403 bare, 200 with code, 200 with admin PIN
  req('POST', '/api/settings', { pin: '1234', creatorCode: 'vol42' }, function (c) {
    req('GET', '/api/rooms', null, function (c1) {
      check('gated: /api/rooms bare -> 403', c1 === 403, c1);
      req('GET', '/api/rooms?code=vol42', null, function (c2) {
        check('gated: /api/rooms with code -> 200', c2 === 200, c2);
        req('GET', '/api/templates?code=1234', null, function (c3) {
          check('gated: /api/templates with admin PIN -> 200', c3 === 200, c3);
          req('GET', '/api/templates?code=wrong-code', null, function (c4) {
            check('gated: /api/templates wrong code -> 403', c4 === 403, c4);
            n();
          });
        });
      });
    });
  });
});
step(function (n) {
  // Template-pointer flow: no room -> active:false; from-template room -> resolves; QR serves
  req('POST', '/api/settings/templates', { pin: '1234', name: 'Perm Svc', hostingCode: 'perm7', sourceLanguage: 'es', sttBackend: 'speechmatics', audioSource: 'web' }, function (c, j) {
    var tid = j.id;
    req('GET', '/api/templates/' + tid + '/active-room', null, function (c1, j1) {
      check('resolver: no room yet -> active:false', c1 === 200 && j1.active === false, c1 + ' ' + JSON.stringify(j1));
      req('POST', '/api/rooms/from-template', { templateId: tid, hostingCode: 'perm7', hostClientId: '' }, function (c2, j2) {
        req('GET', '/api/templates/' + tid + '/active-room', null, function (c3, j3) {
          check('resolver: live room resolves', c3 === 200 && j3.active === true && j3.roomId === j2.id, JSON.stringify(j3));
          req('GET', '/api/templates/' + tid + '/qr', null, function (c4, j4, raw4) {
            check('permanent template QR serves PNG', c4 === 200 && raw4.length > 400, c4 + ' bytes=' + raw4.length);
            req('GET', '/api/templates/zzz-none/active-room', null, function (c5) {
              check('resolver: unknown template -> 404', c5 === 404, c5);
              req('POST', '/api/settings/templates/delete', { pin: '1234', id: tid }, function () { n(); });
            });
          });
        });
      });
    });
  });
});
step(function (n) {
  // /api/control: status open; mutations PIN-gated (pre-existing hole closed)
  req('GET', '/api/control?action=status', null, function (c, j) {
    check('control: status stays open', c === 200 && typeof j.live === 'boolean', c);
    req('GET', '/api/control?action=clear', null, function (c1) {
      check('control: mutation bare -> 403', c1 === 403, c1);
      req('GET', '/api/control?action=clear&pin=1234', null, function (c2, j2) {
        check('control: mutation with PIN -> ok', c2 === 200 && j2.ok === true, c2 + ' ' + JSON.stringify(j2));
        // clear the creator code so later steps run ungated
        req('POST', '/api/settings', { pin: '1234', creatorCode: '-' }, function () { n(); });
      });
    });
  });
});

// ── bibles ──
step(function (n) { req('GET', '/api/settings/bibles/status?pin=1234', null, function (c, j) { check('bibles status endpoint (new)', c === 200 && typeof j.states === 'object', c); n(); }); });
step(function (n) {
  req('GET', '/api/settings/bibles?pin=1234', null, function (c, j) {
    check('bibles catalog on FRESH dir', c === 200 && j.catalog.length > 1000, c + ' entries=' + (j && j.catalog ? j.catalog.length : '-'));
    n();
  });
});
step(function (n) { req('POST', '/api/settings/bibles/download', { pin: '1234', translationId: 'zzz-nope' }, function (c) { check('bible download unknown id -> 404', c === 404, c); n(); }); });
step(function (n) {
  req('POST', '/api/settings/bibles/download', { pin: '1234', translationId: 'spablm' }, function (c, j) {
    check('bible download start', c === 200 && j.ok, c);
    var tries = 0;
    var iv = setInterval(function () {
      req('GET', '/api/settings/bibles/status?pin=1234', null, function (c2, j2) {
        var st = j2 && j2.states && j2.states.spablm;
        if (st === 'done' || (st && st.indexOf('error') === 0) || ++tries > 24) {
          clearInterval(iv);
          check('bible download completes via status poll', st === 'done', st);
          req('GET', '/bible/translations?language=spa', null, function (c3, j3) {
            check('live Bible API serves it (no restart)', c3 === 200 && j3.length === 1, c3 + ' ' + JSON.stringify(j3).slice(0, 80));
            n();
          });
        }
      });
    }, 2500);
  });
});

// ── bootstrap (PIN cleared via rawconfig) + rotation ──
step(function (n) {
  req('GET', '/api/settings/rawconfig?pin=1234', null, function (c, j) {
    var cfg = JSON.parse(j.json);
    cfg.AdminPin = '';
    req('POST', '/api/settings/rawconfig', { pin: '1234', json: JSON.stringify(cfg) }, function (c2, j2) {
      check('rawconfig PIN clear flagged (pinCleared)', c2 === 200 && j2.pinCleared === true, JSON.stringify(j2));
      req('GET', '/api/settings', null, function (c3, j3) {
        check('bootstrap mode: settings open with NO pin', c3 === 200 && j3.adminPinSet === false, c3);
        req('POST', '/api/settings', { pin: '', adminPin: '5678' }, function (c4, j4) {
          check('bootstrap: set new PIN', c4 === 200 && j4.ok, c4);
          req('GET', '/api/settings?pin=1234', null, function (c5) {
            check('old PIN rejected after change', c5 === 403, c5);
            req('GET', '/api/settings?pin=5678', null, function (c6) {
              check('new PIN accepted', c6 === 200, c6);
              n();
            });
          });
        });
      });
    });
  });
});

// ── rate limiter — MUST BE LAST: it blocks this IP for 5 minutes ──
step(function (n) {
  var fails = 0, sent = 0;
  function hammer() {
    if (sent >= 11) {
      // even the CORRECT pin is now rejected — the IP is blocked
      req('GET', '/api/settings?pin=5678', null, function (c) {
        check('rate limiter: correct PIN rejected after 10 failures', c === 403, c);
        n();
      });
      return;
    }
    sent++;
    req('GET', '/api/settings?pin=wrong-' + sent, null, function (c) { if (c === 403) fails++; hammer(); });
  }
  hammer();
});
run();
