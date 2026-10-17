# Publicar a demonstração (Render + Vercel + MongoDB Atlas)

Guia para colocar a demonstração no ar em camadas gratuitas. Tudo que é segredo é colado no painel de cada plataforma: **nada disso vai para o Git**. Decisão registrada em [ADR 0007](adr/0007-hospedagem-gratuita.md).

Resultado final:

| Peça | Onde | Endereço (exemplo) |
|---|---|---|
| API (.NET, Docker) | Render, plano Free | `https://gestaopro-api-demo.onrender.com` |
| Painel web (React) | Vercel, Hobby | `https://gestaopro-painel.vercel.app` |
| App (Expo exportado para web) | Vercel, Hobby | `https://gestaopro-app.vercel.app` |
| Banco | MongoDB Atlas, M0 | cluster só da demonstração |

> Use um banco **exclusivo da demonstração**. Qualquer visitante entra nas contas demo com um clique (`POST /api/demo/entrar`, sem senha), os dados voltam ao estado inicial a cada 90 minutos e há tetos de escrita e de documentos (ver [ADR 0004](adr/0004-modo-demonstracao.md)). Nunca ligue `Demo__Enabled` num ambiente com dados reais.

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
| `Cors__AllowedOrigins__0` | URL do painel na Vercel, sem barra no fim (ex.: `https://gestaopro-painel.vercel.app`) |
| `Cors__AllowedOrigins__1` | URL do app web na Vercel (ex.: `https://gestaopro-app.vercel.app`) |
| `Demo__CodigoCondominio` | código do condomínio demo, forte (ex.: `Residencial-Demo-2026`); fica visível no cartão para testar a ativação por CPF |

Já vêm no blueprint: `ASPNETCORE_ENVIRONMENT=Production`, `MongoDbSettings__DatabaseName=GestaoProDemo`, `JwtSettings__SecretKey` (gerada pelo Render), `Proxy__ConfiarNoProxy=true` (o IP do visitante vem do proxy do Render; sem isso o limite de tentativas trata todo mundo como um IP só), `Demo__Enabled=true`, `Demo__IntervaloResetMinutos=90`, `TZ=America/Sao_Paulo` e os tetos da demonstração (`Demo__LimiteEscrita__PorUsuarioPorMinuto=30`, `Demo__LimiteEscrita__PorIpPorMinuto=60`, `Demo__LimitePorColecao=300`).

3. Aguarde o deploy e confira:
   - `https://<sua-api>.onrender.com/api/health/mongodb` responde `{"connected":true,"database":"GestaoProDemo",...}`;
   - `https://<sua-api>.onrender.com/api/demo` lista os três papéis (sem e-mail nem senha) e o código do condomínio;
   - `https://<sua-api>.onrender.com/swagger` responde 404 (documentação fechada em produção).

O plano Free dorme depois de 15 minutos sem uso. A primeira requisição leva de 30 a 60 s; o painel mostra "Acordando o servidor de demonstração" enquanto isso. Ao acordar, a API restaura a demonstração e as sessões anteriores deixam de valer (401): o painel e o app voltam ao cartão, que entra de novo no mesmo papel.

### Conferir o IP real (limite de tentativas e de escritas)

Os limites por IP só funcionam se a API enxergar o IP do visitante, e não o do proxy do Render. Com `Proxy__ConfiarNoProxy=true`, a API troca o `RemoteIpAddress` pelo último valor do `X-Forwarded-For` (`Proxy__Saltos=1`). Para conferir depois do deploy:

1. Do computador, erre a senha de uma conta qualquer 11 vezes seguidas em `POST /api/Auth/login`: a 11ª resposta é 429.
2. Logo em seguida, do celular **na rede móvel** (outro IP), tente entrar: precisa responder normalmente (401 com senha errada, não 429). Se o celular também receber 429, todos os visitantes estão caindo no mesmo balde: a API está vendo o IP do proxy.
3. Nesse caso, ajuste pela configuração (sem mudar código) e repita os passos 1 e 2 depois de cada tentativa:
   - se o `X-Forwarded-For` chega com mais de um salto (`cliente, proxy`), aumente `Proxy__Saltos` para a quantidade de proxies na frente da API;
   - se a borda manda o IP do visitante em `True-Client-IP` (CDN na frente do Render), defina `Proxy__CabecalhoIp=True-Client-IP`.
