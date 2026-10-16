import { createRequire } from 'node:module';
import { describe, expect, it } from 'vitest';

// O app.config.js é CommonJS (lido pelo Expo); a regra fica em scripts/apiUrl.js.
const { apiUrlProblem, buildSettings } = createRequire(import.meta.url)('./apiUrl.js') as {
  apiUrlProblem: (value?: string) => string | null;
  buildSettings: (env: Record<string, string | undefined>) => { cleartext: boolean; problem: string | null };
};

// Export de produção (Expo web na Vercel ou build nativo) sem a URL da API caía em http://localhost:5088.
describe('apiUrlProblem', () => {
  it('sem EXPO_PUBLIC_API_URL explica o que falta', () => {
    expect(apiUrlProblem(undefined)).toMatch(/EXPO_PUBLIC_API_URL/);
  });

  it('exige https, salvo localhost', () => {
    expect(apiUrlProblem('http://gestaopro-api.onrender.com')).toMatch(/https/);
    expect(apiUrlProblem('https://gestaopro-api.onrender.com')).toBeNull();
    expect(apiUrlProblem('http://localhost:5088')).toBeNull();
  });
});

// Tráfego sem TLS (usesCleartextTraffic) só em desenvolvimento, para falar com a API no IP do PC.
describe('buildSettings', () => {
  it('desenvolvimento libera http e não exige a URL', () => {
    expect(buildSettings({ NODE_ENV: 'development' })).toEqual({ cleartext: true, problem: null });
  });

  it('produção sem URL https falha e não libera http', () => {
    expect(buildSettings({ NODE_ENV: 'production' }).problem).toMatch(/EXPO_PUBLIC_API_URL/);
    expect(buildSettings({ NODE_ENV: 'production', EXPO_PUBLIC_API_URL: 'https://api.exemplo.test' }))
      .toEqual({ cleartext: false, problem: null });
  });

  it('build de teste na rede local pode liberar http de propósito', () => {
    expect(buildSettings({ NODE_ENV: 'production', EXPO_PUBLIC_API_URL: 'http://192.168.0.10:5088', GESTAOPRO_PERMITIR_HTTP: '1' }))
      .toEqual({ cleartext: true, problem: null });
  });
});
