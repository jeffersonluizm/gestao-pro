# 0004. Modo demonstração por flag de servidor

**Status:** aceita (revista em 2026-10-15: entrada sem senha, época da sessão e tetos de uso).

## Contexto
A versão anterior criava uma conta administradora com senha fixa e fraca sempre que a API subia em `Development`, e o painel e o app mostravam essa credencial no código. Isso misturava "ambiente de desenvolvimento" com "demonstração", deixava uma senha versionada (porta dos fundos se alguém subisse em Development contra um banco real) e repetia o segredo em três lugares. Uma demonstração publicada roda em `Production` e ficaria sem contas.

Na primeira versão do modo demonstração, a senha das contas demo era publicada em `GET /api/demo` e aparecia no cartão do painel. Duas falhas apareceram na revisão: o reset recriava as contas com ids novos e o token antigo (8 h) continuava válido apontando para ids que não existiam mais (listas vazias, perfil 404 depois de o servidor dormir); e a demonstração pública não tinha teto de escrita nem de tamanho, então um script com o token do síndico demo esgotaria os 512 MB do Atlas gratuito.

## Decisão
- Flag `Demo:Enabled` (padrão **desligada**), independente do ambiente. Sem ela, nenhuma conta demo existe e `GET /api/demo` e `POST /api/demo/entrar` respondem 404.
- **Entrada sem senha.** `POST /api/demo/entrar {"papel": "sindico" | "morador" | "porteiro"}` (lista fixa, limitador próprio por IP) devolve a sessão pronta da conta demo do papel. As contas demo guardam o hash de um valor aleatório: login por senha nelas dá 401. Nenhuma senha aparece em resposta da API nem na tela; `Demo:Senha` deixou de existir.
- `GET /api/demo` devolve só papéis, nomes, o código do condomínio e os CPFs pré-cadastrados. O código continua visível porque serve para demonstrar a ativação de acesso por CPF. Ele vem de `Demo:CodigoCondominio` (forte; valor fraco impede a partida) ou é sorteado na partida.
- **Janela do reset.** Enquanto o reset roda, toda escrita em `/api` (inclusive login e entrada na demo) responde 503 com `Retry-After`; as escritas em andamento terminam antes da limpeza. A época muda antes de limpar, e um conflito ao semear limpa e semeia de novo. O seed sempre cria tudo e protege por id (nunca por nome), e devolve nome, endereço, regras e regulamento do condomínio ao original.
- **Credenciais.** Na rota da administração, a credencial segue o papel e o condomínio do cadastro da pessoa, não usa o domínio reservado `@demo.gestaopro.local` e não é criada para registros semeados. `/api/demo/entrar` só emite sessão para a conta semeada (condomínio, papel e e-mail conferidos).
- **Época da demonstração.** Cada reset (na partida e a cada `Demo:IntervaloResetMinutos`, padrão 90, entre 15 e 1440) troca a época. O token emitido com a flag ligada leva a claim `demoEpoch`; na validação do JWT, época diferente da atual vira 401. O painel e o app já voltam ao login no 401, e o cartão da demonstração entra de novo no mesmo papel, uma vez por minuto no máximo.
- **Registros protegidos.** Contas demo não trocam senha, nome, telefone, unidade, papel nem acesso. Os registros semeados (áreas comuns, unidade 101, CPFs pré-cadastrados) não são removidos nem desativados. O condomínio demo não é excluído nem tem nome ou código trocados. Tudo responde 403 com mensagem, e o que o visitante cria continua editável.
- **Tetos de uso** (só com a flag): escritas por conta e IP e por IP por minuto, inclusive as rotas autenticadas de `/api/auth` (só as anônimas têm balde próprio) (`Demo:LimiteEscrita:PorUsuarioPorMinuto`, padrão 30, e `PorIpPorMinuto`, padrão 60; acima, 429 com `Retry-After`); documentos por coleção no condomínio demo, votos inclusive (`Demo:LimitePorColecao`, padrão 300; acima, 409 com mensagem). Históricos de ocorrência, vaga e visitante guardam as últimas 50 entradas. Valores inválidos em `Demo:*`, `Proxy:*` e `Limites:*` impedem a partida. Em toda a API, com ou sem a flag: corpo de requisição até 64 KB (413) e tamanho máximo em todo texto de entrada (`[StringLength]` nos DTOs).
- Bancos que passaram pelas versões antigas: na partida, `LegacySeedRetirement` desativa `admin@gestaopro.local` se a senha ainda for a antiga e troca o código público do condomínio antigo (CNPJ `00.000.000/0001-00`) por um aleatório. Nada é criado com esses valores; eles só servem para reconhecer o que precisa ser desativado.

## Consequências
- Produção real fica com a flag desligada; a demonstração hospedada usa um banco só dela.
- Quem explora a demo entra com um clique e não vê senha nenhuma. O reset derruba as sessões abertas, e o cartão devolve a pessoa ao mesmo papel.
- Os tetos tornam o custo de um abuso previsível: no pior caso, 300 documentos por coleção até o próximo reset.
- O teste de estresse liga a flag e precisa subir os tetos por variável de ambiente (`tests/carga/subir-ambiente.ps1`), senão mede o limitador em vez da API.