4. Se o teste der certo de primeira, não mude nada: `Proxy__Saltos=1` e o `X-Forwarded-For` são o padrão.

O log da partida mostra a configuração efetiva ("Proxy: confiar no proxy=..., saltos=..., cabeçalho do IP=..."). Com `Proxy__CabecalhoIp` configurado, um aviso aparece no log se uma requisição chegar sem esse cabeçalho. Valor inválido (por exemplo `Proxy__Saltos=0`) impede a partida, com a chave no erro.

## 4. Painel web: Vercel

1. Em <https://vercel.com>, **Add New > Project**, escolha o repositório e defina **Root Directory = `src/GestaoPro.Web`**. O [`vercel.json`](../src/GestaoPro.Web/vercel.json) já define build (`npm run build`), saída (`dist`), rotas da SPA e cabeçalhos de segurança.
2. Variáveis de ambiente (Production):
   - `VITE_API_BASE_URL = https://<sua-api>.onrender.com` (sem barra no fim). **Obrigatória e com https:** sem ela o build falha com a mensagem "VITE_API_BASE_URL não definida", em vez de publicar um painel que chamaria a própria Vercel.
   - `VITE_SITE_URL = https://<seu-painel>.vercel.app` (opcional): endereço público usado na imagem de prévia do link (`og:image`). Sem ela, o build usa o domínio de produção que a Vercel informa (`VERCEL_PROJECT_PRODUCTION_URL`).
3. Deploy. Confira: a landing mostra o cartão "Explore a demonstração"; "Entrar como síndico" abre o painel da gestão.
4. Cabeçalho `Content-Security-Policy` (no [`vercel.json`](../src/GestaoPro.Web/vercel.json)): libera chamadas para `https://*.onrender.com`. Com o endereço definitivo da API, troque o curinga pelo host exato em `connect-src` (e no `wss://` do SignalR) nos dois `vercel.json`. Para conferir a prévia do link, cole a URL no WhatsApp ou no [Post Inspector do LinkedIn](https://www.linkedin.com/post-inspector/).

## 5. App (Expo para web): Vercel

1. Outro projeto na Vercel com **Root Directory = `src/GestaoPro.Mobile`**. O [`vercel.json`](../src/GestaoPro.Mobile/vercel.json) roda `npx expo export --platform web` e publica `dist`.
2. Variável de ambiente: `EXPO_PUBLIC_API_URL = https://<sua-api>.onrender.com`. **Obrigatória e com https** no export: sem ela, o `app.config.js` interrompe o build com a mensagem "EXPO_PUBLIC_API_URL não definida". Em produção o app também não libera tráfego sem TLS (`usesCleartextTraffic`); para um build de teste na rede local, use `GESTAOPRO_PERMITIR_HTTP=1`.
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
| Render | `Demo__CodigoCondominio` | recomendada | sem ela a API sorteia a cada partida |
| Render | `Demo__IntervaloResetMinutos`, `Demo__LimiteEscrita__*`, `Demo__LimitePorColecao` | já no blueprint | tetos da demonstração |
| Render | `Proxy__Saltos`, `Proxy__CabecalhoIp` | só se precisar | ver "Conferir o IP real" |
| Render | `TZ` | já no blueprint | `America/Sao_Paulo` (logs) |
| Vercel (painel) | `VITE_API_BASE_URL` | sim | URL https da API; sem ela o build falha |
| Vercel (painel) | `VITE_SITE_URL` | não | endereço público para o `og:image` |
| Vercel (app) | `EXPO_PUBLIC_API_URL` | sim | URL https da API; sem ela o export falha |

## Produção real (fora do portfólio)

Mesma receita, com `Demo__Enabled` **ausente ou `false`**, banco próprio sem acesso aberto, `Cors__AllowedOrigins__*` com os domínios definitivos e a primeira conta administradora criada pela instalação inicial, que a API só aceita enquanto não existe nenhum administrador (não há conta padrão).
