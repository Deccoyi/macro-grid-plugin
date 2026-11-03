// Tests for VoiceChangerControl/index.js against a fake Voicemod server (a minimal web socket server on this computer).
// The plugin script runs in a vm context with a fake `host`; time is a fake clock, so no test waits for a real retry delay.
// Run: node --test scripts/tests/voice-changer.test.mjs   (needs Node 22 or newer for the global WebSocket client)
import { test } from 'node:test';
import assert from 'node:assert/strict';
import net from 'node:net';
import crypto from 'node:crypto';
import vm from 'node:vm';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const pluginDir = path.join(path.dirname(fileURLToPath(import.meta.url)), '../../VoiceChangerControl');
const script = fs.readFileSync(path.join(pluginDir, 'index.js'), 'utf8');
const manifest = JSON.parse(fs.readFileSync(path.join(pluginDir, 'plugin.json'), 'utf8'));
const CAP = manifest.limits.wsMessageKB * 1024; // the host closes a socket on a message over this
const GUID = '258EAFA5-E914-47DA-95CA-C5AB0DC85B11';
const KEY = 'secret-test-key';

// ---- fake Voicemod ----
function frame(text) {
  const body = Buffer.from(text);
  let head;
  if (body.length < 126) head = Buffer.from([0x81, body.length]);
  else if (body.length < 65536) head = Buffer.from([0x81, 126, body.length >> 8, body.length & 255]);
  else { head = Buffer.alloc(10); head[0] = 0x81; head[1] = 127; head.writeBigUInt64BE(BigInt(body.length), 2); }
  return Buffer.concat([head, body]);
}

function fakeVoicemod({ validKey = KEY } = {}) {
  const received = [];
  const sockets = new Set();
  let fake;
  const server = net.createServer((sock) => {
    sockets.add(sock);
    sock.on('close', () => sockets.delete(sock));
    sock.on('error', () => {});
    let handshaken = false;
    let buf = Buffer.alloc(0);
    sock.on('data', (chunk) => {
      buf = Buffer.concat([buf, chunk]);
      if (!handshaken) {
        const end = buf.indexOf('\r\n\r\n');
        if (end < 0) return;
        const key = /sec-websocket-key: (.+)\r\n/i.exec(buf.subarray(0, end).toString() + '\r\n')[1].trim();
        const accept = crypto.createHash('sha1').update(key + GUID).digest('base64');
        sock.write('HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: ' + accept + '\r\n\r\n');
        handshaken = true;
        buf = buf.subarray(end + 4);
      }
      while (buf.length >= 2) {
        const opcode = buf[0] & 15;
        let len = buf[1] & 127, off = 2;
        if (len === 126) { if (buf.length < 4) return; len = buf.readUInt16BE(2); off = 4; }
        if (buf.length < off + 4 + len) return;
        const mask = buf.subarray(off, off + 4);
        const data = Buffer.from(buf.subarray(off + 4, off + 4 + len));
        for (let i = 0; i < len; i++) data[i] ^= mask[i % 4];
        buf = buf.subarray(off + 4 + len);
        if (opcode === 8) { sock.end(Buffer.from([0x88, 0])); return; } // close handshake
        let msg;
        try { msg = JSON.parse(data.toString()); } catch { continue; } // close frames and pings are not JSON
        received.push(msg);
        if (msg.action === 'getMemes') sock.write(frame(JSON.stringify({ actionType: 'getMemes', actionObject: { listOfMemes: fake.memes } })));
        if (msg.action === 'registerClient') {
          const ok = msg.payload.clientKey === validKey;
          sock.write(frame(JSON.stringify({ id: msg.id, action: 'registerClient', payload: { status: { code: ok ? 200 : 401, description: ok ? 'ok' : 'Invalid key' } } })));
        }
      }
    });
  });
  fake = {
    received,
    memes: [], // what getMemes answers with
    push: (obj) => { for (const s of sockets) s.write(frame(JSON.stringify(obj))); },
    listen: () => new Promise((res) => server.listen(0, '127.0.0.1', () => res(server.address().port))),
    close: () => { for (const s of sockets) s.destroy(); server.close(); },
  };
  return fake;
}

// ---- the plugin under a fake host ----
function loadPlugin({ portMap, apiKey = KEY }) {
  let now = 1_000_000;
  const settings = { apiKey };
  let open = 0, maxOpen = 0;
  const varWrites = {}, sources = {}, puts = [], logs = [], invalidated = [], described = [];
  let imageHandler = null;
  const vars = {}, statuses = [], reports = [], actions = {}, every = [], connectPorts = [], clientSockets = [];
  const host = {
    variables: { describe: (list) => { described.push(...list); }, set: (n, v) => { vars[n] = v; varWrites[n] = (varWrites[n] || 0) + 1; } },
    settings: { page() {}, get: () => settings },
    status: (id, text, level) => statuses.push({ text, level }),
    diagnostics: { report: (level, message, key) => reports.push({ level, message, key }), resolve() {} },
    registerAction: (a) => { actions[a.type] = a; },
    every: (ms, fn) => every.push(fn),
    after() {},
    options: { source: (id, fn) => { sources[id] = fn; } },
    log: (m) => logs.push(m),
    images: { onRequest: (fn) => { imageHandler = fn; }, put: (id, url, version) => { puts.push({ id, url, version }); return true; }, invalidate: (id, version) => { invalidated.push(id + '@' + version); } },
    ws: {
      connect(url, handlers) {
        const port = Number(new URL(url).port);
        connectPorts.push(port);
        const real = portMap[port];
        if (real === undefined) { // nothing listens there: the same as a refused connection
          queueMicrotask(() => handlers.onClose({ code: null, reason: '', error: 'refused' }));
          return { send() {}, close() {} };
        }
        const ws = new WebSocket('ws://127.0.0.1:' + real + '/v1/');
        clientSockets.push(ws);
        open++; maxOpen = Math.max(maxOpen, open); // the host counts a socket from connect to close
        ws.onopen = () => handlers.onOpen();
        // the host closes a socket that receives a message over the limit of plugin.json
        ws.onmessage = (e) => { if (String(e.data).length > CAP) ws.close(1000); else handlers.onMessage(String(e.data)); };
        ws.onclose = () => { open--; handlers.onClose({ code: 1000, reason: '', error: null }); };
        ws.onerror = () => {};
        return { send: (t) => { if (ws.readyState !== 1) throw new Error('The web socket is closed.'); ws.send(t); }, close: () => ws.close() };
      },
    },
  };
  vm.runInNewContext(script, { host, Date: { now: () => now }, JSON, Number, String, Array, Math });
  return {
    varWrites, sources, puts, logs, invalidated, described, requestImages: (ids) => imageHandler(ids), pump: () => every[1](), maxOpen: () => maxOpen, open: () => open,
    vars, statuses, reports, actions, settings, connectPorts,
    tick: () => every[0](),
    advance: (ms) => { now += ms; },
    last: () => statuses[statuses.length - 1],
    stop: () => clientSockets.forEach((s) => s.close()),
  };
}

