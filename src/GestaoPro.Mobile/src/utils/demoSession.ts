// Entrada na demonstração pelo papel (POST /api/demo/entrar), sem senha.

const RETRY_DELAY_MS = 1500;
const AUTO_RETRY_WINDOW_MS = 60_000;

// Demonstração sendo preparada (503, servidor acordando ou restaurando os dados): tenta mais uma vez.
export async function enterDemoWithRetry<T>(
  enter: (role: string) => Promise<T>,
  role: string,
  wait: (ms: number) => Promise<void> = ms => new Promise(resolve => setTimeout(resolve, ms)),
): Promise<T> {
  try {
    return await enter(role);
  } catch (error) {
    if ((error as { status?: number })?.status !== 503) throw error;
    await wait(RETRY_DELAY_MS);
    return enter(role);
  }
}

// Estado em memória: papel da sessão demo atual, se ela caiu com 401 e quando foi a última nova entrada automática.
let demoRole: string | null = null;
let expired = false;
let lastAutoRetry = Number.NEGATIVE_INFINITY;

export function rememberDemoRole(role: string) {
  demoRole = role;
  expired = false;
}

export function forgetDemoRole() {
  demoRole = null;
  expired = false;
  lastAutoRetry = Number.NEGATIVE_INFINITY;
}

export function markSessionExpired() {
  expired = true;
}

// Papel para entrar de novo sozinho (sessão demo caiu), no máximo uma vez por minuto; senão null.
export function takeAutoRetryRole(now = Date.now()): string | null {
  if (!expired || !demoRole || now - lastAutoRetry < AUTO_RETRY_WINDOW_MS) return null;
  expired = false;
  lastAutoRetry = now;
  return demoRole;
}
