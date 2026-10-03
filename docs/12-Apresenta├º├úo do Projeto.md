# Apresentacao do Projeto - GestaoPro

Este documento apresenta a estrutura da apresentacao final do projeto GestaoPro, organizada slide a slide para a defesa academica.

---

## Slide 1 - Capa

**GestaoPro - Sistema de Gestao Condominial**

Analise e Desenvolvimento de Sistemas - PUC Minas

Eixo 4 - Infraestrutura e Arquitetura Distribuida

1o Semestre de 2026

---

## Slide 2 - Equipe

### Integrantes

- Deuslano
- Gustavo
- Jefferson

### Orientador

- Leonardo Vilela Cardoso

### Disciplina

- Projeto: Infraestrutura e Arquitetura Distribuida (Turma 1)

---

## Slide 3 - Problema

### Gestao Condominial Fragmentada

- A administracao de condominios e feita de forma **descentralizada e manual**, com informacoes espalhadas em planilhas, grupos de WhatsApp e murais fisicos.
- **Falta de transparencia** nas decisoes de assembleia e na prestacao de contas financeira.
- **Dificuldade de comunicacao** entre sindico, administradora e moradores.
- **Processos burocraticos** para reservas de areas comuns, registro de ocorrencias e controle de acesso.
- Moradores nao possuem uma **plataforma unica** para acompanhar avisos, boletos, votacoes e reservas.

---

## Slide 4 - Solucao

### GestaoPro: Plataforma Distribuida de Gestao Condominial

O GestaoPro e um sistema distribuido que centraliza a gestao condominial em uma unica plataforma, acessivel via **Web** e **Mobile**.

**Pilares da solucao:**

- **Centralizacao**: Todas as informacoes do condominio em um unico lugar
- **Transparencia**: Votacoes digitais, historico financeiro e avisos rastreados
- **Acessibilidade**: Acesso pelo navegador (Web) ou celular (Mobile)
- **Automacao**: Notificacoes, controle de reservas e gestaode boletos automatizados

---

## Slide 5 - Arquitetura da Solucao

### Arquitetura Distribuida

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   React Web     │     │  React Native   │     │    Swagger UI   │
│   (Frontend)    │     │    (Mobile)     │     │  (Documentacao) │
└────────┬────────┘     └────────┬────────┘     └────────┬────────┘
         │                       │                       │
         └───────────────────────┼───────────────────────┘
                                 │
                        ┌────────▼────────┐
                        │  ASP.NET Core   │
                        │   API (.NET 10) │
                        │   + JWT Auth    │
                        └────────┬────────┘
                                 │
                        ┌────────▼────────┐
                        │  MongoDB Atlas  │
                        │  (NoSQL Cloud)  │
                        └─────────────────┘