const wait = (check, what) => new Promise((res, rej) => {
  const start = Date.now();
  const t = setInterval(() => {
    if (check()) { clearInterval(t); res(); } else if (Date.now() - start > 3000) { clearInterval(t); rej(new Error('timeout: ' + what)); }
  }, 10);
});

async function connected({ validKey, lazy = false } = {}) {
  const fake = fakeVoicemod({ validKey });
  const port = await fake.listen();
  const plugin = loadPlugin({ portMap: { 59129: port } });
  if (!lazy) plugin.sources.sounds({ text: '' }, {}); // a picker has asked for the sounds (they are read lazily)
  plugin.tick();
  return { fake, plugin };
}

test('register 200: the first message is registerClient and the status turns Ok', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected status');
    assert.equal(fake.received[0].action, 'registerClient');
    assert.equal(fake.received[0].payload.clientKey, KEY);
    await wait(() => fake.received.some((m) => m.action === 'getVoiceChangerStatus'), 'status queries');
  } finally { plugin.stop(); fake.close(); }
});

test('register 401: clear message, key never shown, no new attempt until the key changes', async () => {
  const { fake, plugin } = await connected({ validKey: 'another-key' });
  try {
    await wait(() => plugin.reports.length > 0, '401 report');
    assert.match(plugin.reports[0].message, /401/);
    assert.match(plugin.reports[0].message, /tutorials\/voicemod/);
    for (const text of [...plugin.statuses.map((s) => s.text), ...plugin.reports.map((r) => r.message)]) assert.ok(!text.includes(KEY));
    assert.equal(plugin.last().level, 'Error');
    await new Promise((r) => setTimeout(r, 50)); // let the close arrive
    const attempts = plugin.connectPorts.length;
    plugin.advance(60_000); plugin.tick(); plugin.tick();
    assert.equal(plugin.connectPorts.length, attempts, 'no retry with the rejected key');
    plugin.settings.apiKey = 'another-key';
    plugin.tick();
    await wait(() => plugin.last().level === 'Ok', 'reconnect with the new key');
  } finally { plugin.stop(); fake.close(); }
});

test('empty key: a warning and no connection', () => {
  const plugin = loadPlugin({ portMap: {}, apiKey: '  ' });
  plugin.tick();
  assert.equal(plugin.connectPorts.length, 0);
  assert.equal(plugin.last().level, 'Warning');
  assert.match(plugin.last().text, /API key/);
});

test('port fallback and retry spacing: Voicemod not running, then found on a later port', async () => {
  const fake = fakeVoicemod();
  const port = await fake.listen();
  const plugin = loadPlugin({ portMap: { 20000: port } }); // 59129 refuses, 20000 answers
  try {
    plugin.tick();
    await wait(() => plugin.last()?.level === 'Warning', 'not running warning');
    assert.match(plugin.last().text, /not running/);
    plugin.tick(); // too early: still waiting
    assert.deepEqual(plugin.connectPorts, [59129]);
    plugin.advance(7000); plugin.tick();
    await wait(() => plugin.last().level === 'Ok', 'connected on a fallback port');
    assert.deepEqual(plugin.connectPorts, [59129, 20000]);
  } finally { plugin.stop(); fake.close(); }
});

test('every port tried is in the permission list; the first port comes back every second attempt', async () => {
  const plugin = loadPlugin({ portMap: {} });
  for (let i = 0; i < 24; i++) {
    plugin.advance(30_000);
    plugin.tick();
    await new Promise((r) => setTimeout(r, 5)); // the refusal arrives as a microtask
  }
  const manifest = JSON.parse(fs.readFileSync(path.join(pluginDir, 'plugin.json'), 'utf8'));
  const allowed = new Set(manifest.permissions.filter((p) => p.startsWith('ws:localhost:')).map((p) => Number(p.split(':')[2])));
  assert.equal(allowed.size, 11);
  assert.equal(plugin.connectPorts.length, 24);
  for (const p of plugin.connectPorts) assert.ok(allowed.has(p), 'port ' + p);
  plugin.connectPorts.forEach((p, i) => { if (i % 2 === 0) assert.equal(p, 59129); });
  assert.equal(new Set(plugin.connectPorts).size, 11, 'every fallback port is reached');
});

test('actions send the Voicemod message; without a connection they fail clearly', async () => {
  const { fake, plugin } = await connected();
  try {
    assert.equal(plugin.actions['voicechanger.togglemute'].run({}, {}).ok, false); // not registered yet
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    plugin.advance(1100); // the start-up queries are out of the one-second send window
    assert.equal(plugin.actions["voicechanger.togglevoicechanger"].run({}, {}).ok, true);
    plugin.actions['voicechanger.togglehearmyself'].run({}, {});
    plugin.actions['voicechanger.loadvoice'].run({}, { voiceID: 'baby' });
    plugin.actions['voicechanger.randomvoice'].run({}, { mode: 'FavoriteVoices' });
    plugin.actions['voicechanger.playsound'].run({}, { fileName: 'laugh.mp3' });
    plugin.actions['voicechanger.stopsounds'].run({}, {});
    await wait(() => fake.received.some((m) => m.action === 'stopAllMemeSounds'), 'messages arrive');
    const by = (a) => fake.received.find((m) => m.action === a);
    assert.ok(by('toggleVoiceChanger'));
    assert.ok(by('toggleHearMyVoice'));
    assert.deepEqual(by('loadVoice').payload, { voiceID: 'baby' });
    assert.deepEqual(by('selectRandomVoice').payload, { mode: 'FavoriteVoices' });
    assert.deepEqual(by('playMeme').payload, { FileName: 'laugh.mp3', IsKeyDown: true });
    assert.equal(plugin.actions['voicechanger.loadvoice'].run({}, { voiceID: ' ' }).ok, false);
  } finally { plugin.stop(); fake.close(); }
});

