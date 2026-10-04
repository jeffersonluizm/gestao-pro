# Programação de Funcionalidades

<span style="color:red">Pré-requisitos: <a href="2-Especificação do Projeto.md"> Especificação do Projeto</a></span>, <a href="3-Projeto de Interface.md"> Projeto de Interface</a>, <a href="4-Metodologia.md"> Metodologia</a>, <a href="5-Arquitetura da Solução.md"> Arquitetura da Solução</a>

Esta seção descreve a implementação dos requisitos funcionais do sistema GestaoPro, relacionando cada requisito com os artefatos de código criados, as tecnologias utilizadas e as instruções para verificação.

## Tecnologias Utilizadas

| Camada | Tecnologia | Versão |
|--------|-----------|--------|
| Backend (API) | C# ASP.NET Core Web API | .NET 10 |
| Banco de Dados | MongoDB (driver C#) | MongoDB.Driver 2.28.0 |
| Autenticação | JWT (JSON Web Tokens) | Microsoft.AspNetCore.Authentication.JwtBearer 10.0 |
| Hash de Senhas | BCrypt | BCrypt.Net-Next 4.0.3 |
| Documentação API | Swagger / OpenAPI | Swashbuckle.AspNetCore 10.0.1 |
| Geração de QR Code | QRCoder | 1.7.0 |
| Frontend Web | React + TypeScript | React 18.3, Vite 5.4, TypeScript 5.4 |
| Frontend Mobile | React Native + Expo | React Native 0.79.2, Expo 53, TypeScript 5.3 |
| Navegação Mobile | React Navigation | 6.x (bottom-tabs, native-stack) |
| Armazenamento Local (Mobile) | AsyncStorage | 2.1.2 |

## Estrutura do Código-Fonte

```
src/
├── GestaoPro.API/              # Backend - API REST em C# ASP.NET Core
│   ├── Controllers/            # Controladores das rotas da API
│   ├── Models/                 # Modelos de dados (entidades MongoDB)
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Services/               # Lógica de negócio
│   ├── Repositories/           # Acesso a dados (MongoDB)
│   ├── Settings/               # Configurações (banco, JWT, etc.)
│   └── Program.cs              # Ponto de entrada da aplicação
│
├── GestaoPro.API.Tests/        # Testes unitários do backend (xUnit)
│
├── GestaoPro.Web/              # Frontend Web - React + TypeScript + Vite
│   └── src/
│       ├── components/         # Componentes React
│       └── App.tsx             # Componente raiz (módulos, chamadas à API e telas)
│
└── GestaoPro.Mobile/           # Frontend Mobile - React Native + Expo
    └── src/
        ├── api/                # Cliente HTTP para comunicação com a API
        ├── context/            # Contextos React (AuthContext)
        ├── navigation/         # Configuração de navegação (AppNavigator)
        ├── screens/            # Telas do aplicativo
        │   ├── auth/           # Login e Cadastro
        │   └── main/           # Telas principais (Home, Reservas, etc.)
        └── utils/              # Utilitários (formatters, responsive)
```

## Implementação dos Requisitos Funcionais

### Autenticação e Acesso (RF-001 a RF-006)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-001, RF-002, RF-003 | `UsersController.cs`, `CondominiumsController.cs`, `UnitsController.cs` | Jefferson |
| RF-004, RF-005 | `AuthController.cs`, `AuthAppService.cs`, `LoginScreen.tsx`, `RegisterScreen.tsx` | Jefferson |
| RF-006 | `UsersController.cs` (endpoint de atualização de role) | Jefferson |

**Descrição:** O sistema utiliza autenticação JWT. O login aceita matrícula ou e-mail com senha (hash BCrypt). O token JWT contém o `userId`, `role` e `condominiumId`, permitindo controle de acesso por papel (morador/admin). O cadastro de moradores e administradores é feito pelo painel web. As telas de login e cadastro no mobile (`LoginScreen.tsx`, `RegisterScreen.tsx`) consomem o endpoint `api/auth/login`.

**Verificação:** Acessar `http://localhost:5000/swagger` → endpoint `POST /api/auth/login`.

### Reservas de Áreas Comuns (RF-007 a RF-009)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-007, RF-008, RF-009 | `ReservationsController.cs`, `CommonAreasController.cs`, `ReservationsScreen.tsx`, `AreasComunsScreen.tsx` | Jefferson |

**Descrição:** Moradores podem criar reservas de áreas comuns informando área, data e horário. O sistema valida conflitos de horário e atribui automaticamente o `userId` do token JWT (proteção contra fraude de identidade). Administradores gerenciam todas as reservas pelo painel web.

**Verificação:** Swagger → `POST /api/reservations`, `GET /api/commonareas`.

### Avisos e Notificações (RF-010 a RF-012)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-010, RF-011, RF-012 | `AnnouncementsController.cs`, painel web (App.tsx) | Jefferson |

**Descrição:** Administradores publicam avisos com título, conteúdo, categoria e data de expiração. Moradores visualizam avisos ativos no app mobile e no painel web.

**Verificação:** Swagger → `POST /api/announcements`, `GET /api/announcements`.

### Ocorrências e Solicitações (RF-013 a RF-015)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-013, RF-014, RF-015 | `OccurrencesController.cs`, `OccurrencesScreen.tsx` | Deuslano |

**Descrição:** Moradores registram ocorrências (manutenção, reclamação, sugestão) com título, descrição, categoria e prioridade. O sistema mantém histórico de ações. Administradores gerenciam o status (aberta, em_andamento, concluída).

**Verificação:** Swagger → `POST /api/occurrences`, `GET /api/occurrences`.

### Estacionamento (RF-016 a RF-019)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-016, RF-017, RF-018, RF-019 | `ParkingController.cs` | Deuslano |

**Descrição:** Administradores cadastram vagas de estacionamento para visitantes ou uso compartilhado. Moradores visualizam disponibilidade e reservam vagas.

**Verificação:** Swagger → `GET /api/parking`, `POST /api/parking`.

### Encomendas e Correspondência (RF-020, RF-021)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-020, RF-021 | `PackageController.cs`, `DeliveryController.cs`, `EncomendasScreen.tsx`, `DeliveriesScreen.tsx` | Deuslano |

**Descrição:** Administradores registram encomendas recebidas associadas a unidades. Moradores visualizam e confirmam retirada pelo app mobile.

**Verificação:** Swagger → `POST /api/packages`, `GET /api/packages`.

### Visitantes (RF-022, RF-023)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-022, RF-023 | `VisitorController.cs`, `VisitantesScreen.tsx` | Deuslano |

**Descrição:** Registro de visitantes com nome, documento, unidade visitada e controle de entrada/saída.

**Verificação:** Swagger → `POST /api/visitors`, `PATCH /api/visitors/{id}/exit`.

### Assembleias e Votações (RF-024 a RF-026)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-024, RF-025, RF-026 | `AssemblyController.cs`, `VoteController.cs`, `AssembleiasScreen.tsx`, `VotosScreen.tsx` | Gustavo |

**Descrição:** Administradores criam assembleias com tópicos de votação. Moradores participam das votações pelo app mobile. O sistema garante um voto por morador por tópico.

**Verificação:** Swagger → `POST /api/assemblies`, `POST /api/votes`.

### Calendário e Eventos (RF-027, RF-028)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-027, RF-028 | Painel web (App.tsx), tela de calendário | Jefferson |

**Descrição:** Administradores cadastram eventos no calendário geral. Moradores visualizam eventos e atividades.

### Pets (RF-029, RF-030)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-029, RF-030 | `PetController.cs`, `PetsScreen.tsx` | Deuslano |

**Descrição:** Cadastro de pets associados a unidades com nome, espécie, raça e status de vacinação.

**Verificação:** Swagger → `POST /api/pets`, `GET /api/pets`.

### Financeiro (RF-031 a RF-035)

| RF | Artefato | Responsável |
|----|----------|-------------|
| RF-031, RF-032, RF-033, RF-034, RF-035 | `BillsController.cs`, `PaymentsController.cs` | Gustavo |

**Descrição:** Administradores cadastram cobranças e geram boletos para unidades. Moradores visualizam boletos, registram pagamentos e consultam histórico.

**Verificação:** Swagger → `POST /api/bills`, `GET /api/payments`.

## Hospedagem

- **API Backend:** hospedada no Render (plano gratuito) com deploy automático via GitHub
- **Banco de Dados:** MongoDB Atlas (plano M0 gratuito)
- **Frontend Web:** build estático via Vite, servido localmente ou via Vercel
- **App Mobile:** build via Expo EAS para geração de APK Android
