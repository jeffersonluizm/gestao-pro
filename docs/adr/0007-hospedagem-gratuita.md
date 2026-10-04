# 0007. Demonstração hospedada em camadas gratuitas

**Status:** aceita (plataformas escolhidas pelo dono do projeto em 2026-10-04).

## Contexto
Quem avalia o portfólio abre um link e testa; não clona nem instala nada.

## Decisão
- API no **Render** (Docker, plano gratuito), a partir do `Dockerfile` e do `render.yaml`.
- Painel e app (Expo exportado para web) na **Vercel**.
- **MongoDB Atlas** gratuito com um banco **só de demonstração**.
- Modo demonstração ligado apenas nesse ambiente, com reset periódico.

## Consequências
- O plano gratuito do Render dorme sem uso: a primeira requisição leva de 30 a 60 s. O painel mostra "Acordando o servidor de demonstração" em vez de parecer quebrado.
- Atrás do proxy do Render, `Proxy__ConfiarNoProxy=true` para o limite de tentativas usar o IP real do visitante.
- Passo a passo em [docs/deploy.md](../deploy.md).