test('events: both actionID and actionId spellings and the other envelope update the variables', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    fake.push({ actionID: 'a1', actionType: 'toggleVoiceChanger', actionObject: { value: true } });
    await wait(() => plugin.vars['voicechanger.enabled'] === true, 'enabled via actionID');
    fake.push({ actionId: 'a2', actionType: 'toggleMuteMic', actionObject: { value: true } });
    await wait(() => plugin.vars['voicechanger.micmuted'] === true, 'muted via actionId');
    fake.push({ actionType: 'voiceChangedEvent', actionObject: { voiceID: 'baby' } });
    await wait(() => plugin.vars['voicechanger.voice'] === 'baby', 'voice event');
    fake.push({ id: '9', action: 'getVoiceChangerStatus', payload: { value: false } }); // another envelope
    await wait(() => plugin.vars['voicechanger.enabled'] === false, 'payload envelope');
    fake.push({ actionType: 'toggleMuteMemeForMe', actionObject: { value: true } });
    fake.push({ actionType: 'toggleMuteMic', actionObject: { value: false } });
    await wait(() => plugin.vars['voicechanger.micmuted'] === false, 'the meme mute is not the microphone');
  } finally { plugin.stop(); fake.close(); }
});

test('never more than one socket open: port fallback, reconnects and a dropped session', async () => {
  const fake = fakeVoicemod();
  const port = await fake.listen();
  const plugin = loadPlugin({ portMap: { 39273: port } }); // two refused ports before the one that answers
  try {
    for (let i = 0; i < 12; i++) { plugin.advance(7000); plugin.tick(); await new Promise((r) => setTimeout(r, 30)); }
    await wait(() => plugin.last().level === 'Ok', 'connected');
    assert.equal(plugin.open(), 1);
    fake.close(); // Voicemod quits: the socket is dropped
    await wait(() => plugin.open() === 0, 'closed');
    plugin.advance(7000); plugin.tick();
    await new Promise((r) => setTimeout(r, 30));
    assert.equal(plugin.maxOpen(), 1);
  } finally { plugin.stop(); fake.close(); }
});

test('connects stay under the host limit of 10 a minute, also after a dropped session', async () => {
  const plugin = loadPlugin({ portMap: {} });
  let t = 0;
  while (t < 120_000) { plugin.advance(1000); t += 1000; plugin.tick(); await new Promise((r) => setTimeout(r, 2)); }
  const perMinute = plugin.connectPorts.length / 2;
  assert.ok(perMinute <= 8.5, 'connects a minute: ' + perMinute);
});



test('a message that cannot be read does not throw out of the callback', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    fake.push({ actionType: 'getVoices', actionObject: { voices: [null] } });
    fake.push({ actionType: 'toggleMuteMic', actionObject: { value: true } });
    await wait(() => plugin.vars['voicechanger.micmuted'] === true, 'still working');
  } finally { plugin.stop(); fake.close(); }
});

// ---- host limits (see the table in the README) ----
test('limits: every status text fits the host cut of 80 characters, and an unchanged status is not sent again', async () => {
  const noKey = loadPlugin({ portMap: {}, apiKey: '' });
  for (let i = 0; i < 20; i++) noKey.tick();
  assert.equal(noKey.statuses.length, 1, 'the same status is sent once');
  const down = loadPlugin({ portMap: {} });
  down.tick();
  await new Promise((r) => setTimeout(r, 30));
  const { fake, plugin } = await connected({ validKey: 'other' });
  try {
    await wait(() => plugin.reports.length > 0, '401');
    await new Promise((r) => setTimeout(r, 30));
    for (const p of [noKey, down, plugin]) for (const st of p.statuses) assert.ok(st.text.length <= 80, st.text.length + ': ' + st.text);
    assert.ok(plugin.reports[0].message.length <= 300, 'a diagnostic is cut at 300 characters');
  } finally { plugin.stop(); fake.close(); }
});

test('limits: an event flood writes each variable once while its value stays the same', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    for (let i = 0; i < 120; i++) fake.push({ actionType: 'toggleMuteMic', actionObject: { value: i % 40 === 39 } });
    await wait(() => plugin.vars['voicechanger.micmuted'] === true && plugin.varWrites['voicechanger.micmuted'] >= 6, 'events handled');
    assert.ok(plugin.varWrites['voicechanger.micmuted'] <= 8, 'writes: ' + plugin.varWrites['voicechanger.micmuted']);
  } finally { plugin.stop(); fake.close(); }
});


test('limits: pressing a button very fast sends at most 10 messages a second and never throws', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    await new Promise((r) => setTimeout(r, 50));
    let refused = 0;
    for (let i = 0; i < 60; i++) {
      const r = plugin.actions['voicechanger.togglemute'].run({}, {});
      if (!r.ok) { refused++; assert.match(r.message, /Too many/); }
    }
    assert.ok(refused > 0);
    await new Promise((r) => setTimeout(r, 100));
    const toggles = fake.received.filter((m) => m.action === 'toggleMuteMic').length;
    assert.ok(toggles <= 10, 'sent ' + toggles);
  } finally { plugin.stop(); fake.close(); }
});

test('limits: an action while the socket is closing fails with a message, it does not throw', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    fake.close();
    await wait(() => plugin.open() === 0, 'closed');
    const r = plugin.actions['voicechanger.togglemute'].run({}, {});
    assert.equal(r.ok, false);
  } finally { plugin.stop(); fake.close(); }
});

test('limits: names, timers and permissions stay inside the host rules', () => {
  const manifest = JSON.parse(fs.readFileSync(path.join(pluginDir, 'plugin.json'), 'utf8'));
  for (const p of manifest.permissions) assert.match(p, /^(variables|actions|ws:[A-Za-z0-9.\-]+:[0-9]{1,5})$/);
  const names = [...script.matchAll(/voicechanger\.[a-z]+/g)].map((m) => m[0]);
  for (const n of names) assert.ok(n.length <= 120 && /^[A-Za-z0-9._-]+$/.test(n));
  const timers = [...script.matchAll(/host\.(every|after)\(/g)];
  assert.ok(timers.length <= 20);
});

// ---- voice and sound lists, search sources ----
const VOICES = [
  { id: 'baby', friendlyName: 'Baby', favorited: false },
  { id: 'deep', friendlyName: 'Deep', favorited: true },
  { id: 'robot', friendlyName: 'Robot', favorited: false },
  { id: 'alien', friendlyName: 'Alien', favorited: false },
];
const SOUNDBOARDS = [
  { id: 'b1', name: 'Funny', sounds: [{ id: 's-laugh', name: 'Laugh', bitmapChecksum: 'CS-LAUGH-1' }, { id: 's-airhorn', name: 'Air horn' }] },
  { id: 'b2', name: 'Alerts', sounds: [{ id: 's-nuke', name: 'Nuke alert', bitmapChecksum: 'has space' }] },
];

async function withLists() {
  const ctx = await connected();
  await wait(() => ctx.fake.received.some((m) => m.action === 'getVoices'), 'getVoices asked');
  ctx.fake.push({ actionType: 'getVoices', actionObject: { voices: VOICES }, currentVoice: 'baby' });
  await wait(() => ctx.fake.received.some((m) => m.action === 'getAllSoundboard'), 'getAllSoundboard asked after the voices');
  ctx.fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: SOUNDBOARDS } });
  await wait(() => ctx.plugin.sources.sounds({ text: '' }).options.length > 0, 'sound list');
  return ctx;
}

