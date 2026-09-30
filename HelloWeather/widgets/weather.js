// The weather widget. It draws the plugin's weather variables; it has no network access. It draws only when a value or a setting changes.
const info = await macroGrid.ready;
const { canvas } = info;
const ctx = canvas.getContext('2d');

const names = ['helloweather.temperature', 'helloweather.code', 'helloweather.wind'];
let temperature = null;
let code = null;
let wind = null;
let unit = info.settings.unit === 'F' ? 'F' : 'C';

macroGrid.subscribe(names);
macroGrid.onVariables((values) => {
  if ('helloweather.temperature' in values) temperature = Number(values['helloweather.temperature']);
  if ('helloweather.code' in values) code = Number(values['helloweather.code']);
  if ('helloweather.wind' in values) wind = Number(values['helloweather.wind']);
  macroGrid.frame(draw);
});
macroGrid.onSettings((change) => { unit = change.settings.unit === 'F' ? 'F' : 'C'; macroGrid.frame(draw); });
macroGrid.onResize(({ width, height, dpr }) => { size(width, height, dpr); macroGrid.frame(draw); });
size(info.width, info.height, info.dpr);
macroGrid.frame(draw);

function size(width, height, dpr) {
  canvas.width = Math.max(1, Math.round(width * dpr));
  canvas.height = Math.max(1, Math.round(height * dpr));
}

function draw() {
  const w = canvas.width, h = canvas.height, u = Math.min(w, h);
  const light = info.theme === 'light';
  ctx.clearRect(0, 0, w, h);
  ctx.fillStyle = light ? '#111' : '#fff';
  ctx.textAlign = 'center';
  if (temperature === null) {
    ctx.font = (u * 0.12) + 'px sans-serif';
    ctx.fillText('Waiting for data', w / 2, h / 2);
    return;
  }
  picture(w / 2, h * 0.34, u * 0.32);
  const shown = unit === 'F' ? temperature * 9 / 5 + 32 : temperature;
  ctx.fillStyle = light ? '#111' : '#fff';
  ctx.font = 'bold ' + (u * 0.22) + 'px sans-serif';
  ctx.fillText(Math.round(shown) + '°' + unit, w / 2, h * 0.76);
  if (wind !== null) {
    ctx.fillStyle = light ? '#555' : '#9aa0a6';
    ctx.font = (u * 0.09) + 'px sans-serif';
    ctx.fillText(Math.round(wind) + ' km/h', w / 2, h * 0.9);
  }
}

// A simple picture for the weather code: sun, cloud, fog, rain, snow or storm.
function picture(x, y, r) {
  if (code === 0 || code === 1) {
    ctx.fillStyle = '#facc15';
    ctx.beginPath(); ctx.arc(x, y, r * 0.55, 0, Math.PI * 2); ctx.fill();
  }
  if (code >= 2) {
    ctx.fillStyle = code >= 95 ? '#64748b' : '#cbd5e1';
    ctx.beginPath();
    ctx.arc(x - r * 0.35, y + r * 0.1, r * 0.4, 0, Math.PI * 2);
    ctx.arc(x + r * 0.1, y - r * 0.15, r * 0.5, 0, Math.PI * 2);
    ctx.arc(x + r * 0.5, y + r * 0.15, r * 0.35, 0, Math.PI * 2);
    ctx.fill();
  }
  ctx.strokeStyle = '#38bdf8';
  ctx.lineWidth = r * 0.08;
  ctx.lineCap = 'round';
  if ((code >= 51 && code <= 67) || (code >= 80 && code <= 82)) {
    for (let i = -1; i <= 1; i++) { ctx.beginPath(); ctx.moveTo(x + i * r * 0.4, y + r * 0.55); ctx.lineTo(x + i * r * 0.3, y + r * 0.85); ctx.stroke(); }
  } else if ((code >= 71 && code <= 77) || code === 85 || code === 86) {
    ctx.fillStyle = '#e2e8f0';
    for (let i = -1; i <= 1; i++) { ctx.beginPath(); ctx.arc(x + i * r * 0.4, y + r * 0.7, r * 0.07, 0, Math.PI * 2); ctx.fill(); }
  } else if (code >= 95) {
    ctx.strokeStyle = '#facc15';
    ctx.beginPath(); ctx.moveTo(x + r * 0.1, y + r * 0.5); ctx.lineTo(x - r * 0.1, y + r * 0.75); ctx.lineTo(x + r * 0.1, y + r * 0.75); ctx.lineTo(x - r * 0.05, y + r); ctx.stroke();
  } else if (code === 45 || code === 48) {
    ctx.strokeStyle = '#94a3b8';
    for (let i = 0; i < 2; i++) { ctx.beginPath(); ctx.moveTo(x - r * 0.6, y + r * (0.6 + i * 0.25)); ctx.lineTo(x + r * 0.6, y + r * (0.6 + i * 0.25)); ctx.stroke(); }
  }
}
