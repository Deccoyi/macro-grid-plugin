// Voice Changer Control: controls the Voicemod app on this PC through its local Control API (a plain web socket on this computer).
// Independent plugin, not made, endorsed or affiliated with Voicemod. The person brings their own Control API key; it is a Password
// setting, so the server stores it protected and the editor never shows it. The key is only ever sent to the app on this PC and is never logged.

const TUTORIAL_URL = 'https://deccoyi.github.io/macro-grid/tutorials/voicemod';
// The app listens on the first port and uses the others only when that one is taken.
const PORTS = [59129, 20000, 39273, 42152, 43782, 46667, 35679, 37170, 38501, 33952, 30546];
// The host allows 10 connects a minute per plugin, so attempts are at least 7 seconds apart.
const RETRY_MS = 7000;
const RETRY_SLOW_MS = 30000;
const SLOW_AFTER = 22;
const MAX_CONNECTS_PER_MINUTE = 8; // the host stops a plugin at 10
const REGISTER_TIMEOUT_MS = 5000;
const TICK_MS = 1000;
// Host limits this plugin stays under (see the table in the README): 20 messages a second out, 80 characters in a status text, no cap on variable writes.
const MAX_SENDS_PER_SECOND = 10;
const MAX_STATUS_CHARS = 80;
const MAX_VARIABLE_CHARS = 8000;
const LOG_EVERY_MS = 10000;
const MAX_PICTURE_QUEUE = 50;
const PICTURES_PER_SECOND = 4;
const PICTURE_TIMEOUT_MS = 5000;
const MAX_PICTURE_BYTES = 256 * 1024; // the host accepts at most 256 KB decoded and 1024 x 1024 pixels
const MAX_PICTURE_SIDE = 1024;
const EMPTY_REFRESH_MS = 5 * 60 * 1000; // a search that finds an empty list reads the lists again, at most this often
const PICTURE_PUMP_MS = 250;

