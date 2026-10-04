# 0001. Monólito modular em vez de microsserviços

**Status:** aceita (2026-10-04). Substitui a proposta acadêmica de microsserviços com gateway e RabbitMQ.

## Contexto
A especificação acadêmica (versão original do docs/05) previa um microsserviço por domínio, API Gateway (Ocelot/YARP), RabbitMQ e um banco por serviço. O código entregue sempre foi uma única API ASP.NET Core com um MongoDB. O domínio é um só (um condomínio e suas rotinas), o time é pequeno e a carga é baixa.

## Decisão
Manter **uma API** organizada em camadas com responsabilidade única: `Controllers` finos (HTTP e decisão de acesso), `Services` (regras de negócio), `Repositories` (MongoDB.Driver) e `Middleware` (acesso, cabeçalhos de segurança, erro como ProblemDetails). Os módulos (avisos, reservas, portaria, financeiro, assembleias) são pastas e interfaces, não processos.

## Consequências
- Um deploy, uma configuração e consultas simples; o teste de integração sobe a API inteira com `WebApplicationFactory`.
- As fronteiras (interfaces de serviço e de repositório) permitem extrair um módulo depois, se carga ou time justificarem.
- Sem mensageria: notificações em tempo real usam SignalR no próprio processo.
