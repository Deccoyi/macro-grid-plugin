// A small example of a plugin whose widget shows data from the internet. Only the plugin talks to the network: it asks a weather service now and then
// and publishes the answer as variables. The widget (widgets/weather.js) has no network access and just draws those variables.

host.variables.describe([
  { name: 'helloweather.temperature', description: 'Temperature in degrees Celsius', example: '18.4', category: 'Weather' },
  { name: 'helloweather.code', description: 'Weather code: 0 clear, 1-3 clouds, 45-48 fog, 51-67 rain, 71-77 snow, 80-99 showers and thunder', example: '3', category: 'Weather' },
  { name: 'helloweather.wind', description: 'Wind speed in km/h', example: '12', category: 'Weather' },
]);

host.settings.page([
  { key: 'latitude', label: 'Latitude', kind: 'Number', default: 52.52, min: -90, max: 90 },
  { key: 'longitude', label: 'Longitude', kind: 'Number', default: 13.41, min: -180, max: 180 },
]);

function refresh() {
  const settings = host.settings.get();
  const latitude = Number(settings.latitude ?? 52.52);
  const longitude = Number(settings.longitude ?? 13.41);
  const url = 'https://api.open-meteo.com/v1/forecast?latitude=' + latitude + '&longitude=' + longitude + '&current=temperature_2m,weather_code,wind_speed_10m';
  const response = host.http.get(url);
  if (response.status !== 200) {
    host.status('weather', 'Weather: the service answered ' + response.status, 'Warning');
    return;
  }
  const current = JSON.parse(response.body).current;
  host.variables.set('helloweather.temperature', current.temperature_2m);
  host.variables.set('helloweather.code', current.weather_code);
  host.variables.set('helloweather.wind', current.wind_speed_10m);
  host.status('weather', 'Weather: ' + current.temperature_2m + ' °C', 'Ok');
}

// The first call waits a moment: the script's start-up has only 2 seconds, and a request can take longer.
host.after(200, refresh);
host.every(15 * 60 * 1000, refresh);
