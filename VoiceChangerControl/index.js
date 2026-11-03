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

host.variables.describe([
  { name: 'voicechanger.enabled', description: 'Whether the voice changer is on', example: 'true', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.micmuted', description: 'Whether the microphone is muted in the app', example: 'false', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.voice', description: 'The id of the current voice', example: 'nofx', category: 'Voice Changer' },
]);

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
  tooBig: { voices: false, sounds: false },
  pending: null, // the list whose reply is awaited
  queue: [],
  recent: [], // the last voices used, newest first
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

function readVoices(obj) {
  const out = [];
  for (const v of Array.isArray(obj.voices) ? obj.voices : []) {
    if (out.length >= MAX_ITEMS) break;
    const id = v && (v.id || v.voiceID || v.voiceId);
    if (typeof id === 'string' && id) out.push({ id, name: clip(v.friendlyName || v.name || id, 100), favorite: v.favorited === true });
  }
  return out;
}

function readSounds(obj) {
  const out = [];
  for (const board of Array.isArray(obj.soundboards) ? obj.soundboards : []) {
    for (const sd of board && Array.isArray(board.sounds) ? board.sounds : []) {
      if (out.length >= MAX_ITEMS) return out;
      if (sd && typeof sd.id === 'string' && sd.id) out.push({ id: sd.id, name: clip(sd.name || sd.id, 100), board: clip(board.name || 'Sounds', 60), favorite: sd.favorited === true || sd.isFavorite === true });
    }
  }
  return out;
}

function pushRecent(id) {
  state.recent = [id].concat(state.recent.filter((r) => r !== id)).slice(0, 10);
}

// One list at a time, so two big replies never arrive together.
function queueLists() {
  state.queue = ['voices', 'sounds'].filter((k) => !state.tooBig[k]);
  loadNext();
}

function loadNext() {
  if (state.pending || !state.registered) return;
  const kind = state.queue.shift();
  if (!kind) return;
  state.pending = kind;
  if (!send(kind === 'voices' ? 'getVoices' : 'getAllSoundboard', {})) { state.pending = null; state.queue = []; }
}

function listDone(kind) {
  if (state.pending === kind) state.pending = null;
  loadNext();
}

const byName = (a, b) => (a.name.toLowerCase() < b.name.toLowerCase() ? -1 : a.name.toLowerCase() > b.name.toLowerCase() ? 1 : 0);

// The options of the editor's search box: favorites first, then (voices) the last ones used, then the rest A-Z; sounds are grouped by soundboard.
function searchOptions(kind, query) {
  const list = state.lists[kind];
  if (!list) {
    if (state.tooBig[kind]) return { options: [], error: 'This list is too large for the message limit of the plugin. Raise limits.wsMessageKB in plugin.json.' };
    if (!currentKey()) return { options: [], error: 'Enter your Voicemod Control API key in the plugin settings.' };
    if (!state.registered) return { options: [], error: 'Voicemod is not running or not connected. Start the app and try again.' };
    return { options: [], error: 'The list is loading, try again in a moment.' };
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
    page = [found].concat(page.slice(0, limit - 1));
    more = limit - 1 < items.length;
  }
  return { options: page.map((i) => ({ value: i.id, label: i.name, group: section(i) })), more, selectedMissing: selected ? !found : false };
}

host.options.source('voices', (query) => searchOptions('voices', query));
host.options.source('sounds', (query) => searchOptions('sounds', query));

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
    if (typeof id === 'string') { setVar('voicechanger.voice', id); pushRecent(id); }
  } else if (key === 'getvoices') {
    if (typeof msg.currentVoice === 'string') setVar('voicechanger.voice', msg.currentVoice);
    state.lists.voices = readVoices(obj);
    listDone('voices');
  } else if (key === 'getallsoundboard') {
    state.lists.sounds = readSounds(obj);
    listDone('sounds');
  } else if (typeof obj.value === 'boolean') {
    // toggle... events and get...Status answers; the meme sound "mute for me" switch is not the microphone.
    if (key.includes('voicechanger')) setVar('voicechanger.enabled', obj.value);
    else if (key.includes('mutemic')) setVar('voicechanger.micmuted', obj.value);
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

function toggle(type, name, description, action, after) {
  host.registerAction({ type: 'voicechanger.' + type, name, category: 'Voice Changer', description, fields: [], run: () => run(action, {}, after) });
}

toggle('togglevoicechanger', 'Toggle voice changer', 'Switches the voice changer on or off.', 'toggleVoiceChanger', 'getVoiceChangerStatus');
toggle('togglemute', 'Toggle microphone mute', 'Mutes or unmutes the microphone in the Voicemod app.', 'toggleMuteMic', 'getMuteMicStatus');
toggle('togglehearmyself', 'Toggle hear myself', 'Switches hearing your own voice on or off.', 'toggleHearMyVoice');
toggle('togglebackground', 'Toggle background effect', 'Switches the background effect on or off.', 'toggleBackground');
toggle('stopsounds', 'Stop all sounds', 'Stops every sound that is playing.', 'stopAllMemeSounds');

host.registerAction({
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

host.registerAction({
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

host.registerAction({
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

host.registerAction({
  type: 'voicechanger.playsound', name: 'Play sound', category: 'Voice Changer',
  description: 'Plays a sound of the Voicemod app. Search the sounds by name.',
  fields: [{ key: 'fileName', label: 'Sound', kind: 'Select', searchable: true, optionsSource: 'sounds', default: '' }],
  run(context, settings) {
    const FileName = String(settings.fileName || '').trim();
    if (!FileName) return { ok: false, code: 'NotConfigured', message: 'Choose a sound.' };
    if (state.lists.sounds && !state.lists.sounds.some((v) => v.id === FileName)) return { ok: false, code: 'NotFound', message: 'This sound is not in the Voicemod app any more. Choose another.' };
    return run('playMeme', { FileName, IsKeyDown: true });
  },
});

// The script's start-up has only 2 seconds; the first connection waits a moment.
host.after(200, tick);
host.every(TICK_MS, tick);
