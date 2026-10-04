# GestaoPro — Sistema de Gestão Condominial

`Análise e Desenvolvimento de Sistemas`

`Eixo 4 — Projeto: Arquitetura de Sistemas Distribuídos`

`2026/1`

Sistema distribuído de gestão condominial composto por uma API REST em C# ASP.NET Core, um painel web em React (TypeScript) e um aplicativo mobile em React Native (Expo). A plataforma permite que administradores gerenciem moradores, avisos, reservas de áreas comuns, assembleias, estacionamento, visitantes, encomendas e financeiro, enquanto moradores acessam essas funcionalidades pelo aplicativo móvel.

## Integrantes

* Deuslano Veloso de Oliveira Junior
* Gustavo Marques Pereira
* Jefferson Luiz Mendonça da Silva

## Orientador

* Leonardo Vilela Cardoso

## Instruções de utilização

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- [Expo CLI](https://docs.expo.dev/get-started/installation/) (`npm install -g expo-cli`)

### Configuração da API

A connection string do MongoDB e a chave JWT não ficam no repositório. Crie o arquivo `src/GestaoPro.API/appsettings.Local.json` (ele já é ignorado pelo Git e pelo Docker):

```json
{
  "MongoDbSettings": { "ConnectionString": "mongodb+srv://<usuario>:<senha>@<cluster>/" },
  "JwtSettings": { "SecretKey": "<chave aleatória com pelo menos 32 caracteres>" }
}
```

Em produção (Render), configure as variáveis de ambiente `MongoDbSettings__ConnectionString` e `JwtSettings__SecretKey` **antes** do deploy: sem elas a API recusa a partida com uma mensagem dizendo o que falta.

#### Cadastro de moradores e limites de tentativa

O morador ativa a própria conta pela tela "Cadastrar-se" (painel web ou app) informando o condomínio, o CPF e o **código do condomínio**. O código é a senha padrão do condomínio, definida no cadastro do condomínio e entregue pela administração aos moradores (é também a senha inicial das contas criadas pela administração). Regras:

- o código é obrigatório ao cadastrar um condomínio, com ao menos 8 caracteres e sem sequências, repetições ou senhas comuns (123456, 12345678, abcdefgh...); na edição, em branco mantém o atual. Espaços nas pontas são ignorados;
- condomínios antigos que ainda têm um código fraco (como 123456) não aceitam autoatendimento até a administração definir um código novo;
- só contas de morador são ativadas assim; síndico, administrador e porteiro recebem o acesso da administração. Quem já tem credencial (ativa ou desativada) também precisa falar com a administração;
- a verificação do CPF não mostra nome nem diz o motivo de uma recusa: a mensagem é sempre a mesma;
- a troca de senha sem login vale só no primeiro acesso (conta marcada para trocar a senha).

Em desenvolvimento, o condomínio de demonstração criado pela API usa o código `Demo-Condominio-2026`.

Limites de tentativa (acima deles a API responde 429 com o cabeçalho `Retry-After`):

| Rotas | Limite padrão |
|---|---|
| `POST /api/auth/login` | 10 por minuto por IP |
| `POST /api/auth/check-cpf` e `POST /api/auth/register` | 10 por minuto por IP e 30 por minuto por condomínio |
| `PATCH /api/auth/change-password` | 10 por minuto por IP |

Os valores ficam em `RateLimiting:Auth:PermitLimit`, `RateLimiting:Auth:PerCondominiumPermitLimit` e `RateLimiting:Auth:WindowSeconds` (segundos).

**Proxy (Render):** atrás do proxy, o IP do cliente chega no cabeçalho `X-Forwarded-For`. A API só confia nesse cabeçalho com `Proxy:ConfiarNoProxy=true` (variável de ambiente `Proxy__ConfiarNoProxy=true`); o padrão é `false`, porque sem um proxy na frente qualquer cliente poderia forjar o cabeçalho. No Render, ligue a opção. Para confirmar, suba temporariamente um log do `X-Forwarded-For` e do `RemoteIpAddress` em uma requisição, confira que o IP mostrado é o do seu computador (e não o do proxy) e remova o log em seguida. Com a opção desligada atrás do proxy, todos os acessos contam como um único IP.

#### Dados antigos sem condomínio

Cada leitura e alteração é limitada ao condomínio de quem está logado. Visitantes e vagas gravados antes dessa regra podem estar sem o campo `condominiumId`, e a administração recebe "Registro não encontrado" ao editá-los. Por isso, a cada partida a API preenche o campo em segundo plano, sem atrasar a subida:

- visitante: usa o condomínio do morador visitado;
- vaga: usa o condomínio do morador da vaga, de quem a reservou ou da unidade; se não houver nenhum vínculo e o sistema tiver um único condomínio, usa esse condomínio.

O preenchimento só mexe em registros sem condomínio, então pode rodar quantas vezes for preciso. Se algum registro não puder ser resolvido (vaga sem vínculo com mais de um condomínio cadastrado), o log da API mostra quantos ficaram sem condomínio; preencha `condominiumId` desses registros direto no banco. Se o banco estiver fora do ar, o preenchimento só registra o erro no log e a API continua de pé.

### Executando o projeto

**Opção rápida (Windows):** execute o arquivo `iniciar.bat` na raiz do repositório. Ele inicia a API, o painel web e o app mobile simultaneamente. Para o celular encontrar a API, o script usa, nesta ordem: a variável `EXPO_PUBLIC_API_URL` já definida, o arquivo `src/GestaoPro.Mobile/.env` ou o IP desta máquina na rede local, detectado automaticamente.

**Manualmente:**

1. **API (Backend):**
   ```bash
   cd src/GestaoPro.API
   dotnet run
   ```
   A API estará disponível em `http://localhost:5088` (porta definida em `src/GestaoPro.API/Properties/launchSettings.json`) com Swagger em `/swagger`.

2. **Painel Web:**
   ```bash
   cd src/GestaoPro.Web
   npm install
   npm run dev
   ```
   O painel estará disponível em `http://localhost:3000`. Em desenvolvimento, as chamadas a `/api` e `/notificacaoHub` passam por um proxy que aponta para a API local (`http://localhost:5088`). Os testes do painel rodam com `npm test`.

3. **App Mobile:**
   ```bash
   cd src/GestaoPro.Mobile
   npm install
   cp .env.example .env   # e troque pelo IP do seu PC na rede
   npx expo start
   ```
   Escaneie o QR Code com o app Expo Go ou execute em emulador. No celular, `localhost` é o próprio aparelho: por isso o app precisa do IP do PC em `EXPO_PUBLIC_API_URL`.

### Variáveis de ambiente

Os arquivos `.env` não vão para o Git; cada pasta tem um `.env.example` para copiar.

| Variável | Onde | Para que serve | Padrão |
|---|---|---|---|
| `VITE_API_PROXY_TARGET` | `src/GestaoPro.Web/.env` | Destino do proxy do `npm run dev`. Use a URL do Render só para testar o painel contra a API de produção. | `http://localhost:5088` |
| `VITE_API_BASE_URL` | `src/GestaoPro.Web/.env` | URL da API no build de produção (`npm run build`). | API do Render |
| `EXPO_PUBLIC_API_URL` | `src/GestaoPro.Mobile/.env` | Endereço da API usado pelo app, por exemplo `http://192.168.0.10:5088`. | `http://localhost:5088` (só funciona no navegador do mesmo PC) |
| `MongoDbSettings__ConnectionString`, `JwtSettings__SecretKey` | ambiente da API | Segredos da API (alternativa ao `appsettings.Local.json`). | nenhum: a API não sobe sem eles |

O hub de notificações em tempo real (`/notificacaoHub`, SignalR) exige login: envie o mesmo token JWT da API (cabeçalho `Authorization` ou parâmetro `access_token`).

# Documentação

<ol>
<li><a href="docs/01-Documentação de Contexto.md"> Documentação de Contexto</a></li>
<li><a href="docs/02-Especificação do Projeto.md"> Especificação do Projeto</a></li>
<li><a href="docs/03-Metodologia.md"> Metodologia</a></li>
<li><a href="docs/04-Projeto de Interface.md"> Projeto de Interface</a></li>
<li><a href="docs/05-Arquitetura da Solução.md"> Arquitetura da Solução</a></li>
<li><a href="docs/06-Template Padrão da Aplicação.md"> Template Padrão da Aplicação</a></li>
<li><a href="docs/07-Programação de Funcionalidades.md"> Programação de Funcionalidades</a></li>
<li><a href="docs/08-Registro de Testes Unitários.md"> Registro de Testes Unitários</a></li>
<li><a href="docs/09-Registro de Testes de Integração.md"> Registro de Testes de Integração</a></li>
<li><a href="docs/10-Registro de Testes de Sistema.md"> Registro de Testes de Sistema</a></li>
<li><a href="docs/11-Registro de Contribuição.md"> Registro de Contribuição</a></li>
<li><a href="docs/12-Apresentação do Projeto.md"> Apresentação do Projeto</a></li>
<li><a href="docs/13-Referências.md"> Referências</a></li>
</ol>

# Código

<li><a href="src/README.md"> Código Fonte</a></li>

# Apresentação

<li><a href="presentation/README.md"> Apresentação da solução</a></li>