test('lists: voices, then sounds, then memes, one at a time; no bitmap is ever asked for', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    await new Promise((r) => setTimeout(r, 100));
    assert.ok(!fake.received.some((m) => m.action === 'getAllSoundboard'), 'the second list waits for the first reply');
    fake.push({ actionType: 'getVoices', actionObject: { voices: VOICES } });
    await wait(() => fake.received.some((m) => m.action === 'getAllSoundboard'), 'second list');
    const asked = fake.received.map((m) => m.action);
    assert.ok(!asked.includes('getBitmap'));
    assert.ok(!asked.includes('getMemes'), 'the memes wait for the sounds');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [] } });
    await wait(() => fake.received.some((m) => m.action === 'getMemes'), 'memes after the sounds');
  } finally { plugin.stop(); fake.close(); }
});

test('sounds source: custom and disabled boards, boards with other key spelling, memes as the Community group, search over both, play for both kinds', async () => {
  const { fake, plugin } = await connected();
  try {
    fake.memes = [
      { Name: 'Toc toc', FileName: '78', Type: 'PlayRestart', Image: 'x' },
      { Name: 'Toc toc again', FileName: '78', Type: 'PlayRestart' },
      { Name: 'Community horn', Filename: '80', Type: 'PlayStop' },
      { Name: 'No file', Type: 'PlayStop' },
    ];
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    fake.push({ actionType: 'getVoices', actionObject: { voices: VOICES } });
    await wait(() => fake.received.some((m) => m.action === 'getAllSoundboard'), 'sounds asked');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [
      { id: 'b1', name: 'Mine', isCustom: true, enabled: true, sounds: [{ id: 'g-1', name: 'Custom beep', isCustom: true, enabled: true }, { id: 'g-1', name: 'duplicate' }] },
      { Id: 'b2', Name: 'Off board', Enabled: false, sounds: [{ Id: 'g-2', Name: 'Hidden', Enabled: true }] },
      { id: 'b3', name: 'Empty', sounds: [] },
      { id: 'b4', name: 'Mixed', sounds: [{ id: 'g-3', name: 'Off sound', enabled: false }, { name: 'no id' }] },
    ] } });
    await wait(() => plugin.sources.sounds({ text: '' }).options.length >= 5, 'all lists');
    const all = plugin.sources.sounds({ text: '', limit: 50 }).options;
    assert.deepEqual([...all.map((o) => o.label)].sort(), ['Community horn', 'Custom beep', 'Hidden (disabled)', 'Off sound (disabled)', 'Toc toc'].sort());
    assert.equal(all.find((o) => o.value === 'meme:78').group, 'Community');
    assert.equal(all.find((o) => o.value === 'g-2').group, 'Off board');
    assert.equal(plugin.vars['voicechanger.soundcount'], 5);
    assert.deepEqual([...plugin.sources.sounds({ text: 'horn' }).options.map((o) => o.value)], ['meme:80']);
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 'meme:78' }).ok, true);
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 'g-3' }).ok, true);
    await wait(() => fake.received.filter((m) => m.action === 'playMeme').length === 2, 'two plays');
    const plays = fake.received.filter((m) => m.action === 'playMeme').map((m) => m.payload.FileName);
    assert.deepEqual([...plays], ['78', 'g-3']);
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 'meme:999' }).code, 'NotFound');
  } finally { plugin.stop(); fake.close(); }
});

test('voices source: favorites first, then recent, then A-Z; search; paging; selected', async () => {
  const { fake, plugin } = await withLists();
  try {
    const all = plugin.sources.voices({ text: '', offset: 0, limit: 50 });
    assert.deepEqual([...all.options.map((o) => o.label)], ['Deep', 'Alien', 'Baby', 'Robot']);
    assert.deepEqual([...all.options.map((o) => o.group)], ['Favorites', 'All voices', 'All voices', 'All voices']);
    assert.equal(all.more, false);
    // a voice that was just used moves up, under "Recent"
    fake.push({ actionType: 'voiceChangedEvent', actionObject: { voiceID: 'robot' } });
    await wait(() => plugin.vars['voicechanger.voice'] === 'robot', 'voice event');
    assert.deepEqual([...plugin.sources.voices({ text: '' }).options.map((o) => o.label)], ['Deep', 'Robot', 'Alien', 'Baby']);
    assert.equal(plugin.sources.voices({ text: '' }).options[1].group, 'Recent');
    assert.deepEqual([...plugin.sources.voices({ text: 'RO' }).options.map((o) => o.label)], ['Robot']);
    const p1 = plugin.sources.voices({ text: '', offset: 0, limit: 2 });
    assert.equal(p1.options.length, 2); assert.equal(p1.more, true);
    assert.equal(plugin.sources.voices({ text: '', offset: 2, limit: 2 }).more, false);
    assert.equal(plugin.sources.voices({ text: '', selected: 'alien', limit: 50 }).selectedMissing, false);
    assert.equal(plugin.sources.voices({ text: '', selected: 'gone' }).selectedMissing, true);
    assert.ok(plugin.sources.voices({ text: '', selected: 'baby', offset: 0, limit: 2 }).options.some((o) => o.value === 'baby'), 'the selected voice is on the first page');
    assert.ok(plugin.sources.voices({ text: '', limit: 9999 }).options.length <= 50);
  } finally { plugin.stop(); fake.close(); }
});

test('sounds source: grouped by soundboard, searchable', async () => {
  const { fake, plugin } = await withLists();
  try {
    const all = plugin.sources.sounds({ text: '' });
    assert.deepEqual([...all.options.map((o) => o.group + '/' + o.label)], ['Funny/Air horn', 'Funny/Laugh', 'Alerts/Nuke alert']);
    assert.deepEqual([...plugin.sources.sounds({ text: 'nuke' }).options.map((o) => o.value)], ['s-nuke']);
  } finally { plugin.stop(); fake.close(); }
});

