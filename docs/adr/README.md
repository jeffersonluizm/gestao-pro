# Decisões de arquitetura (ADRs)

Registros curtos das decisões que alguém revisando o projeto provavelmente questionaria. Formato: contexto, decisão, consequências.

| ADR | Decisão |
|---|---|
| [0001](0001-monolito-modular.md) | Monólito modular em vez de microsserviços |
| [0002](0002-acesso-decidido-no-servidor.md) | Acesso por dono e por condomínio decidido na API, a partir do token |
| [0003](0003-token-no-navegador.md) | JWT no `localStorage` do painel (e não cookie httpOnly), por enquanto |
| [0004](0004-modo-demonstracao.md) | Modo demonstração por flag de servidor, sem credencial no código |
| [0005](0005-trava-de-reserva.md) | Trava por área e data contra reservas simultâneas |
| [0006](0006-configuracao-e-segredos.md) | Segredos fora do Git e precedência da configuração |
| [0007](0007-hospedagem-gratuita.md) | Demonstração hospedada em camadas gratuitas (Render, Vercel, Atlas) |