host.variables.describe([
  { name: 'voicechanger.enabled', description: 'Whether the voice changer is on', example: 'true', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.micmuted', description: 'Whether the microphone is muted in the app', example: 'false', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.voice', description: 'The id of the current voice', example: 'nofx', category: 'Voice Changer' },
  { name: 'voicechanger.voicename', description: 'The name of the current voice', example: 'Baby', category: 'Voice Changer' },
  { name: 'voicechanger.hearmyself', description: 'Whether you hear your own voice (hear myself)', example: 'true', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.background', description: 'Whether the background effect is on', example: 'false', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.memesmuted', description: 'Whether the sounds are muted for you (mute for me)', example: 'false', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.license', description: 'The Voicemod license: free or pro', example: 'pro', category: 'Voice Changer' },
  { name: 'voicechanger.state', description: 'The state of the connection to the Voicemod app, as a short text', example: 'connected', category: 'Voice Changer' },
  { name: 'voicechanger.voicecount', description: 'How many voices the Voicemod app has', example: '257', type: 'number', category: 'Voice Changer' },
  { name: 'voicechanger.soundcount', description: 'How many sounds the Voicemod app has', example: '860', type: 'number', category: 'Voice Changer' },
  { name: 'voicechanger.lastsound', description: 'The name of the sound played last by this plugin', example: 'Air horn', category: 'Voice Changer' },
  { name: 'voicechanger.lasterror', description: 'The last problem as a short text (empty when there is none)', example: '', category: 'Voice Changer' },
]);

// A boolean variable shows Açık/Kapalı by default; the text of a widget can pick its own words, for example {voicechanger.enabled|ON/OFF}.

host.settings.page([
  { key: 'notice', label: 'Unofficial', kind: 'Notice', description: 'This plugin works with the Voicemod app on this PC. It is an independent plugin and is not made, endorsed or affiliated with Voicemod. Voicemod is a trademark of its owner. It needs your own Voicemod Control API key.' },
  { key: 'apiKey', label: 'API key', kind: 'Password', description: 'Your own Voicemod Control API key. How to get a key: ' + TUTORIAL_URL },
]);

const state = {
  socket: null,
  open: false,
  registered: false,
  openedAt: 0,
  keyUsed: '',
  rejectedKey: null, // the key Voicemod answered 401 to; no new attempt until the key changes
  attempts: 0,
  nextAt: 0,
  lastGoodPort: null,
  nextId: 1,
  registerId: null,
  connects: [], // timestamps of the connects of the last minute
  // The voice and sound lists live in memory only. A big reply is read because plugin.json asks the host for a larger message limit; when the host
  // still closes the socket on a reply (the limit is reached), that list is switched off until "Refresh lists".
  lists: { voices: null, sounds: null },
  wanted: { sounds: false },
  tooBig: { voices: false, sounds: false },
  pending: null, // the list whose reply is awaited
  queue: [],
  recent: [], // the last voices used, newest first
  // Button pictures: the host asks for image ids (voice:<id>, sound:<checksum>); each is one getBitmap, one at a time.
  imageSources: new Map(), // image id -> { kind: 'voice' | 'sound', id }
  pictureQueue: [],
  pictureInFlight: null, // { imageId, at }
  pictureSends: [], // timestamps of the getBitmap requests of the last second
  pictureFailures: 0,
  picturesOff: false,
  lastEmptyRefresh: 0,
  sends: [], // timestamps of the messages sent in the last second
  lastStatus: '',
  lastLog: 0,
  vars: {}, // the last value written to each variable: unchanged values are not written again
};

// A callback must never throw: the host switches a plugin off after five failures in a row. The message is not shown, it could hold the reply.
function guard(fn) {
  try { fn(); } catch (e) {
    setStatus('internal error, see the plugin log', 'Error');
    if (Date.now() - state.lastLog > LOG_EVERY_MS) { state.lastLog = Date.now(); host.log('callback failed: ' + (e && e.name ? e.name : 'error')); }
  }
}

// The host cuts a status text at 80 characters, so the long explanation goes to the Error List. An unchanged status is not sent again.
function setStatus(text, level) {
  const full = ('Voice Changer: ' + text).slice(0, MAX_STATUS_CHARS);
  if (full + level === state.lastStatus) return;
  state.lastStatus = full + level;
  host.status('voicechanger', full, level);
  setVar('voicechanger.state', String(text).slice(0, 80));
  if (level === 'Error' || level === 'Warning') setVar('voicechanger.lasterror', String(text).slice(0, 80));
  else if (level === 'Ok') setVar('voicechanger.lasterror', '');
}

function setVar(name, value) {
  if (typeof value === 'string' && value.length > MAX_VARIABLE_CHARS) value = value.slice(0, MAX_VARIABLE_CHARS);
  if (state.vars[name] === value) return;
  state.vars[name] = value;
  host.variables.set(name, value);
}

// The host allows 20 messages a second and throws when the socket is closed; both end here as a refusal, never as a failure of the plugin.
function rawSend(socket, text) {
  const now = Date.now();
  state.sends = state.sends.filter((t) => now - t < 1000);
  if (state.sends.length >= MAX_SENDS_PER_SECOND) return false;
  state.sends.push(now);
  try { socket.send(text); } catch (e) { return false; }
  return true;
}

function currentKey() { return String(host.settings.get().apiKey || '').trim(); }

function send(action, payload) {
  if (!state.registered) return false;
  return rawSend(state.socket, JSON.stringify({ id: String(state.nextId++), action, payload: payload || {} }));
}

function portForAttempt(n) {
  if (n === 0 && state.lastGoodPort) return state.lastGoodPort;
  // The first port stays in the rotation every second attempt, because the app uses it in nearly every case.
  return n % 2 === 0 ? PORTS[0] : PORTS[1 + ((n - 1) >> 1) % (PORTS.length - 1)];
}

function connect(key) {
  const port = portForAttempt(state.attempts);
  state.keyUsed = key;
  state.connects = state.connects.filter((t) => Date.now() - t < 60000);
  if (state.connects.length >= MAX_CONNECTS_PER_MINUTE) { state.nextAt = state.connects[0] + 60000; return; }
  state.connects.push(Date.now());
  state.open = false;
  state.registered = false;
  let socket;
  try {
    socket = host.ws.connect('ws://localhost:' + port + '/v1/', {
      onOpen() { guard(() => onOpen(socket, port, key)); },
      onMessage(text) { guard(() => { if (state.socket === socket) onMessage(text, port); }); },
      onClose() { guard(() => onClose(socket)); },
    });
  } catch (e) {
    state.attempts++;
    state.nextAt = Date.now() + RETRY_SLOW_MS;
    setStatus('cannot open a connection', 'Error');
    host.diagnostics.report('error', 'The connection to the Voicemod app was refused by Macro Grid: ' + (e && e.message ? e.message : 'refused'), 'voicechanger-connect');
    return;
  }
  state.socket = socket;
}

function onOpen(socket, port, key) {
  if (state.socket !== socket) return;
  state.open = true;
  state.openedAt = Date.now();
  state.registerId = String(state.nextId++);
  setStatus('connecting', 'Busy');
  // The registerClient message must be the first one on the connection.
  rawSend(socket, JSON.stringify({ id: state.registerId, action: 'registerClient', payload: { clientKey: key } }));
}

function onClose(socket) {
  if (state.socket !== socket) return;
  const wasRegistered = state.registered;
  state.socket = null;
  state.open = false;
  state.registered = false;
  if (state.rejectedKey !== null && state.rejectedKey === state.keyUsed) return; // the 401 message is already shown
  state.attempts = wasRegistered ? 0 : state.attempts + 1;
  state.nextAt = Date.now() + (state.attempts > SLOW_AFTER ? RETRY_SLOW_MS : RETRY_MS);
  if (state.pictureInFlight) {
    state.pictureInFlight = null;
    if (++state.pictureFailures >= 2) {
      state.picturesOff = true;
      host.diagnostics.report('warning', 'The Voicemod app closed the connection twice while a button picture was asked for, so button pictures are switched off until the plugin is reloaded.', 'voicechanger-pictures');
    }
  }
  state.pictureQueue = [];
  if (state.pending) {
    state.tooBig[state.pending] = true;
    state.pending = null;
    state.queue = [];
    setStatus('a reply was too large, a list is off', 'Warning');
    host.diagnostics.report('warning', 'A reply of the Voicemod app was larger than the web socket message limit of this plugin (limits.wsMessageKB in plugin.json), so that list is switched off. "Refresh lists" tries again.', 'voicechanger-big');
    return;
  }
  setStatus('Voicemod is not running (start the app)', 'Warning');
}

// ---- messages ----

function pickName(msg) {
  const name = msg.actionType || msg.action || msg.actionID || msg.actionId || msg.event || msg.type;
  return typeof name === 'string' ? name : '';
}

// The docs write actionID and actionId, and some events use a different envelope; read what is there.
function pickObject(msg) {
  return msg.actionObject || msg.payload || {};
}

// ---- the voice and sound lists ----

const MAX_ITEMS = 5000;
const clip = (v, n) => String(v === undefined || v === null ? '' : v).slice(0, n);

// The id of a button picture is 1 to 128 characters of letters, digits and . _ : - ; an id outside that has no picture.
// Every character outside the allowed ones becomes _ and four hex digits, so two different names never give the same id.
function enc(text) {
  return String(text).replace(/[^A-Za-z0-9.:-]/g, (c) => '_' + ('0000' + c.charCodeAt(0).toString(16)).slice(-4));
}

// Image ids are stable per item (never based on the picture): voice:<voiceID>, sound:<soundId>. The item's bitmapChecksum is only the version of the
// picture: when a refreshed list shows another one, the cached picture is dropped so the host asks for it again when it is used.
function imageRef(imageId, kind, id, version) {
  if (!/^[A-Za-z0-9._:-]{1,128}$/.test(imageId)) return null;
  const old = state.prevSources && state.prevSources.get(imageId);
  if (old && version && old.version && old.version !== version && typeof host.images.invalidate === 'function') host.images.invalidate(imageId, versionOf(version));
  state.imageSources.set(imageId, { kind, id, version: version || '' });
  return imageId;
}

// Before a list is read again: its old entries are kept aside (to compare the versions) and the table starts empty.
// The host takes a version of at most 64 printable characters.
function versionOf(text) {
  return typeof text === 'string' && /^[ -~]{1,64}$/.test(text) ? text : undefined;
}

function beginRead(prefix) {
  state.prevSources = new Map();
  for (const [k, v] of [...state.imageSources]) if (k.startsWith(prefix)) { state.prevSources.set(k, v); state.imageSources.delete(k); }
}

function readVoices(obj) {
  beginRead('voice:');
  const out = [];
  for (const v of Array.isArray(obj.voices) ? obj.voices : []) {
    if (out.length >= MAX_ITEMS) break;
    const id = v && (v.id || v.voiceID || v.voiceId);
    if (typeof id === 'string' && id) out.push({ id, name: clip(v.friendlyName || v.name || id, 100), favorite: v.favorited === true, image: imageRef('voice:' + enc(id), 'voice', id, typeof v.bitmapChecksum === 'string' ? v.bitmapChecksum : '') });
  }
  return out;
}

// Every soundboard the app returns is listed: your own and disabled ones too (a disabled sound is marked in its label). The docs spell the keys
// both ways (id / Id, name / Name), so both are read. Only exact duplicates (same id) are dropped.
const pick = (o, ...keys) => { for (const k of keys) if (o && o[k] !== undefined && o[k] !== null) return o[k]; return undefined; };
// An id is a text or a number (an item may carry a numeric one); anything else has no id.
const idOf = (v) => (typeof v === 'string' && v ? v : typeof v === 'number' && Number.isFinite(v) ? String(v) : '');

// Each sound is classified on its own (its isCustom flag, not only the board's): a sound the person added from the community has isCustom = true,
// also inside a mixed board or a board of the app. Community = such sounds; My soundboards = all sounds of the person's own boards;
// Voicemod library = the other sounds of the app's boards. A sound can be in two sources, but only once in each.
// getMemes is the flattened list of the same sounds (every board sound's id is a meme FileName), so it is not read.
function readSounds(obj) {
  beginRead('sound:');
  const out = [];
  const seen = new Set();
  for (const board of Array.isArray(obj.soundboards) ? obj.soundboards : (Array.isArray(obj.Soundboards) ? obj.Soundboards : [])) {
    const boardName = clip(pick(board, 'name', 'Name') || 'Sounds', 60);
    const boardOff = pick(board, 'enabled', 'Enabled') === false;
    const boardCustom = pick(board, 'isCustom', 'IsCustom') === true;
    const inBoard = pick(board, 'sounds', 'Sounds', 'memes', 'Memes', 'items', 'Items');
    for (const sd of Array.isArray(inBoard) ? inBoard : []) {
      // A sound may carry a FileName / memeId instead of an Id; that is then its identifier and what playMeme takes.
      const fileKey = idOf(pick(sd, 'FileName', 'Filename', 'fileName', 'filename', 'memeId', 'MemeId'));
      const id = idOf(pick(sd, 'id', 'Id', 'ID')) || fileKey;
      if (!id) continue;
      const soundCustom = pick(sd, 'isCustom', 'IsCustom') === true;
      const sources = [];
      if (soundCustom) sources.push('community');
      if (boardCustom) sources.push('mine');
      if (!soundCustom && !boardCustom) sources.push('library');
      const off = boardOff || pick(sd, 'enabled', 'Enabled') === false;
      const rawName = pick(sd, 'name', 'Name');
      const name = clip(typeof rawName === 'string' && rawName.trim() ? rawName : id, 100);
      // The same name is not a duplicate (two sounds may share a name); the same id twice in one source is.
      for (const source of sources) {
        if (out.length >= MAX_ITEMS) return out;
        if (seen.has(source + '|' + id)) continue;
        seen.add(source + '|' + id);
        out.push({ id, name: off ? name + ' (disabled)' : name, board: boardName, source, file: fileKey || '', favorite: sd.favorited === true || sd.isFavorite === true, image: imageRef('sound:' + enc(id), 'sound', id, typeof sd.bitmapChecksum === 'string' ? sd.bitmapChecksum : '') });
      }
    }
  }
  return out;
}

const allSounds = () => state.lists.sounds;

function updateVoiceName() {
  const id = state.vars['voicechanger.voice'];
  const v = state.lists.voices && state.lists.voices.find((x) => x.id === id);
  setVar('voicechanger.voicename', v ? v.name : '');
}

function pushRecent(id) {
  state.recent = [id].concat(state.recent.filter((r) => r !== id)).slice(0, 10);
}

// One list at a time, so two big replies never arrive together.
// The sound lists are read lazily: only the voices at first, a sound list the first time a picker opens it (state.wanted).
// A reconnect or Refresh lists reads again what is wanted; the old list stays until the new one replaces it.
function queueLists() {
  state.queue = ['voices'].concat(['sounds'].filter((k) => state.wanted[k])).filter((k) => !state.tooBig[k]);
  loadNext();
}

function ensureList(kind) {
  if (state.wanted[kind]) return;
  state.wanted[kind] = true;
  if (state.registered && !state.tooBig[kind] && !state.queue.includes(kind) && state.pending !== kind) { state.queue.push(kind); loadNext(); }
}

// The sources of the sound pickers: library (the app's sounds), community (sounds the person added from the community) and mine (own soundboards).

function soundList(source) {
  const need = ['sounds'];
  need.forEach(ensureList);
  const all = state.lists.sounds;
  if (!all) return { list: null, need };
  if (source === 'library' || source === 'mine' || source === 'community') return { list: all.filter((i) => i.source === source), need };
  // No source (a button of version 0.2.0): all sources, each sound once.
  const ids = new Set();
  return { list: all.filter((i) => !ids.has(i.id) && ids.add(i.id)), need };
}

const LIST_ACTIONS = { voices: 'getVoices', sounds: 'getAllSoundboard' };

function loadNext() {
  if (state.pending || !state.registered) return;
  const kind = state.queue.shift();
  if (!kind) return;
  state.pending = kind;
  if (!send(LIST_ACTIONS[kind], {})) { state.pending = null; state.queue = []; }
}

function listDone(kind) {
  if (state.pending === kind) state.pending = null;
  loadNext();
}

const byName = (a, b) => (a.name.toLowerCase() < b.name.toLowerCase() ? -1 : a.name.toLowerCase() > b.name.toLowerCase() ? 1 : 0);

// The options of the editor's search box: favorites first, then (voices) the last ones used, then the rest A-Z; sounds are grouped by soundboard.
function searchOptions(kind, query, values) {
  const source = kind === 'sounds' && values && typeof values.source === 'string' ? values.source : '';
  const found0 = kind === 'sounds' ? soundList(source) : null;
  const list = kind === 'sounds' ? found0.list : state.lists[kind];
  if (!list) {
    if (kind === 'sounds' ? found0.need.every((k) => state.tooBig[k]) : state.tooBig[kind]) return { options: [], error: 'This list is too large for the message limit of the plugin. Raise limits.wsMessageKB in plugin.json.' };
    if (!currentKey()) return { options: [], error: 'Enter your Voicemod Control API key in the plugin settings.' };
    if (!state.registered) return { options: [], error: 'Voicemod is not running or not connected. Start the app and try again.' };
    return { options: [], error: 'The list is loading, try again in a moment.' };
  }
  if (list.length === 0) {
    if (kind === 'sounds' && source) return { options: [], error: 'There are no sounds in this source.' };
    if (state.registered && !state.pending && Date.now() - state.lastEmptyRefresh > EMPTY_REFRESH_MS) { state.lastEmptyRefresh = Date.now(); queueLists(); }
    return { options: [], error: 'Voicemod has no entries in this list yet. If you just added some, try again in a moment.' };
  }
  const text = String((query && query.text) || '').toLowerCase();
  const limit = Math.max(1, Math.min(50, Number(query && query.limit) || 50));
  const offset = Math.max(0, Number(query && query.offset) || 0);
  const selected = query && typeof query.selected === 'string' && query.selected ? query.selected : null;
  const section = (item) => (item.favorite ? 'Favorites' : kind === 'voices' ? (state.recent.includes(item.id) ? 'Recent' : 'All voices') : item.board);
  let items = list.slice();
  if (text) items = items.filter((i) => i.name.toLowerCase().includes(text) || i.id.toLowerCase().includes(text));
  const rank = (i) => (i.favorite ? 0 : kind === 'voices' && !text && state.recent.includes(i.id) ? 1 : 2);
  const boards = [];
  for (const i of items) if (i.board && !boards.includes(i.board)) boards.push(i.board);
  items.sort((a, b) => {
    const r = rank(a) - rank(b);
    if (r) return r;
    if (rank(a) === 1) return state.recent.indexOf(a.id) - state.recent.indexOf(b.id);
    if (kind === 'sounds' && !text && rank(a) === 2) { const g = boards.indexOf(a.board) - boards.indexOf(b.board); if (g) return g; }
    return byName(a, b);
  });
  const found = selected ? list.find((i) => i.id === selected) : null;
  let page = items.slice(offset, offset + limit);
  let more = offset + limit < items.length;
  if (found && offset === 0 && !text && !page.some((i) => i.id === found.id)) {
    page = [found].concat(page); // one more than the limit: the paging offsets of the next pages stay exact
  }
  return { options: page.map((i) => (i.image ? { value: i.id, label: i.name, group: section(i), icon: 'plugin-image:voicechanger/' + i.image } : { value: i.id, label: i.name, group: section(i) })), more, selectedMissing: selected ? !found : false };
}

// ---- button pictures ----

// The first bytes of a base64 text, decoded (enough to read the type and the size of a picture).
function headBytes(base64, count) {
  const table = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
  const out = [];
  let bits = 0;
  let acc = 0;
  for (let i = 0; i < base64.length && out.length < count; i++) {
    const v = table.indexOf(base64[i]);
    if (v < 0) break;
    acc = (acc << 6) | v;
    bits += 6;
    if (bits >= 8) { bits -= 8; out.push((acc >> bits) & 255); acc &= (1 << bits) - 1; }
  }
  return out;
}

// One base64 text (with or without a data: prefix) becomes a data URL the host takes: png, jpeg, gif or webp, at most 256 KB decoded and
// (png) 1024 x 1024 pixels; anything else is null.
function dataUrlOf(text) {
  if (typeof text !== 'string') return null;
  const base64 = text.startsWith('data:') ? text.slice(text.indexOf(',') + 1) : text;
  if (base64.length < 16) return null;
  const type = base64.startsWith('iVBOR') ? 'png' : base64.startsWith('/9j/') ? 'jpeg' : base64.startsWith('R0lG') ? 'gif' : base64.startsWith('UklGR') ? 'webp' : null;
  if (!type) return null; // svg and everything else is not a picture the host takes
  const padding = base64.endsWith('==') ? 2 : base64.endsWith('=') ? 1 : 0;
  if (Math.floor(base64.length * 3 / 4) - padding > MAX_PICTURE_BYTES) return null;
  if (type === 'png') {
    const h = headBytes(base64, 24);
    if (h.length === 24 && (h[16] * 16777216 + h[17] * 65536 + h[18] * 256 + h[19] > MAX_PICTURE_SIDE || h[20] * 16777216 + h[21] * 65536 + h[22] * 256 + h[23] > MAX_PICTURE_SIDE)) return null;
  }
  return 'data:image/' + type + ';base64,' + base64;
}

// The picture of a getBitmap reply: `transparent`, else `default` (never `selected`). The reply is read at actionObject.result, at
// actionObject itself and at actionObject["1"].result, because the docs show more than one layout. This is the one place to change.
function pictureFromReply(obj) {
  const o = obj || {};
  for (const where of [o.result, o, o['1'] && o['1'].result]) {
    if (!where || typeof where !== 'object') continue;
    for (const field of ['transparent', 'default', 'image']) {
      const url = dataUrlOf(where[field]);
      if (url) return url;
    }
  }
  return null;
}

function onBitmap(obj) {
  const flight = state.pictureInFlight;
  state.pictureInFlight = null;
  if (!flight) return;
  const url = pictureFromReply(obj);
  // The version is the item's bitmapChecksum when Voicemod gave one; a host without versions ignores the third argument.
  const source = state.imageSources.get(flight.imageId);
  if (url) host.images.put(flight.imageId, url, source ? versionOf(source.version) : undefined);
}

host.images.onRequest((ids) => {
  guard(() => {
    for (const imageId of Array.isArray(ids) ? ids : []) {
      if (!state.imageSources.has(imageId) || state.pictureQueue.includes(imageId)) continue;
      state.pictureQueue.push(imageId);
      if (state.pictureQueue.length > MAX_PICTURE_QUEUE) state.pictureQueue.shift();
    }
  });
});

// One getBitmap at a time, at most four a second.
function pump() {
  const now = Date.now();
  if (state.pictureInFlight && now - state.pictureInFlight.at > PICTURE_TIMEOUT_MS) state.pictureInFlight = null;
  if (state.picturesOff || !state.registered || state.pending || state.pictureInFlight || !state.pictureQueue.length) return;
  state.pictureSends = state.pictureSends.filter((t) => now - t < 1000);
  if (state.pictureSends.length >= PICTURES_PER_SECOND) return;
  const imageId = state.pictureQueue[0];
  const source = state.imageSources.get(imageId);
  if (!source) { state.pictureQueue.shift(); return; }
  if (!send('getBitmap', source.kind === 'voice' ? { voiceID: source.id } : { memeId: source.id })) return; // too many messages just now: next time
  state.pictureQueue.shift();
  state.pictureSends.push(now);
  state.pictureInFlight = { imageId, at: now };
}

host.options.source('voices', (query) => searchOptions('voices', query));
host.options.source('sounds', (query, values) => searchOptions('sounds', query, values));

function onMessage(text, port) {
  let msg;
  try { msg = JSON.parse(text); } catch (e) { return; }
  if (!msg || typeof msg !== 'object') return;
  const name = pickName(msg);
  const obj = pickObject(msg);

  if (!state.registered) {
    if (name.toLowerCase() === 'registerclient' || (msg.id !== undefined && String(msg.id) === state.registerId)) onRegister(msg, port);
    return;
  }

  const key = name.toLowerCase();
  if (key === 'voicechangedevent' || key === 'getcurrentvoice') {
    const id = obj.voiceID !== undefined ? obj.voiceID : obj.voiceId !== undefined ? obj.voiceId : obj.currentVoice;
    if (typeof id === 'string') { setVar('voicechanger.voice', id); pushRecent(id); updateVoiceName(); }
  } else if (key === 'getvoices') {
    if (typeof msg.currentVoice === 'string') setVar('voicechanger.voice', msg.currentVoice);
    state.lists.voices = readVoices(obj);
    setVar('voicechanger.voicecount', state.lists.voices.length);
    updateVoiceName();
    listDone('voices');
  } else if (key === 'getbitmap') {
    onBitmap(obj);
  } else if (key === 'getallsoundboard') {
    state.lists.sounds = readSounds(obj);
    setVar('voicechanger.soundcount', new Set(allSounds().map((x) => x.id)).size);
    listDone('sounds');
  } else if (key === 'voicechangerenabledevent') {
    setVar('voicechanger.enabled', true);
  } else if (key === 'voicechangerdisabledevent') {
    setVar('voicechanger.enabled', false);
  } else if (key === 'licensetypechanged') {
    if (typeof obj.licenseType === 'string') setVar('voicechanger.license', clip(obj.licenseType, 20).toLowerCase());
  } else if (typeof obj.value === 'boolean') {
    // toggle... events and get...Status answers; the sounds' "mute for me" switch is not the microphone.
    if (key.includes('voicechanger')) setVar('voicechanger.enabled', obj.value);
    else if (key.includes('mutemic')) setVar('voicechanger.micmuted', obj.value);
    else if (key.includes('hearmy')) setVar('voicechanger.hearmyself', obj.value);
    else if (key.includes('background')) setVar('voicechanger.background', obj.value);
    else if (key.includes('mutememe')) setVar('voicechanger.memesmuted', obj.value);
  }
}

function onRegister(msg, port) {
  const payload = pickObject(msg);
  const status = payload.status || msg.status || {};
  const code = Number(status.code);
  if (code === 200) {
    state.registered = true;
    state.attempts = 0;
    state.lastGoodPort = port;
    host.diagnostics.resolve('voicechanger-auth');
    host.diagnostics.resolve('voicechanger-connect');
    setStatus('connected', 'Ok');
    send('getCurrentVoice');
    send('getVoiceChangerStatus');
    send('getMuteMicStatus');
    send('getHearMyselfStatus');
    send('getBackgroundEffectStatus');
    send('getMuteMemeForMeStatus');
    queueLists();
    return;
  }
  if (code === 401) {
    state.rejectedKey = state.keyUsed;
    const message = 'Voicemod refused the API key (401). Paste the key from your Voicemod e-mail again, with no spaces. How to get a key: ' + TUTORIAL_URL;
    setStatus('API key refused (401), see Error List', 'Error');
    host.diagnostics.report('error', message, 'voicechanger-auth');
  } else {
    setStatus('Voicemod answered with an error' + (code ? ' (' + code + ')' : ''), 'Error');
  }
  state.socket.close();
}

// ---- connection loop ----

function tick() {
  const key = currentKey();
  if (!key) {
    if (state.socket) { state.socket.close(); }
    setVar('voicechanger.enabled', false);
    setStatus('enter your API key in the settings', 'Warning');
    return;
  }
  if (state.rejectedKey !== null && state.rejectedKey !== key) { state.rejectedKey = null; state.attempts = 0; state.nextAt = 0; }
  if (state.socket) {
    if (key !== state.keyUsed) state.socket.close(); // the key was changed: reconnect with the new one
    else if (state.open && !state.registered && Date.now() - state.openedAt > REGISTER_TIMEOUT_MS) state.socket.close();
    return;
  }
  if (state.rejectedKey !== null) return;
  if (Date.now() >= state.nextAt) connect(key);
}

// ---- actions ----

function run(action, payload, after) {
  if (!currentKey()) return { ok: false, code: 'NotConfigured', message: 'Enter your Voicemod Control API key in the plugin settings.' };
  if (!state.registered) return { ok: false, code: 'NotConnected', message: 'Not connected to the Voicemod app. Start Voicemod and check the plugin status.' };
  if (!send(action, payload)) return { ok: false, code: 'Unavailable', message: 'Too many commands at once. Try again in a moment.' };
  if (after) send(after);
  return { ok: true };
}

// An action that fails leaves its message in voicechanger.lasterror.
function registerAction(action) {
  const original = action.run;
  action.run = (context, settings) => {
    const result = original(context, settings);
    if (result && result.ok === false) setVar('voicechanger.lasterror', String(result.message || '').slice(0, 200));
    return result;
  };
  host.registerAction(action);
}

function toggle(type, name, description, action, after) {
  registerAction({ type: 'voicechanger.' + type, name, category: 'Voice Changer', description, fields: [], run: () => run(action, {}, after) });
}

toggle('togglevoicechanger', 'Toggle voice changer', 'Switches the voice changer on or off.', 'toggleVoiceChanger', 'getVoiceChangerStatus');
toggle('togglemute', 'Toggle microphone mute', 'Mutes or unmutes the microphone in the Voicemod app.', 'toggleMuteMic', 'getMuteMicStatus');
toggle('togglehearmyself', 'Toggle hear myself', 'Switches hearing your own voice on or off.', 'toggleHearMyVoice', 'getHearMyselfStatus');
toggle('togglebackground', 'Toggle background effect', 'Switches the background effect on or off.', 'toggleBackground', 'getBackgroundEffectStatus');
toggle('stopsounds', 'Stop all sounds', 'Stops every sound that is playing.', 'stopAllMemeSounds');

registerAction({
  type: 'voicechanger.loadvoice', name: 'Load voice', category: 'Voice Changer',
  description: 'Loads a voice. Search the voices of the app by name.',
  fields: [{ key: 'voiceID', label: 'Voice', kind: 'Select', searchable: true, optionsSource: 'voices', default: '' }],
  run(context, settings) {
    const voiceID = String(settings.voiceID || '').trim();
    if (!voiceID) return { ok: false, code: 'NotConfigured', message: 'Choose a voice.' };
    if (state.lists.voices && !state.lists.voices.some((v) => v.id === voiceID)) return { ok: false, code: 'NotFound', message: 'This voice is not in the Voicemod app any more. Choose another.' };
    const result = run('loadVoice', { voiceID }, 'getCurrentVoice');
    if (result.ok) pushRecent(voiceID);
    return result;
  },
});

registerAction({
  type: 'voicechanger.refreshvoices', name: 'Refresh lists', category: 'Voice Changer',
  description: 'Reads the voices and sounds of the Voicemod app again (they are read when the plugin connects).',
  fields: [],
  run() {
    if (!state.registered) return run('getVoices', {});
    state.tooBig = { voices: false, sounds: false };
    queueLists();
    return { ok: true };
  },
});

registerAction({
  type: 'voicechanger.randomvoice', name: 'Random voice', category: 'Voice Changer',
  description: 'Loads a random voice.',
  fields: [{
    key: 'mode', label: 'From', kind: 'Select', default: 'AllVoices',
    options: [
      { value: 'AllVoices', label: 'All voices' }, { value: 'FreeVoices', label: 'Free voices' },
      { value: 'FavoriteVoices', label: 'Favorite voices' }, { value: 'CustomVoices', label: 'Custom voices' },
    ],
  }],
  run(context, settings) { return run('selectRandomVoice', { mode: settings.mode || 'AllVoices' }, 'getCurrentVoice'); },
});

registerAction({
  type: 'voicechanger.playsound', name: 'Play sound', category: 'Voice Changer',
  description: 'Plays a sound of the Voicemod app. Search the sounds by name.',
  fields: [
    {
      key: 'source', label: 'Source', kind: 'Select', default: 'library',
      options: [{ value: 'library', label: 'Voicemod library' }, { value: 'community', label: 'Community' }, { value: 'mine', label: 'My soundboards' }],
    },
    { key: 'fileName', label: 'Sound', kind: 'Select', searchable: true, optionsSource: 'sounds', dependsOn: ['source'], default: '' },
  ],
  run(context, settings) {
    const value = String(settings.fileName || '').trim();
    if (!value) return { ok: false, code: 'NotConfigured', message: 'Choose a sound.' };
    // The lists are lazy: a list that is not loaded yet cannot say the sound is gone, so the value is played as it is.
    const all = allSounds();
    const played = all && all.find((x) => x.id === value);
    // A value saved by a development build as meme:<FileName> is played as that FileName.
    if (all && !played && !value.startsWith('meme:')) return { ok: false, code: 'NotFound', message: 'This sound is not in the Voicemod app any more. Choose another.' };
    // playMeme takes the FileName of a meme; a soundboard sound is sent by its id (the docs name no other play action).
    const FileName = played && played.file ? played.file : value.startsWith('meme:') ? value.slice(5) : value;
    const result = run('playMeme', { FileName, IsKeyDown: true });
    if (result.ok && played) setVar('voicechanger.lastsound', played.name);
    return result;
  },
});

// The script's start-up has only 2 seconds; the first connection waits a moment.
setVar('voicechanger.voicecount', 0);
setVar('voicechanger.soundcount', 0);
setVar('voicechanger.voicename', '');
setVar('voicechanger.lastsound', '');
setVar('voicechanger.lasterror', '');
setVar('voicechanger.license', '');
host.after(200, tick);
host.every(TICK_MS, tick);
host.every(PICTURE_PUMP_MS, () => guard(pump));