test('sources: clear errors when no key, not running, loading', async () => {
  const noKey = loadPlugin({ portMap: {}, apiKey: '' });
  assert.match(noKey.sources.voices({ text: '' }).error, /API key/);
  const down = loadPlugin({ portMap: {} });
  assert.match(down.sources.sounds({ text: '' }).error, /not running/);
  const { fake, plugin } = await connected();
  try {
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    assert.match(plugin.sources.voices({ text: '' }).error, /loading/);
  } finally { plugin.stop(); fake.close(); }
});

test('actions: NotFound for a removed voice or sound, the id of a listed one is sent', async () => {
  const { fake, plugin } = await withLists();
  try {
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.loadvoice'].run({}, { voiceID: 'gone' }).code, 'NotFound');
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 'gone' }).code, 'NotFound');
    assert.equal(plugin.actions['voicechanger.loadvoice'].run({}, { voiceID: '' }).code, 'NotConfigured');
    assert.equal(plugin.actions['voicechanger.loadvoice'].run({}, { voiceID: 'alien' }).ok, true);
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 's-laugh' }).ok, true);
    await wait(() => fake.received.some((m) => m.action === 'playMeme'), 'playMeme');
    assert.deepEqual(fake.received.find((m) => m.action === 'playMeme').payload, { FileName: 's-laugh', IsKeyDown: true });
    assert.deepEqual(fake.received.find((m) => m.action === 'loadVoice').payload, { voiceID: 'alien' });
  } finally { plugin.stop(); fake.close(); }
});

test('actions: the pickers are searchable fields with an options source', () => {
  const plugin = loadPlugin({ portMap: {} });
  for (const [type, key, source] of [['voicechanger.loadvoice', 'voiceID', 'voices'], ['voicechanger.playsound', 'fileName', 'sounds']]) {
    const field = plugin.actions[type].fields.find((x) => x.key === key);
    assert.equal(field.kind, 'Select'); assert.equal(field.searchable, true); assert.equal(field.optionsSource, source);
  }
});

test('an oversized reply (over the plugin.json limit): the host closes the socket, that list goes off, Refresh lists tries again', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    const voices = Array.from({ length: 4000 }, (_, i) => ({ id: 'voice' + i, friendlyName: 'Voice number ' + i, bitmapChecksum: '9af56b7cb7a87db7b9d4e93852599981', pad: 'x'.repeat(40) }));
    const big = JSON.stringify({ actionType: 'getVoices', actionObject: { voices } });
    assert.ok(big.length > CAP, 'size ' + big.length);
    fake.push(JSON.parse(big));
    await wait(() => /too large/.test(plugin.last().text), 'too large status');
    assert.equal(plugin.open(), 0);
    plugin.advance(7000); plugin.tick();
    await wait(() => plugin.last().level === 'Ok', 'reconnected');
    await wait(() => fake.received.filter((m) => m.action === 'getAllSoundboard').length === 1, 'only the sounds are asked for');
    assert.equal(fake.received.filter((m) => m.action === 'getVoices').length, 1, 'the big list is not asked for again');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [] } });
    assert.match(plugin.sources.voices({ text: '' }).error, /too large/);
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.refreshvoices'].run({}, {}).ok, true);
    await wait(() => fake.received.filter((m) => m.action === 'getVoices').length === 2, 'asked again after Refresh lists');
  } finally { plugin.stop(); fake.close(); }
});

test('a realistic soundboard list of 500 sounds (about 134 KB) fits the limit and is searchable', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    fake.push({ actionType: 'getVoices', actionObject: { voices: VOICES } });
    await wait(() => fake.received.some((m) => m.action === 'getAllSoundboard'), 'second list');
    const sounds = Array.from({ length: 500 }, (_, i) => ({ id: 'ad6efa6b-1111-2222-3333-4444555566' + String(i).padStart(2, '0'), name: 'Sound ' + i, isCustom: false, enabled: true, playbackMode: 'PlayRestart', loop: false, muteOtherSounds: false, muteVoice: false, showProLogo: false, bitmapChecksum: '14-04-DB-12-50-09-25-14-18-C5-62-2D-4A-AB-8D-31' }));
    const reply = JSON.stringify({ actionType: 'getAllSoundboard', actionObject: { soundboards: [{ id: 'x', name: 'Mine', sounds }] } });
    assert.ok(reply.length > 64 * 1024 && reply.length < CAP, 'size ' + reply.length);
    fake.push(JSON.parse(reply));
    await wait(() => plugin.sources.sounds({ text: '' }).options.length > 0, 'sound list');
    const first = plugin.sources.sounds({ text: '', limit: 50 });
    assert.equal(first.options.length, 50); assert.equal(first.more, true);
    assert.equal(plugin.sources.sounds({ text: 'Sound 49', limit: 50 }).options.length, 11);
  } finally { plugin.stop(); fake.close(); }
});

// ---- button pictures ----
function png(width, height, extra = 40) {
  const ihdr = Buffer.alloc(25);
  ihdr.writeUInt32BE(13, 0); ihdr.write('IHDR', 4); ihdr.writeUInt32BE(width, 8); ihdr.writeUInt32BE(height, 12);
  return Buffer.concat([Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]), ihdr, Buffer.alloc(extra, 1)]).toString('base64');
}
const PNG = png(64, 64);
const bitmaps = (fake) => fake.received.filter((m) => m.action === 'getBitmap');

test('pictures: options carry stable plugin-image references (the item, not the picture)', async () => {
  const { fake, plugin } = await withLists();
  try {
    const voices = plugin.sources.voices({ text: '' }).options;
    assert.equal(voices.find((o) => o.value === 'baby').icon, 'plugin-image:voicechanger/voice:baby');
    const sounds = plugin.sources.sounds({ text: '' }).options;
    assert.equal(sounds.find((o) => o.value === 's-laugh').icon, 'plugin-image:voicechanger/sound:s-laugh');
    assert.equal(sounds.find((o) => o.value === 's-airhorn').icon, 'plugin-image:voicechanger/sound:s-airhorn', 'the id does not depend on the checksum');
  } finally { plugin.stop(); fake.close(); }
});

