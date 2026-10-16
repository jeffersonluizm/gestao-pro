# Teste de estresse do GestaoPro: hipóteses (escritas antes de medir)

Data: 2026-10-04. Este arquivo foi versionado antes da primeira medição. Os resultados ficam em
`2026-10-04-resultados.md`.

## Ambiente

- Máquina única (Ryzen 5 5600, 6 núcleos e 12 threads, 16 GB). Gerador de carga (k6 2.2.0), API e banco dividem a máquina: os números servem para comparar **antes e depois**, não como capacidade de produção.
- Banco descartável: `mongod` 8.2.6 local na porta 27100, base `GestaoProStress`, pasta de dados temporária apagada no fim. Nunca o banco real.
- API publicada em pasta temporária (`dotnet publish -c Release`) **sem** `appsettings.Local.json`, com `ASPNETCORE_ENVIRONMENT=Development` (cria a conta de demonstração) e log em `Warning`.
- Portão: antes de qualquer requisição de carga, `GET /api/health/mongodb` precisa responder `database: "GestaoProStress"` (`tests/carga/portao.mjs`).
- "Antes" = build da `main`. "Depois" = build da branch `feat/portfolio-ui-ux`.
- Massa: 100 avisos no condomínio de demonstração, 50 condomínios com 100 usuários cada; em H3, mais 20.000 avisos de outro condomínio.
- Protocolo: 10 s de aquecimento, degraus de 10, 50, 100 e 200 usuários virtuais por 30 s, registrando req/s, p50, p95, p99 e erros.

## Hipóteses

| # | Cenário | Hipótese (mensurável) |
|---|---|---|
| H1 | `POST /api/Auth/login` (BCrypt custo 11), limite de autenticação aberto | O login fica preso à CPU: entre **60 e 120 req/s** no platô, p95 < 400 ms com 10 VUs, latência crescendo em fila acima disso, **0% de 5xx**. |
| H1b | Login com e-mail inexistente x senha errada (1 VU, 200 pares) | **Antes:** a mediana do e-mail inexistente é **mais de 50 ms menor** (sem BCrypt), o que permite enumerar contas. **Depois:** diferença **menor que 10 ms**. |
| H2 | `GET /api/announcements` com 100 avisos | **≥ 1.000 req/s** com 50 VUs, p95 < 50 ms, 0% de erro. |
| H3 | H2 com +20.000 avisos de outro condomínio | **Antes:** o throughput cai **5x ou mais** e p95 > 200 ms (a API lê a coleção inteira e filtra em memória). **Depois** (filtro no MongoDB com índice e paginação): volta a menos de 20% de H2. |
| H4 | Lista de condomínios para o cadastro (anônima), 50 condomínios x 100 usuários | **Antes** (`GET /api/Condominiums`, N+1): p95 > 1 s com 20 VUs. **Depois** (`GET /api/Condominiums/public`, uma consulta com projeção): p95 < 50 ms. |
| H5 | `POST /api/announcements` | ≥ 300 req/s com 50 VUs, p95 < 100 ms, 0% de erro; o depois (sem a consulta de administrador a cada escrita) ao menos 15% mais rápido. |
| H6 | 50 `POST /api/reservations` simultâneos, mesmo horário e área | **Antes:** mais de 1 reserva criada (corrida). **Depois:** exatamente 1 criada, 49 recusadas com 4xx, nenhum 5xx. |
| H7 | 50 `POST /api/vote` simultâneos, mesmo usuário e pauta | Antes e depois: exatamente 1 voto gravado (índice único), o resto 4xx, nenhum 5xx. |
| H8 | `GET /api/health/mongodb` com 50 VUs | **Antes** (cliente novo por chamada): p95 > 100 ms e erros ou conexões crescendo. **Depois** (cliente da aplicação): p95 < 20 ms, 0% de erro. |
| H9 | Soak de 10 min a ~50% da capacidade de H2 (depois) | Memória do processo estável (crescimento < 20% entre o minuto 2 e o 10) e 0% de erro. |
