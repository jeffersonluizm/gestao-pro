| Cenário | Antes: req/s | Antes: p95 (ms) | Antes: erro | Depois: req/s | Depois: p95 (ms) | Depois: erro |
|---|---:|---:|---:|---:|---:|---:|
| H1 login, 10 VUs | 65,8 | 169,4 | 0,00% | 70,7 | 153,1 | 0,00% |
| H1 login, 50 VUs | 75,4 | 1.053,0 | 0,00% | 78,0 | 991,0 | 0,00% |
| H2 avisos, 10 VUs | 4.420,9 | 4,9 | 0,00% | 3.019,7 | 6,7 | 0,00% |
| H2 avisos, 50 VUs | 3.880,3 | 36,9 | 0,00% | 3.036,4 | 44,4 | 0,00% |
| H2 avisos, 100 VUs | 3.722,2 | 78,7 | 0,00% | 2.998,3 | 91,4 | 0,00% |
| H2 avisos, 200 VUs | 4.138,5 | 87,2 | 0,00% | 2.895,8 | 113,0 | 0,00% |
| H3 avisos + 20 mil de outro condomínio, 10 VUs | 43,2 | 382,5 | 0,00% | 3.075,3 | 6,5 | 0,00% |
| H3 avisos + ruído, 50 VUs | 42,5 | 2.578,6 | 0,00% | 2.987,7 | 44,9 | 0,00% |
| H3 avisos + ruído, 100 VUs | 41,8 | 4.755,5 | 0,00% | 2.952,2 | 93,1 | 0,00% |
| H4 lista de condomínios (cadastro), 10 VUs | 77,9 | 150,0 | 0,00% | 4.700,5 | 4,1 | 0,00% |
| H4 lista de condomínios, 20 VUs | 79,8 | 296,2 | 0,00% | 4.728,1 | 10,5 | 0,00% |
| H4 lista de condomínios, 50 VUs | 77,9 | 753,7 | 0,00% | 4.660,7 | 29,0 | 0,00% |
| H5 publicar aviso, 50 VUs | 4.575,8 | 28,8 | 0,00% | 7.558,9 | 14,1 | 0,00% |
| H8 health, 50 VUs | 12.588,3 | 8,1 | 0,00% | 15.495,1 | 7,5 | 0,00% |
| H6 50 reservas simultâneas, mesmo horário | 50 aceitos, 0 recusados (4xx), 0 com erro (5xx) | | | 1 aceitos, 49 recusados (4xx), 0 com erro (5xx) | | |
| H7 50 votos simultâneos, mesma pauta | 1 aceitos, 49 recusados (4xx), 0 com erro (5xx) | | | 1 aceitos, 49 recusados (4xx), 0 com erro (5xx) | | |
| H1b mediana do login (1 VU) | inexistente 9,5 ms x senha errada 113,9 ms | | | inexistente 119,4 ms x senha errada 111,7 ms | | |