test('pictures: one getBitmap at a time, only for ids the plugin knows, a valid picture is put', async () => {
  const { fake, plugin } = await withLists();
  try {
    plugin.sources.voices({ text: '' }); plugin.sources.sounds({ text: '' });
    plugin.requestImages(['voice:baby', 'sound:s-laugh', 'voice:unknown', 'nope:x', 7]);
    plugin.advance(1100);
    plugin.pump();
    await wait(() => bitmaps(fake).length === 1, 'first getBitmap');
    assert.deepEqual(bitmaps(fake)[0].payload, { voiceID: 'baby' });
    plugin.pump(); plugin.pump();
    await new Promise((r) => setTimeout(r, 50));
    assert.equal(bitmaps(fake).length, 1, 'the next one waits for the reply');
    fake.push({ actionType: 'getBitmap', actionObject: { default: PNG, selected: PNG, transparent: false } });
    await wait(() => plugin.puts.length === 1, 'put');
    assert.equal(plugin.puts[0].id, 'voice:baby');
    assert.equal(plugin.puts[0].version, undefined, 'no checksum in the list, no version');
    assert.match(plugin.puts[0].url, /^data:image\/png;base64,iVBOR/);
    plugin.pump();
    await wait(() => bitmaps(fake).length === 2, 'second getBitmap');
    assert.deepEqual(bitmaps(fake)[1].payload, { memeId: 's-laugh' });
    assert.equal(bitmaps(fake).length, 2, 'unknown ids are never asked for');
    assert.deepEqual([...plugin.logs], [], 'nothing about ids or pictures is logged');
  } finally { plugin.stop(); fake.close(); }
});

test('pictures: svg, a non-picture and a picture over 256 KB are not put', async () => {
  const { fake, plugin } = await withLists();
  try {
    plugin.sources.voices({ text: '' });
    const big = png(64, 64, 300 * 1024);
    const bad = [Buffer.from('<svg xmlns="http://www.w3.org/2000/svg"></svg>').toString('base64'), 'bm90IGEgcGljdHVyZSBhdCBhbGw=', big];
    const ids = ['voice:baby', 'voice:deep', 'voice:robot'];
    plugin.requestImages(ids);
    for (let i = 0; i < 3; i++) {
      plugin.advance(1100);
      plugin.pump();
      await wait(() => bitmaps(fake).length === i + 1, 'getBitmap ' + i);
      fake.push({ actionType: 'getBitmap', actionObject: { default: bad[i] } });
      await new Promise((r) => setTimeout(r, 40));
    }
    assert.equal(plugin.puts.length, 0);
  } finally { plugin.stop(); fake.close(); }
});

test('pictures: at most 4 requests a second and a queue of 50 that drops the oldest', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    const voices = Array.from({ length: 80 }, (_, i) => ({ id: 'v' + i, friendlyName: 'Voice ' + String(i).padStart(2, '0') }));
    fake.push({ actionType: 'getVoices', actionObject: { voices } });
    await wait(() => fake.received.some((m) => m.action === 'getAllSoundboard'), 'second list');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [] } });
    await wait(() => plugin.sources.voices({ text: '' }).options.length > 0, 'lists');
    plugin.requestImages(voices.slice(0, 20).map((v) => 'voice:' + v.id));
    plugin.requestImages(voices.slice(20, 40).map((v) => 'voice:' + v.id));
    plugin.requestImages(voices.slice(40, 60).map((v) => 'voice:' + v.id));
    plugin.requestImages(voices.slice(60, 80).map((v) => 'voice:' + v.id));
    plugin.advance(1100);
    let answered = 0;
    for (let i = 0; i < 12; i++) { // inside one fake second
      plugin.pump();
      await new Promise((r) => setTimeout(r, 20));
      if (bitmaps(fake).length > answered) { answered = bitmaps(fake).length; fake.push({ actionType: 'getBitmap', actionObject: { default: PNG } }); await new Promise((r) => setTimeout(r, 20)); }
    }
    assert.ok(bitmaps(fake).length <= 4, 'sent ' + bitmaps(fake).length);
    // let time pass until the queue is empty
    for (let i = 0; i < 80; i++) {
      plugin.advance(300);
      plugin.pump();
      await new Promise((r) => setTimeout(r, 15));
      if (bitmaps(fake).length > answered) { answered = bitmaps(fake).length; fake.push({ actionType: 'getBitmap', actionObject: { default: PNG } }); await new Promise((r) => setTimeout(r, 15)); }
    }
    const asked = bitmaps(fake).map((m) => m.payload.voiceID);
    assert.equal(asked.length, 50, 'the queue holds 50');
    assert.ok(!asked.includes('v0') && asked.includes('v79'), 'the oldest were dropped');
  } finally { plugin.stop(); fake.close(); }
});

test('pictures: transparent first, then default, never selected; bare base64 and a data: prefix both work; the reply layouts', async () => {
  const { fake, plugin } = await withLists();
  try {
    const t = png(10, 10, 5), d = png(20, 20, 5), sel = png(30, 30, 5);
    const replies = [
      { result: { default: d, selected: sel, transparent: t } }, // expect t
      { result: { default: d, selected: sel } }, // expect d
      { result: { selected: sel } }, // nothing
      { default: d, transparent: 'data:image/png;base64,' + t }, // actionObject itself, data: prefix
      { 1: { result: { image: d } } }, // meme layout of the docs
    ];
    const expected = [t, d, null, t, d];
    const ids = ['voice:baby', 'voice:deep', 'voice:robot', 'voice:alien', 'sound:s-laugh'];
    plugin.requestImages(ids);
    for (let i = 0; i < replies.length; i++) {
      plugin.advance(1100); plugin.pump();
      await wait(() => bitmaps(fake).length === i + 1, 'getBitmap ' + i);
      fake.push({ actionType: 'getBitmap', actionObject: replies[i] });
      await new Promise((r) => setTimeout(r, 40));
    }
    const got = new Map(plugin.puts.map((p) => [p.id, p.url]));
    ids.forEach((id, i) => {
      if (expected[i] === null) assert.equal(got.has(id), false, id);
      else assert.equal(got.get(id), 'data:image/png;base64,' + expected[i], id);
    });
  } finally { plugin.stop(); fake.close(); }
});

test('pictures: a png over 1024 pixels on a side is not put; other types are sniffed', async () => {
  const { fake, plugin } = await withLists();
  try {
    const jpeg = Buffer.concat([Buffer.from([0xff, 0xd8, 0xff, 0xe0]), Buffer.alloc(30, 1)]).toString('base64');
    const webp = Buffer.concat([Buffer.from('RIFF'), Buffer.alloc(4), Buffer.from('WEBP'), Buffer.alloc(30, 1)]).toString('base64');
    const gif = Buffer.concat([Buffer.from('GIF89a'), Buffer.alloc(30, 1)]).toString('base64');
    const cases = [[png(2000, 10), null], [png(1024, 1024), 'png'], [jpeg, 'jpeg'], [webp, 'webp'], [gif, 'gif']];
    const ids = ['voice:baby', 'voice:deep', 'voice:robot', 'voice:alien', 'sound:s-laugh'];
    plugin.requestImages(ids);
    for (let i = 0; i < cases.length; i++) {
      plugin.advance(1100); plugin.pump();
      await wait(() => bitmaps(fake).length === i + 1, 'getBitmap ' + i);
      fake.push({ actionType: 'getBitmap', actionObject: { result: { transparent: cases[i][0] } } });
      await new Promise((r) => setTimeout(r, 40));
    }
    const got = new Map(plugin.puts.map((p) => [p.id, p.url]));
    assert.equal(got.has('voice:baby'), false);
    cases.slice(1).forEach(([, type], i) => assert.match(got.get(ids[i + 1]), new RegExp('^data:image/' + type + ';base64,')));
  } finally { plugin.stop(); fake.close(); }
});