```

| Camada | Tecnologia | Funcao |
|---|---|---|
| API Backend | ASP.NET Core 10 (C#) | Logica de negocio, autenticacao e endpoints REST |
| Frontend Web | React | Interface web para sindicos e moradores |
| Mobile | React Native | Aplicativo mobile para moradores |
| Banco de Dados | MongoDB Atlas | Armazenamento NoSQL em nuvem |
| Autenticacao | JWT Bearer Token | Seguranca e controle de acesso por perfil |

---

## Slide 6 - Funcionalidades Principais

### Modulos do Sistema

| Modulo | Descricao | Perfis de Acesso |
|---|---|---|
| Autenticacao JWT | Login seguro com tokens e controle de perfis (Sindico, Morador, Porteiro) | Todos |
| Reservas de Areas Comuns | Agendamento de churrasqueira, salao de festas, piscina, etc. | Morador, Sindico |
| Avisos e Comunicados | Publicacao e leitura de avisos do condominio | Sindico (cria), Morador (le) |
| Assembleias e Votacoes | Criacao de assembleias com votacao digital e apuracao automatica | Sindico (cria), Morador (vota) |
| Financeiro (Boletos) | Geracao e consulta de boletos condominiais | Sindico (gera), Morador (consulta) |
| Pagamentos | Registro e rastreamento de pagamentos | Sindico, Morador |
| Visitantes | Cadastro e controle de acesso de visitantes | Morador (cadastra), Porteiro (controla) |
| Encomendas | Registro de recebimento e retirada de encomendas | Porteiro (registra), Morador (retira) |
| Pets | Cadastro de animais de estimacao dos moradores | Morador |
| Estacionamento | Gestao de vagas e registro de veiculos | Morador, Sindico |
| Ocorrencias | Registro e acompanhamento de reclamacoes e solicitacoes | Morador (registra), Sindico (gerencia) |

---

## Slide 7 - Demonstracao

### Destaques da Demonstracao

1. **Login e Autenticacao**
   - Login como Sindico e como Morador
   - Demonstracao do controle de acesso por perfil

2. **Fluxo de Reserva**
   - Visualizacao de areas comuns disponiveis
   - Criacao de reserva com validacao de conflito
   - Visualizacao de reservas do morador

3. **Fluxo de Assembleia e Votacao**
   - Criacao de assembleia pelo sindico
   - Registro de voto pelo morador
   - Apuracao automatica dos resultados

4. **Avisos e Comunicacao**
   - Publicacao de aviso pelo sindico
   - Visualizacao pelo morador

5. **Consulta Financeira**
   - Visualizacao de boletos e status de pagamento

---

## Slide 8 - Testes

### Cobertura de Testes Automatizados

O projeto possui **176 testes unitarios** automatizados, todos com status **aprovado**.

| Classe de Teste | Testes | Status |
|---|---|---|
| AnnouncementServiceTests | 22 | Aprovados |
| AssemblyServiceTests | 20 | Aprovados |
| BillServiceTests | 14 | Aprovados |
| CommonAreaServiceTests | 20 | Aprovados |
| ReservationServiceTests | 22 | Aprovados |
| ParkingAppServiceTests | 18 | Aprovados |
| PaymentServiceTests | 12 | Aprovados |
| VoteServiceTests | 25 | Aprovados |
| VoteControllerTests | 4 | Aprovados |
| VoteOperationExceptionTests | 6 | Aprovados |
| **Total** | **176** | **100% aprovados** |

**Ferramentas**: xUnit + Moq + .NET CLI (`dotnet test`)

---

## Slide 9 - Tecnologias

### Stack Tecnologica

| Categoria | Tecnologia | Versao/Detalhes |
|---|---|---|
| Linguagem Backend | C# | .NET 10 |
| Framework Backend | ASP.NET Core | Web API REST |
| Banco de Dados | MongoDB | Atlas (Cloud) |
| Autenticacao | JWT | Bearer Token |
| Frontend Web | React | JavaScript/TypeScript |
| Mobile | React Native | Android e iOS |
| Testes Unitarios | xUnit | Framework de testes .NET |
| Mocking | Moq | Simulacao de dependencias |
| Documentacao API | Swagger | OpenAPI |
| Controle de Versao | Git | GitHub |
| Metodologia | Scrum/Kanban | GitHub Projects |

---

## Slide 10 - Licoes Aprendidas e Melhorias Futuras

### Licoes Aprendidas

- **Arquitetura distribuida** exige planejamento cuidadoso da comunicacao entre camadas
- **MongoDB** oferece flexibilidade no esquema de dados, mas requer atencao na modelagem de relacionamentos
- **JWT** simplifica a autenticacao em sistemas distribuidos com multiplos clientes (Web + Mobile)
- **Testes automatizados** com mocks aceleram o ciclo de desenvolvimento e aumentam a confianca nas mudancas
- **Trabalho em equipe** com Git e metodologias ageis e essencial para entregas consistentes

### Melhorias Futuras

- Implementacao de **notificacoes push** para mobile
- Integracao com **gateway de pagamento** para boletos reais
- Dashboard com **relatorios e graficos** para o sindico
- Sistema de **chat** entre moradores e administracao
- **Testes de carga** e otimizacao de performance
- Implementacao de **CI/CD** com GitHub Actions
- Suporte a **multiplos condominios** na mesma instancia

---

## Slide 11 - Encerramento

### Obrigado!

**GestaoPro - Sistema de Gestao Condominial**

Centralizando a gestao do seu condominio em uma unica plataforma.

---

**Equipe**: Deuslano, Gustavo, Jefferson

**Orientador**: Leonardo Vilela Cardoso

**Repositorio**: [GitHub - GestaoPro](https://github.com/ICEI-PUC-Minas-PMV-ADS/pmv-ads-2026-1-e4-infra-t1-gestao-de-condominio-provisorio)

---

**Perguntas?**

Estamos a disposicao para esclarecer duvidas sobre o projeto, a arquitetura, as decisoes tecnicas e os proximos passos.
