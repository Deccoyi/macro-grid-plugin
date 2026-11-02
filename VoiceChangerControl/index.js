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
const REGISTER_TIMEOUT_MS = 5000;
const TICK_MS = 1000;

host.variables.describe([
  { name: 'voicechanger.enabled', description: 'Whether the voice changer is on', example: 'true', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.micmuted', description: 'Whether the microphone is muted in the app', example: 'false', type: 'boolean', category: 'Voice Changer' },
  { name: 'voicechanger.voice', description: 'The id of the current voice', example: 'nofx', category: 'Voice Changer' },
  { name: 'voicechanger.voices', description: 'The voices of the app as "id=name" pairs (use an id with "Load voice")', example: 'nofx=Clean, baby=Baby', category: 'Voice Changer' },
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
};

function setStatus(text, level) { host.status('voicechanger', 'Voice Changer: ' + text, level); }

function currentKey() { return String(host.settings.get().apiKey || '').trim(); }

function send(action, payload) {
  if (!state.registered) return false;
  state.socket.send(JSON.stringify({ id: String(state.nextId++), action, payload: payload || {} }));
  return true;
}

function portForAttempt(n) {
  if (n === 0 && state.lastGoodPort) return state.lastGoodPort;
  // The first port stays in the rotation every second attempt, because the app uses it in nearly every case.
  return n % 2 === 0 ? PORTS[0] : PORTS[1 + ((n - 1) >> 1) % (PORTS.length - 1)];
}

function connect(key) {
  const port = portForAttempt(state.attempts);
  state.keyUsed = key;
  state.open = false;
  state.registered = false;
  let socket;
  try {
    socket = host.ws.connect('ws://localhost:' + port + '/v1/', {
      onOpen() { onOpen(socket, port, key); },
      onMessage(text) { if (state.socket === socket) onMessage(text, port); },
      onClose() { onClose(socket); },
    });
  } catch (e) {
    state.attempts++;
    state.nextAt = Date.now() + RETRY_SLOW_MS;
    setStatus('cannot open a connection (' + (e && e.message ? e.message : 'refused') + ')', 'Error');
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
  socket.send(JSON.stringify({ id: state.registerId, action: 'registerClient', payload: { clientKey: key } }));
}

function onClose(socket) {
  if (state.socket !== socket) return;
  const wasRegistered = state.registered;
  state.socket = null;
  state.open = false;
  state.registered = false;
  if (state.rejectedKey !== null && state.rejectedKey === state.keyUsed) return; // the 401 message is already shown
  state.attempts = wasRegistered ? 0 : state.attempts + 1;
  state.nextAt = Date.now() + (wasRegistered ? 2000 : state.attempts > SLOW_AFTER ? RETRY_SLOW_MS : RETRY_MS);
  setStatus('Voicemod is not running or its Control API is off. Start the Voicemod app.', 'Warning');
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
    if (typeof id === 'string') host.variables.set('voicechanger.voice', id);
  } else if (key === 'getvoices') {
    const list = Array.isArray(obj.voices) ? obj.voices : [];
    const pairs = list.map((v) => (v.id || v.voiceID || v.voiceId) + '=' + (v.friendlyName || v.name || '')).filter((p) => p.length > 1);
    if (pairs.length) host.variables.set('voicechanger.voices', pairs.join(', '));
  } else if (typeof obj.value === 'boolean') {
    // toggle... events and get...Status answers; the meme sound "mute for me" switch is not the microphone.
    if (key.includes('voicechanger')) host.variables.set('voicechanger.enabled', obj.value);
    else if (key.includes('mutemic')) host.variables.set('voicechanger.micmuted', obj.value);
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
    setStatus('connected', 'Ok');
    send('getVoices');
    send('getCurrentVoice');
    send('getVoiceChangerStatus');
    send('getMuteMicStatus');
    return;
  }
  if (code === 401) {
    state.rejectedKey = state.keyUsed;
    const message = 'Voicemod refused the API key (401). Paste the key from your Voicemod e-mail again, with no spaces. How to get a key: ' + TUTORIAL_URL;
    setStatus('the API key was refused (401)', 'Error');
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
    host.variables.set('voicechanger.enabled', false);
    setStatus('enter your Voicemod Control API key in the plugin settings', 'Warning');
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
  if (!currentKey()) return { ok: false, code: 'Unavailable', message: 'Enter your Voicemod Control API key in the plugin settings.' };
  if (!send(action, payload)) return { ok: false, code: 'Unavailable', message: 'Not connected to the Voicemod app. Start Voicemod and check the plugin status.' };
  if (after) send(after);
  return { ok: true };
}

function toggle(type, name, action, after) {
  host.registerAction({ type: 'voicechanger.' + type, name, category: 'Voice Changer', description: 'Sends ' + action + ' to the Voicemod app.', fields: [], run: () => run(action, {}, after) });
}

toggle('togglevoicechanger', 'Toggle voice changer', 'toggleVoiceChanger', 'getVoiceChangerStatus');
toggle('togglemute', 'Toggle microphone mute', 'toggleMuteMic', 'getMuteMicStatus');
toggle('togglehearmyself', 'Toggle hear myself', 'toggleHearMyVoice');
toggle('togglebackground', 'Toggle background effect', 'toggleBackground');
toggle('stopsounds', 'Stop all sounds', 'stopAllMemeSounds');

host.registerAction({
  type: 'voicechanger.loadvoice', name: 'Load voice', category: 'Voice Changer',
  description: 'Loads a voice by its id. The ids are in the variable voicechanger.voices.',
  fields: [{ key: 'voiceID', label: 'Voice id', kind: 'Text', default: '' }],
  run(context, settings) {
    const voiceID = String(settings.voiceID || '').trim();
    if (!voiceID) return { ok: false, code: 'ProviderError', message: 'Enter a voice id.' };
    return run('loadVoice', { voiceID }, 'getCurrentVoice');
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
  description: 'Plays a Voicemod meme sound by its file name.',
  fields: [{ key: 'fileName', label: 'Sound file name', kind: 'Text', default: '' }],
  run(context, settings) {
    const FileName = String(settings.fileName || '').trim();
    if (!FileName) return { ok: false, code: 'ProviderError', message: 'Enter the sound file name.' };
    return run('playMeme', { FileName, IsKeyDown: true });
  },
});

// The script's start-up has only 2 seconds; the first connection waits a moment.
host.after(200, tick);
host.every(TICK_MS, tick);
