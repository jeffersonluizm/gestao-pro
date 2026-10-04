# 0003. JWT no localStorage do painel, por enquanto

**Status:** aceita com ressalva.

## Contexto
O mesmo JWT serve ao painel web e ao app (AsyncStorage). A alternativa mais segura no navegador seria um cookie `httpOnly` com `SameSite`, que exige proteção contra CSRF e painel e API sob o mesmo domínio, o que a hospedagem gratuita em domínios diferentes (Vercel e Render) complica.

## Decisão
Manter o token no `localStorage` do painel e reduzir o risco de XSS por outros meios: React sem HTML injetado, CSP e cabeçalhos de segurança, validação na API, expiração do token e saída automática no 401.

## Consequências
- Um XSS no painel poderia ler o token: é uma limitação registrada no README.
- Evolução prevista: cookie `httpOnly` emitido pela API com painel e API no mesmo domínio, e lista de revogação (hoje o token vale até expirar, mesmo após desativar a conta).
