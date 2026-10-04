# 0002. Acesso por dono e por condomínio decidido na API

**Status:** aceita.

## Contexto
O sistema guarda dados de vários condomínios no mesmo banco (LGPD). Antes, o middleware só verificava "logado ou não" e "administrador ou não"; listas traziam tudo e filtravam em memória, e alguns controllers confiavam em cabeçalhos (`X-User-Id`) como identidade.

## Decisão
- A identidade vem sempre do JWT (`userId`, `role`, `condominiumId`). Cabeçalhos só indicam o morador alvo quando quem chama é da equipe, e o alvo precisa ser do mesmo condomínio.
- Regras puras em `Services/Access/ResourceAccess.cs`; tradução para HTTP em `Controllers/AccessResults.cs`.
- Item de outro condomínio responde **404**, igual a inexistente (não revela existência). Sem permissão no mesmo condomínio: **403**.
- Listas vão ao banco já filtradas pelo condomínio do token, com índice e paginação opcional.

## Consequências
- Painel e app escondem o que o papel não usa, mas quem decide é sempre a API (testes de acesso por papel cobrem cada rota).
- Dados antigos sem `condominiumId` são preenchidos em segundo plano (`LegacyCondominiumBackfill`).
