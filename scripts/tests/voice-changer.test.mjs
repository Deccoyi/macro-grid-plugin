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
const GUID = '258EAFA5-E914-47DA-95CA-C5AB0DC85B11';
const KEY = 'secret-test-key';

// ---- fake Voicemod ----
function frame(text) {
  const body = Buffer.from(text);
  const head = body.length < 126 ? Buffer.from([0x81, body.length]) : Buffer.from([0x81, 126, body.length >> 8, body.length & 255]);
  return Buffer.concat([head, body]);
}

function fakeVoicemod({ validKey = KEY } = {}) {
  const received = [];
  const sockets = new Set();
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
        if (msg.action === 'registerClient') {
          const ok = msg.payload.clientKey === validKey;
          sock.write(frame(JSON.stringify({ id: msg.id, action: 'registerClient', payload: { status: { code: ok ? 200 : 401, description: ok ? 'ok' : 'Invalid key' } } })));
        }
      }
    });
  });
  return {
    received,
    push: (obj) => { for (const s of sockets) s.write(frame(JSON.stringify(obj))); },
    listen: () => new Promise((res) => server.listen(0, '127.0.0.1', () => res(server.address().port))),
    close: () => { for (const s of sockets) s.destroy(); server.close(); },
  };
}

// ---- the plugin under a fake host ----
function loadPlugin({ portMap, apiKey = KEY }) {
  let now = 1_000_000;
  const settings = { apiKey };
  const vars = {}, statuses = [], reports = [], actions = {}, every = [], connectPorts = [], clientSockets = [];
  const host = {
    variables: { describe() {}, set: (n, v) => { vars[n] = v; } },
    settings: { page() {}, get: () => settings },
    status: (id, text, level) => statuses.push({ text, level }),
    diagnostics: { report: (level, message, key) => reports.push({ level, message, key }), resolve() {} },
    registerAction: (a) => { actions[a.type] = a; },
    every: (ms, fn) => every.push(fn),
    after() {},
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
        ws.onopen = () => handlers.onOpen();
        ws.onmessage = (e) => handlers.onMessage(String(e.data));
        ws.onclose = () => handlers.onClose({ code: 1000, reason: '', error: null });
        ws.onerror = () => {};
        return { send: (t) => ws.send(t), close: () => ws.close() };
      },
    },
  };
  vm.runInNewContext(script, { host, Date: { now: () => now }, JSON, Number, String, Array, Math });
  return {
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

async function connected({ validKey } = {}) {
  const fake = fakeVoicemod({ validKey });
  const port = await fake.listen();
  const plugin = loadPlugin({ portMap: { 59129: port } });
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
