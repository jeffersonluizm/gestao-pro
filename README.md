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

### Executando o projeto

**Opção rápida (Windows):** execute o arquivo `iniciar.bat` na raiz do repositório. Ele inicia a API, o painel web e o app mobile simultaneamente.

**Manualmente:**

1. **API (Backend):**
   ```bash
   cd src/GestaoPro.API
   dotnet run
   ```
   A API estará disponível em `http://localhost:5000` com Swagger em `/swagger`.

2. **Painel Web:**
   ```bash
   cd src/GestaoPro.Web
   npm install
   npm run dev
   ```
   O painel estará disponível em `http://localhost:3000`.

3. **App Mobile:**
   ```bash
   cd src/GestaoPro.Mobile
   npm install
   npx expo start
   ```
   Escaneie o QR Code com o app Expo Go ou execute em emulador.

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