test('pictures: image ids encode every unusual character and ignore the checksum', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    fake.push({ actionType: 'getVoices', actionObject: { voices: [{ id: 'my voice/1_x', friendlyName: 'Mine', bitmapChecksum: 'ab-cd' }, { id: 'plain', friendlyName: 'Plain' }] } });
    await wait(() => fake.received.some((m) => m.action === 'getAllSoundboard'), 'second list');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [] } });
    await wait(() => plugin.sources.voices({ text: '' }).options.length > 0, 'lists');
    const icons = Object.fromEntries(plugin.sources.voices({ text: '' }).options.map((o) => [o.value, o.icon]));
    assert.equal(icons['my voice/1_x'], 'plugin-image:voicechanger/voice:my_0020voice_002f1_005fx');
    assert.equal(icons.plain, 'plugin-image:voicechanger/voice:plain');
    for (const icon of Object.values(icons)) assert.match(icon.split('/')[1], /^[A-Za-z0-9._:-]{1,128}$/);
  } finally { plugin.stop(); fake.close(); }
});

test('lists: read after a successful register and on Refresh lists; an empty list is read again at most every 5 minutes', async () => {
  const { fake, plugin } = await connected();
  try {
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    fake.push({ actionType: 'getVoices', actionObject: { voices: [] } });
    await wait(() => fake.received.some((m) => m.action === 'getAllSoundboard'), 'second list');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [] } });
    await new Promise((r) => setTimeout(r, 50));
    const voicesAsked = () => fake.received.filter((m) => m.action === 'getVoices').length;
    assert.equal(voicesAsked(), 1);
    plugin.advance(1100);
    for (let i = 0; i < 5; i++) plugin.sources.voices({ text: 'x' }); // many searches, one new read
    await wait(() => voicesAsked() === 2, 'one refresh');
    fake.push({ actionType: 'getVoices', actionObject: { voices: [] } });
    await wait(() => fake.received.filter((m) => m.action === 'getAllSoundboard').length === 2, 'sounds again');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [] } });
    for (let i = 0; i < 5; i++) plugin.sources.sounds({ text: 'x' });
    await new Promise((r) => setTimeout(r, 50));
    assert.equal(voicesAsked(), 2, 'not again inside 5 minutes');
    plugin.advance(5 * 60 * 1000 + 1000);
    plugin.sources.voices({ text: 'x' });
    await wait(() => voicesAsked() === 3, 'after 5 minutes');
  } finally { plugin.stop(); fake.close(); }
});

test('lists: the measured sizes of a real Voicemod fit the limit of plugin.json', () => {
  assert.equal(manifest.limits.wsMessageKB, 512);
  assert.ok(328812 < CAP && 328812 * 1.4 < CAP, 'the biggest measured reply (getAllSoundboard, 860 sounds) fits with room');
});

test('pictures: a changed bitmapChecksum in a refreshed list drops the cached picture; an unchanged one does not', async () => {
  const { fake, plugin } = await connected();
  try {
    const give = async (voices) => {
      give.n = (give.n || 0) + 1;
      await wait(() => fake.received.filter((m) => m.action === 'getVoices').length === give.n, 'getVoices ' + give.n);
      fake.push({ actionType: 'getVoices', actionObject: { voices } });
      await wait(() => fake.received.filter((m) => m.action === 'getAllSoundboard').length === give.n, 'sounds ' + give.n);
      fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [{ id: 'b', name: 'B', sounds: [{ id: 's1', name: 'One', bitmapChecksum: 'c1' }] }] } });
      await new Promise((r) => setTimeout(r, 40));
    };
    await give([{ id: 'a', friendlyName: 'A', bitmapChecksum: 'v1' }, { id: 'b', friendlyName: 'B', bitmapChecksum: 'v1' }]);
    plugin.requestImages(['voice:a']);
    plugin.advance(1100); plugin.pump();
    await wait(() => bitmaps(fake).length === 1, 'getBitmap');
    fake.push({ actionType: 'getBitmap', actionObject: { result: { transparent: PNG } } });
    await wait(() => plugin.puts.length === 1, 'put');
    assert.equal(plugin.puts[0].version, 'v1', 'the checksum is the version');
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.refreshvoices'].run({}, {}).ok, true);
    await give([{ id: 'a', friendlyName: 'A', bitmapChecksum: 'v2' }, { id: 'b', friendlyName: 'B', bitmapChecksum: 'v1' }]);
    assert.deepEqual([...plugin.invalidated], ['voice:a@v2'], 'only the voice with a new checksum, with the new version');
  } finally { plugin.stop(); fake.close(); }
});

// ---- variables ----
test('variables: the old names stay, every name is described and under the plugin id, types are right', () => {
  const plugin = loadPlugin({ portMap: {} });
  const names = plugin.described.map((d) => d.name);
  for (const old of ['voicechanger.enabled', 'voicechanger.micmuted', 'voicechanger.voice']) assert.ok(names.includes(old), old);
  for (const n of ['voicename', 'hearmyself', 'background', 'memesmuted', 'license', 'state', 'voicecount', 'soundcount', 'lastsound', 'lasterror']) assert.ok(names.includes('voicechanger.' + n), n);
  for (const d of plugin.described) { assert.match(d.name, /^voicechanger\.[a-z]+$/); assert.ok(d.description.length > 0); }
  const type = (n) => plugin.described.find((d) => d.name === n).type;
  for (const n of ['enabled', 'micmuted', 'hearmyself', 'background', 'memesmuted']) assert.equal(type('voicechanger.' + n), 'boolean');
  for (const n of ['voicecount', 'soundcount']) assert.equal(type('voicechanger.' + n), 'number');
  for (const n of Object.keys(plugin.vars)) assert.ok(names.includes(n), 'described: ' + n);
});

