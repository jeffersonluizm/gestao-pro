# Registro de Contribuição

Este documento registra as contribuições de cada membro da equipe ao longo das etapas do projeto GestaoPro — Sistema de Gestão Condominial.

## Visão Geral

| Membro | Papel Principal | Foco |
|--------|----------------|------|
| Jefferson Luiz Mendonça da Silva | Scrum Master / Full Stack | Autenticação, login/cadastro, avisos, reservas, áreas comuns, calendário, responsividade, gestão do projeto |
| Deuslano Veloso de Oliveira Junior | Full Stack | Pets, encomendas, visitantes, estacionamento, ocorrências, frontend web/mobile |
| Gustavo Marques Pereira | Full Stack | Assembleias, votações, financeiro (cobranças/boletos/pagamentos), modelagem de dados |

---

## Etapa 1 — Documentação de Contexto e Especificação (04/03 – 08/03/2026)

### Jefferson Luiz Mendonça da Silva
- Planejamento de cronograma, custos e pessoal do projeto
- Organização da documentação (personas, histórias de usuários, requisitos)
- Diagrama de casos de uso

### Deuslano Veloso de Oliveira Junior
- Diagrama de classes MongoDB

### Gustavo Marques Pereira
- Definição e documentação da arquitetura distribuída do projeto

---

## Etapa 2 — Desenvolvimento do Back-end (09/03 – 12/04/2026)

### Jefferson Luiz Mendonça da Silva
- Implementação de autenticação e autorização JWT (`AuthController.cs`, `AuthAppService.cs`)
- Endpoints de usuários, condomínios e unidades (`UsersController.cs`, `CondominiumsController.cs`, `UnitsController.cs`)
- Endpoints de avisos e comunicados (`AnnouncementsController.cs`)
- Endpoints de reservas e áreas comuns (`ReservationsController.cs`, `CommonAreasController.cs`)
- Configuração do ambiente ASP.NET Core e integração com MongoDB
- Documentação da API com Swagger
- Gestão do quadro de trabalho no GitHub

### Deuslano Veloso de Oliveira Junior
- Endpoints de visitantes e encomendas (`VisitorController.cs`, `PackageController.cs`, `DeliveryController.cs`)
- Endpoints de pets (`PetController.cs`)
- Endpoints de estacionamento (`ParkingController.cs`)
- Endpoints de ocorrências (`OccurrencesController.cs`)
- Atualização da arquitetura da solução

### Gustavo Marques Pereira
- Modelagem de dados no MongoDB
- Endpoints de assembleias e votações (`AssemblyController.cs`, `VoteController.cs`)
- Endpoints de financeiro/cobranças (`BillsController.cs`, `PaymentsController.cs`)
- Atualização da arquitetura da solução

---

## Etapa 3 — Desenvolvimento Web (13/04 – 10/05/2026)

### Jefferson Luiz Mendonça da Silva
- Tela de login e cadastro (web)
- Tela de gerenciamento de avisos e notificações
- Tela de gerenciamento de reservas e áreas comuns
- Tela de gerenciamento de usuários
- Integração do frontend web com a API
- Frontend de reservas e áreas comuns
- Gestão do quadro Kanban

### Deuslano Veloso de Oliveira Junior
- Tela de gerenciamento de pets
- Tela de gerenciamento de encomendas
- Tela de gerenciamento de visitantes e estacionamento
- Configuração do Vite e ajustes de frontend

### Gustavo Marques Pereira
- Tela de assembleias e votações
- Tela de gerenciamento financeiro
- Elaboração de testes unitários

---

## Etapa 4 — Desenvolvimento Mobile (11/05 – 31/05/2026)

### Jefferson Luiz Mendonça da Silva
- Telas de login e cadastro (mobile — `LoginScreen.tsx`, `RegisterScreen.tsx`)
- Implementação do `AuthContext.tsx` e `AppNavigator.tsx`
- Telas de Home, Reservas, Áreas Comuns, Perfil
- Responsividade nativa com utilitários `sw/sf/HP` (`responsive.ts`)
- Frame de celular no preview web do Expo
- Correções de navegação, tab bar e logout
- Implementação de alterar senha
- Build de APK via Expo EAS
- Configuração do `iniciar.bat` para execução simultânea
- Gestão do quadro Kanban

### Deuslano Veloso de Oliveira Junior
- Tela de encomendas e correspondência (`EncomendasScreen.tsx`, `DeliveriesScreen.tsx`)
- Tela de pets (`PetsScreen.tsx`)
- Tela de visitantes (`VisitantesScreen.tsx`)
- Tela de ocorrências (`OccurrencesScreen.tsx`)

### Gustavo Marques Pereira
- Tela de assembleias e votações (`AssembleiasScreen.tsx`, `VotosScreen.tsx`)
- Tela de visualização financeira
- Melhorias em assembleias, votação, web e mobile
- Elaboração de testes de integração

---

## Etapa 5 — Considerações Finais e Entrega (01/06 – 21/06/2026)

### Jefferson Luiz Mendonça da Silva
- Revisão geral da documentação
- Reorganização e atualização de todas as documentações do projeto
- Gestão do quadro de trabalho no GitHub
- Preparação da apresentação final

### Deuslano Veloso de Oliveira Junior
- Avaliação dos frameworks e tecnologias utilizadas
- Testes de sistema
- Preparação da apresentação final

### Gustavo Marques Pereira
- Análise crítica e proposta de melhorias
- Testes de sistema
- Preparação da apresentação final

---

## Ferramentas de Comunicação e Colaboração

- **WhatsApp:** comunicação diária da equipe
- **GitHub Projects:** gerenciamento de tarefas e quadro Kanban
- **GitHub:** versionamento de código e revisão de Pull Requests
- **Reuniões presenciais:** durante as aulas para alinhamento
