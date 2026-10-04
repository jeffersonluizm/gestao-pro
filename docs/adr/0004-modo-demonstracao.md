# 0004. Modo demonstração por flag de servidor

**Status:** aceita.

## Contexto
A versão anterior criava uma conta administradora com senha fixa e fraca sempre que a API subia em `Development`, e o painel e o app mostravam essa credencial no código. Isso misturava "ambiente de desenvolvimento" com "demonstração", deixava uma senha versionada (porta dos fundos se alguém subisse em Development contra um banco real) e repetia o segredo em três lugares. Uma demonstração publicada roda em `Production` e ficaria sem contas.

## Decisão
- Flag `Demo:Enabled` (padrão **desligada**), independente do ambiente. Sem ela, nenhuma conta demo existe e `GET /api/demo` responde 404.
- Senhas e código do condomínio vêm de `Demo:Senha` e `Demo:CodigoCondominio` (validados pela política de senha; valor fraco impede a partida) ou são sorteados na partida.
- `GET /api/demo` (anônimo) é a fonte única das contas (síndico, morador, porteiro) e do código; painel e app mostram "Entrar como ..." com um clique a partir dele.
- Contas e condomínio demo são protegidos: não trocam senha, não são desativados, rebaixados ou excluídos (403 com mensagem). O `DemoResetService` restaura tudo a cada `Demo:IntervaloResetHoras`.

## Consequências
- Produção real fica com a flag desligada; a demonstração hospedada usa um banco só dela.
- As credenciais demo são públicas por definição (estão no endpoint), por isso as contas são protegidas e os dados voltam ao estado inicial.
