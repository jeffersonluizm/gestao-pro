# 0006. Segredos fora do Git e precedência da configuração

**Status:** aceita.

## Contexto
A string de conexão e a chave JWT ficavam num `appsettings.Local.json` que a API carregava **depois** das variáveis de ambiente: o arquivo local vencia, e um ambiente descartável (teste de carga, CI) não conseguia apontar para outro banco. O `dotnet publish` também copiava esse arquivo para a pasta de saída.

## Decisão
- Ordem: `appsettings.json` < `appsettings.{Ambiente}.json` < `appsettings.Local.json` < variáveis de ambiente < linha de comando (`Settings/LocalSettings.cs`, com teste).
- `appsettings.Local.json` nunca vai para `bin/` nem para o publish (`GestaoPro.API.csproj`) e está no `.gitignore` e no `.dockerignore`.
- A API recusa a partida sem string de conexão ou com chave JWT curta, dizendo o que falta.
- Local: o `iniciar.bat` gera um `.env` com segredos aleatórios. Hospedado: variáveis no painel do Render (`render.yaml` com `sync: false` ou `generateValue`).

## Consequências
Nenhum segredo no repositório; quem clona sobe tudo sem pedir credencial a ninguém.
