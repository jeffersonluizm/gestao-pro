# Código-fonte

| Pasta | O que é | Comandos |
|---|---|---|
| [`GestaoPro.API`](GestaoPro.API) | API REST em ASP.NET Core (.NET 10) com MongoDB | `dotnet run` (ver configuração no README da raiz) |
| [`GestaoPro.API.Tests`](GestaoPro.API.Tests) | Testes de unidade e integração da API (xUnit) | `dotnet test` |
| [`GestaoPro.Web`](GestaoPro.Web) | Painel web (React, TypeScript, Vite, React Router) | `npm install`, `npm run dev`, `npm test`, `npm run lint`, `npm run build` |
| [`GestaoPro.Mobile`](GestaoPro.Mobile) | App (React Native com Expo) | `npm install`, `npx expo start`, `npm test`, `npm run lint`, `npm run typecheck` |

A arquitetura está em [docs/05-Arquitetura da Solução](../docs/05-Arquitetura%20da%20Solução.md) e as decisões em [docs/adr](../docs/adr/README.md). Para subir tudo de uma vez na máquina, use o `iniciar.bat` da raiz.
