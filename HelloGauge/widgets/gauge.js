// The gauge widget. It runs in a worker with no network access and draws to a canvas; the only way to the outside is the `macroGrid` object.
const info = await macroGrid.ready;
const { canvas, settings, bindings } = info;
const ctx = canvas.getContext('2d');

// The value the person bound, or the plugin's own demo value.
const source = bindings.source || 'hellogauge.value';
let target = 0;
let shown = 0;
let taps = 0;
let cfg = settings;

macroGrid.subscribe([source]);
macroGrid.onVariables((values) => { target = Number(values[source]) || 0; macroGrid.frame(draw); });
macroGrid.onSettings((change) => { cfg = change.settings; macroGrid.frame(draw); });
macroGrid.onPointer((p) => { if (p.phase === 'down') { taps++; macroGrid.frame(draw); } });
macroGrid.onResize(({ width, height, dpr }) => { size(width, height, dpr); macroGrid.frame(draw); });
size(info.width, info.height, info.dpr);

function size(width, height, dpr) {
  canvas.width = Math.max(1, Math.round(width * dpr));
  canvas.height = Math.max(1, Math.round(height * dpr));
}

function draw() {
  shown += (target - shown) * 0.2; // ease toward the value
  const w = canvas.width, h = canvas.height, r = Math.min(w, h) * 0.38;
  const max = Number(cfg.max) || 100;
  ctx.clearRect(0, 0, w, h);
  ctx.lineWidth = r * 0.2;
  ctx.lineCap = 'round';
  ctx.strokeStyle = '#333';
  ctx.beginPath(); ctx.arc(w / 2, h / 2, r, 0.75 * Math.PI, 2.25 * Math.PI); ctx.stroke();
  const end = 0.75 * Math.PI + 1.5 * Math.PI * Math.min(1, Math.max(0, shown / max));
  ctx.strokeStyle = cfg.color || '#38bdf8';
  ctx.beginPath(); ctx.arc(w / 2, h / 2, r, 0.75 * Math.PI, end); ctx.stroke();
  ctx.fillStyle = '#fff';
  ctx.textAlign = 'center';
  ctx.font = (r * 0.5) + 'px sans-serif';
  ctx.fillText(Math.round(shown) + (cfg.unit || ''), w / 2, h / 2 + r * 0.2);
  if (taps > 0) { ctx.font = (r * 0.22) + 'px sans-serif'; ctx.fillText('taps ' + taps, w / 2, h - r * 0.15); }
  // Draw only while the needle is still moving: a widget that has nothing to animate costs nothing.
  if (Math.abs(target - shown) > 0.1) macroGrid.frame(draw);
}

macroGrid.frame(draw);
