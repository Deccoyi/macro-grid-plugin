// A small example of a JavaScript plugin. It publishes a counter variable that any widget can show
// ("Count: {hellojs.count}") and adds one action that bumps it. Everything a plugin can do goes through
// the global `host` object; there is no other access to the server.

let count = 0;

host.variables.describe([
  { name: 'hellojs.count', description: 'How many times the counter was bumped', example: '3', category: 'Hello' },
]);
host.variables.set('hellojs.count', count);

host.settings.page([
  { key: 'step', label: 'Step', kind: 'Number', default: 1, min: 1, max: 100 },
]);

host.registerAction({
  type: 'hellojs.bump',
  name: 'Bump the counter',
  category: 'Hello',
  description: 'Adds the configured step to the hellojs.count variable.',
  icon: 'plus',
  fields: [
    { key: 'times', label: 'Times', kind: 'Number', default: 1, min: 1, max: 10 },
  ],
  run(context, settings) {
    const step = host.settings.get().step || 1;
    count += step * (settings.times || 1);
    host.variables.set('hellojs.count', count);
  },
});

// Timers need no permission. The shortest allowed interval is 100 ms.
host.every(5000, () => {
  host.status('counter', 'Count ' + count, 'Ok');
});