test('variables: the switches, the license and the counts follow the events and the lists', async () => {
  const { fake, plugin } = await withLists();
  try {
    fake.push({ actionType: 'getHearMyselfStatus', actionObject: { value: true } });
    fake.push({ actionType: 'toggleBackground', actionObject: { value: true } });
    fake.push({ actionID: 'x', actionType: 'toggleMuteMemeForMe', actionObject: { value: true } });
    fake.push({ actionType: 'licenseTypeChanged', actionObject: { licenseType: 'PRO' } });
    await wait(() => plugin.vars['voicechanger.license'] === 'pro', 'license');
    assert.equal(plugin.vars['voicechanger.hearmyself'], true);
    assert.equal(plugin.vars['voicechanger.background'], true);
    assert.equal(plugin.vars['voicechanger.memesmuted'], true);
    assert.equal(plugin.vars['voicechanger.micmuted'], undefined, 'the sounds mute is not the microphone');
    assert.equal(plugin.vars['voicechanger.voicecount'], 4);
    assert.equal(plugin.vars['voicechanger.soundcount'], 3);
    fake.push({ actionType: 'voiceChangerDisabledEvent', actionObject: {} });
    await wait(() => plugin.vars['voicechanger.enabled'] === false, 'disabled event');
    fake.push({ actionType: 'voiceChangerEnabledEvent', actionObject: {} });
    await wait(() => plugin.vars['voicechanger.enabled'] === true, 'enabled event');
  } finally { plugin.stop(); fake.close(); }
});

test('variables: the voice name follows the voice id and the list; the last sound is the name of the sound played', async () => {
  const { fake, plugin } = await withLists();
  try {
    fake.push({ actionType: 'voiceChangedEvent', actionObject: { voiceID: 'robot' } });
    await wait(() => plugin.vars['voicechanger.voicename'] === 'Robot', 'name from the list');
    fake.push({ actionType: 'voiceChangedEvent', actionObject: { voiceID: 'unknown' } });
    await wait(() => plugin.vars['voicechanger.voice'] === 'unknown', 'id');
    assert.equal(plugin.vars['voicechanger.voicename'], '');
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 's-airhorn' }).ok, true);
    assert.equal(plugin.vars['voicechanger.lastsound'], 'Air horn');
  } finally { plugin.stop(); fake.close(); }
});

test('variables: state is the status text; lasterror holds a failure and is cleared when connected', async () => {
  const plugin0 = loadPlugin({ portMap: {}, apiKey: '' });
  plugin0.tick();
  assert.match(plugin0.vars['voicechanger.state'], /API key/);
  assert.match(plugin0.vars['voicechanger.lasterror'], /API key/);
  const { fake, plugin } = await connected();
  try {
    assert.equal(plugin.actions['voicechanger.togglemute'].run({}, {}).ok, false);
    assert.match(plugin.vars['voicechanger.lasterror'], /Not connected/);
    await wait(() => plugin.last()?.level === 'Ok', 'connected');
    assert.equal(plugin.vars['voicechanger.state'], 'connected');
    assert.equal(plugin.vars['voicechanger.lasterror'], '');
    assert.equal(plugin.actions['voicechanger.loadvoice'].run({}, { voiceID: '' }).ok, false);
    assert.match(plugin.vars['voicechanger.lasterror'], /Choose a voice/);
    const queries = fake.received.map((m) => m.action);
    for (const q of ['getHearMyselfStatus', 'getBackgroundEffectStatus', 'getMuteMemeForMeStatus']) assert.ok(queries.includes(q), q);
  } finally { plugin.stop(); fake.close(); }
});

test('sound sources: library, community and my soundboards; each list is read the first time its source is opened and again after Refresh lists', async () => {
  const { fake, plugin } = await connected({ lazy: true });
  try {
    fake.memes = [{ Name: 'Horn', FileName: '80', Type: 'PlayStop' }];
    await wait(() => fake.received.some((m) => m.action === 'getVoices'), 'getVoices');
    fake.push({ actionType: 'getVoices', actionObject: { voices: VOICES } });
    await new Promise((r) => setTimeout(r, 150));
    const asked = () => fake.received.map((m) => m.action);
    assert.ok(!asked().includes('getAllSoundboard') && !asked().includes('getMemes'), 'nothing is read before a picker opens a source');
    assert.match(plugin.sources.sounds({ text: '' }, { source: 'community' }).error, /loading/);
    await wait(() => asked().includes('getMemes'), 'community opens getMemes');
    assert.ok(!asked().includes('getAllSoundboard'), 'only the community list');
    await wait(() => plugin.sources.sounds({ text: '' }, { source: 'community' }).options.length === 1, 'community items');
    assert.deepEqual([...plugin.sources.sounds({ text: '' }, { source: 'community' }).options.map((o) => o.value)], ['meme:80']);
    plugin.sources.sounds({ text: '' }, { source: 'library' });
    await wait(() => asked().includes('getAllSoundboard'), 'library opens getAllSoundboard');
    fake.push({ actionType: 'getAllSoundboard', actionObject: { soundboards: [
      { id: 'b1', name: 'Stock', isCustom: false, sounds: [{ id: 'g-1', name: 'Stock one' }] },
      { id: 'b2', name: 'Mine', isCustom: true, sounds: [{ id: 'g-2', name: 'My one' }] },
    ] } });
    await wait(() => plugin.sources.sounds({ text: '' }, { source: 'library' }).options.length === 1, 'library items');
    assert.deepEqual([...plugin.sources.sounds({ text: '' }, { source: 'library' }).options.map((o) => [o.value, o.group])], [['g-1', 'Stock']]);
    assert.deepEqual([...plugin.sources.sounds({ text: '' }, { source: 'mine' }).options.map((o) => [o.value, o.group])], [['g-2', 'Mine']]);
    assert.equal(plugin.sources.sounds({ text: '' }, {}).options.length, 3, 'no source (a 0.2.0 button) searches all sources');
    assert.equal(plugin.sources.sounds({ text: '' }, { source: 'mine' }).error, undefined);
    // a played value needs no loaded list
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.playsound'].run({}, { fileName: 'meme:80' }).ok, true);
    // Refresh lists reads what was opened again
    plugin.advance(1100);
    assert.equal(plugin.actions['voicechanger.refreshvoices'].run({}, {}).ok, true);
    await wait(() => asked().filter((a) => a === 'getVoices').length === 2, 'voices again');
    fake.push({ actionType: 'getVoices', actionObject: { voices: VOICES } });
    await wait(() => asked().filter((a) => a === 'getAllSoundboard').length === 2, 'sounds again');
    const field = plugin.actions['voicechanger.playsound'].fields.find((f) => f.key === 'fileName');
    assert.deepEqual([...field.dependsOn], ['source']);
  } finally { plugin.stop(); fake.close(); }
});
