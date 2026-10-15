// Configuração dinâmica sobre o app.json: confere a URL da API no export de produção e só libera tráfego
// sem TLS (usesCleartextTraffic) em desenvolvimento. Regras em scripts/apiUrl.js.
const { buildSettings } = require('./scripts/apiUrl');

module.exports = ({ config }) => {
  const { cleartext, problem } = buildSettings(process.env);
  if (problem) throw new Error(`[gestaopro] ${problem}`);

  return {
    ...config,
    android: { ...config.android, usesCleartextTraffic: cleartext },
    plugins: (config.plugins || []).map(plugin =>
      Array.isArray(plugin) && plugin[0] === 'expo-build-properties'
        ? [plugin[0], { ...plugin[1], android: { ...(plugin[1] && plugin[1].android), usesCleartextTraffic: cleartext } }]
        : plugin),
  };
};
