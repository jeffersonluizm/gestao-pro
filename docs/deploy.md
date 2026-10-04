# Publicar a demonstração (Render + Vercel + MongoDB Atlas)

Guia para colocar a demonstração no ar em camadas gratuitas. Tudo que é segredo é colado no painel de cada plataforma: **nada disso vai para o Git**. Decisão registrada em [ADR 0007](adr/0007-hospedagem-gratuita.md).

Resultado final:

| Peça | Onde | Endereço (exemplo) |
|---|---|---|
| API (.NET, Docker) | Render, plano Free | `https://gestaopro-api-demo.onrender.com` |
| Painel web (React) | Vercel, Hobby | `https://gestaopro.vercel.app` |
| App (Expo exportado para web) | Vercel, Hobby | `https://gestaopro-app.vercel.app` |
| Banco | MongoDB Atlas, M0 | cluster só da demonstração |

> Use um banco **exclusivo da demonstração**. As contas demo são públicas (o endpoint `GET /api/demo` mostra as senhas de propósito) e os dados voltam ao estado inicial a cada 6 horas. Nunca ligue `Demo__Enabled` num ambiente com dados reais.

## 1. Repositório

1. Crie um repositório no GitHub (público, para o portfólio) e envie este projeto (`git remote add origin ...` e `git push -u origin main`).
2. Confira que o `.gitignore` deixou de fora `.env`, `appsettings.Local.json` e `node_modules`.
3. O GitHub Actions (`.github/workflows/ci.yml`) roda testes, lint e build a cada push.

## 2. Banco: MongoDB Atlas (gratuito)

1. Em <https://www.mongodb.com/cloud/atlas>, crie um projeto **GestaoPro Demo** e um cluster **M0 (Free)**.
2. **Database Access:** crie um usuário só para a demonstração, com senha gerada pelo Atlas e papel `readWrite` no banco `GestaoProDemo`.
3. **Network Access:** libere `0.0.0.0/0` (o Render gratuito não tem IP fixo). Por isso o usuário do banco precisa ser exclusivo da demo.
4. Copie a *connection string* (`mongodb+srv://...`), com o usuário e a senha do passo 2. Ela será colada no Render, nunca num arquivo.

## 3. API: Render

1. Em <https://render.com>, **New > Blueprint** e escolha o repositório: o Render lê o [`render.yaml`](../render.yaml) e cria o serviço `gestaopro-api-demo` (Docker, plano Free, health check em `/api/health/mongodb`).
2. Preencha as variáveis marcadas como "sync: false":

| Variável | Valor |
|---|---|
| `MongoDbSettings__ConnectionString` | a *connection string* do Atlas (passo 2.4) |
| `Cors__AllowedOrigins__0` | URL do painel na Vercel, sem barra no fim (ex.: `https://gestaopro.vercel.app`) |
| `Cors__AllowedOrigins__1` | URL do app web na Vercel (ex.: `https://gestaopro-app.vercel.app`) |
| `Demo__Senha` | senha das contas demo, forte (ex.: `Explore-Gestao-2026`); a API recusa senha óbvia |
| `Demo__CodigoCondominio` | código do condomínio demo, forte (ex.: `Residencial-Demo-2026`) |

Já vêm no blueprint: `ASPNETCORE_ENVIRONMENT=Production`, `MongoDbSettings__DatabaseName=GestaoProDemo`, `JwtSettings__SecretKey` (gerada pelo Render), `Proxy__ConfiarNoProxy=true` (o IP do visitante vem do proxy do Render; sem isso o limite de tentativas trata todo mundo como um IP só), `Demo__Enabled=true` e `Demo__IntervaloResetHoras=6`.

3. Aguarde o deploy e confira:
   - `https://<sua-api>.onrender.com/api/health/mongodb` responde `{"connected":true,"database":"GestaoProDemo",...}`;
   - `https://<sua-api>.onrender.com/api/demo` lista as três contas;
   - `https://<sua-api>.onrender.com/swagger` responde 404 (documentação fechada em produção).

O plano Free dorme depois de 15 minutos sem uso. A primeira requisição leva de 30 a 60 s; o painel mostra "Acordando o servidor de demonstração" enquanto isso.

## 4. Painel web: Vercel

1. Em <https://vercel.com>, **Add New > Project**, escolha o repositório e defina **Root Directory = `src/GestaoPro.Web`**. O [`vercel.json`](../src/GestaoPro.Web/vercel.json) já define build (`npm run build`), saída (`dist`), rotas da SPA e cabeçalhos de segurança.
2. Variável de ambiente (Production): `VITE_API_BASE_URL = https://<sua-api>.onrender.com` (sem barra no fim).
3. Deploy. Confira: a landing mostra o cartão "Explore a demonstração"; "Entrar como síndico" abre o painel da gestão.

## 5. App (Expo para web): Vercel

1. Outro projeto na Vercel com **Root Directory = `src/GestaoPro.Mobile`**. O [`vercel.json`](../src/GestaoPro.Mobile/vercel.json) roda `npx expo export --platform web` e publica `dist`.
2. Variável de ambiente: `EXPO_PUBLIC_API_URL = https://<sua-api>.onrender.com`.
3. Deploy. Confira o onboarding, o login e "Entrar como morador".

Para celular de verdade, o mesmo app roda no **Expo Go** (`npx expo start` em `src/GestaoPro.Mobile`, com `EXPO_PUBLIC_API_URL` apontando para a API do Render).

## 6. Fechar o ciclo

1. Volte ao Render e confirme que `Cors__AllowedOrigins__0` e `__1` têm exatamente as URLs da Vercel (sem barra no fim). Se mudar, salve: o Render reimplanta sozinho.
2. Atualize os links no topo do [README](../README.md).
3. Teste o link do painel numa aba anônima e num celular: landing, "Entrar como" cada papel, sair.

## Variáveis, resumo

| Onde | Variável | Obrigatória | Observação |
|---|---|---|---|
| Render | `MongoDbSettings__ConnectionString` | sim | só o banco da demo |
| Render | `JwtSettings__SecretKey` | sim | gerada pelo Render (32+ caracteres) |
| Render | `Cors__AllowedOrigins__0`, `__1` | sim | URLs do painel e do app |
| Render | `Proxy__ConfiarNoProxy` | sim | `true` atrás do proxy do Render |
| Render | `Demo__Enabled` | só na demo | **desligado** em produção real |
| Render | `Demo__Senha`, `Demo__CodigoCondominio` | recomendadas | sem elas a API sorteia a cada partida |
| Vercel (painel) | `VITE_API_BASE_URL` | sim | URL da API |
| Vercel (app) | `EXPO_PUBLIC_API_URL` | sim | URL da API |

## Produção real (fora do portfólio)

Mesma receita, com `Demo__Enabled` **ausente ou `false`**, banco próprio sem acesso aberto, `Cors__AllowedOrigins__*` com os domínios definitivos e a primeira conta administradora criada pela instalação inicial, que a API só aceita enquanto não existe nenhum administrador (não há conta padrão).
