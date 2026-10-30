// A small example of a plugin with a custom widget. The plugin itself only publishes a demo value that sweeps between 0 and 100 (hellogauge.value),
// so the gauge has something to show until a person binds it to another variable in the widget's settings. The drawing is in widgets/gauge.js.

host.variables.describe([
  { name: 'hellogauge.value', description: 'A demo value that sweeps between 0 and 100', example: '42', category: 'Hello' },
]);

let phase = 0;
host.every(500, () => {
  phase += 0.1;
  host.variables.set('hellogauge.value', Math.round(50 + 50 * Math.sin(phase)));
});
