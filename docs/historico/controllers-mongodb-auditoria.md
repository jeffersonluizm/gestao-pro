# Auditoria de controllers, JavaScript e MongoDB

Data da auditoria: 2026-05-12.

## Estrutura mantida

- API principal: `GestaoPro.API`
- Program principal: `GestaoPro.API/Program.cs`
- Front principal: `src/GestaoPro.Web`
- JavaScript/TypeScript ativo do front: `src/GestaoPro.Web/src/App.tsx`
- Proxy do front para API: `src/GestaoPro.Web/vite.config.ts`

## Ultimas origens verificadas

| Funcionalidade | Origem mais recente encontrada | Data | Status na API consolidada |
| --- | --- | --- | --- |
| Delivery/Encomendas QR | `origin/Deuslano-Junior:src/GestaoCondominioNet10/Controllers/DeliveryController.cs` | 2026-05-12 | Integrado em `GestaoPro.API/Controllers/DeliveryController.cs` |
| Pets | `origin/Deuslano-Junior:src/GestaoCondominioNet10/Controllers/PetController.cs` | 2026-05-12 | Integrado em `GestaoPro.API/Controllers/PetController.cs` |
| Areas comuns | `origin/main:src/Etapa3/Controllers/CommonAreasController.cs` | 2026-05-10 | Integrado em `GestaoPro.API/Controllers/CommonAreasController.cs` |
| Reservas | `origin/main:src/Etapa3/Controllers/Reservations.cs` e `origin/main:src/GestaoPro.API/Controllers/ReservationsController.cs` | 2026-05-10 | Integrado com regras de horario/conflito |
| Usuarios/Login | `origin/main:src/CondoManagement/UserService/Controllers/UsersController.cs` e `AuthService` | 2026-05-10 | Separado em `UsersController` e `AuthController` |
| Ocorrencias | `origin/main:src/CondoManagement/OcurrenceService/Controllers/OccurrencesController.cs` | 2026-05-10 | Integrado em `OccurrencesController` |
| Condominios/Unidades | `origin/main:src/CondoManagement/UserService/Controllers` | 2026-05-10 | Integrado em `CondominiumsController` e `UnitsController` |
| Avisos | `origin/main:src/GestaoPro.API/Controllers/AnnouncementsController.cs` | 2026-05-10 | Integrado em `AnnouncementsController` |
| Estacionamento | `origin/main:src/ParkingService/Controllers/ParkingController.cs` | 2026-05-06 | Integrado em `ParkingController` |
| Visitantes | `origin/main:src/VisitorService/Controllers/VisitorController.cs` | 2026-05-06 | Integrado em `VisitorController` |
| Packages | `origin/main:src/PackageService/Controllers/PackageController.cs` | 2026-05-06 | Integrado em `PackageController` |
| Assembleias/Votos | `origin/reorganizacao-estrutura:src/AssemblyService/Controllers` | 2026-05-06 | Integrado em `AssemblyController` e `VoteController` |
| Financeiro | `origin/reorganizacao-estrutura:src/FinancialService/Controllers` | 2026-05-06 | Integrado em `BillsController` e `PaymentsController` |

## JavaScript verificado

Os arquivos JavaScript estaticos mais recentes da Etapa 3 foram encontrados em:

- `origin/main:src/Etapa3/wwwroot/condominio.js/area.js`
- `origin/main:src/Etapa3/wwwroot/condominio.js/cadastro.js`
- `origin/main:src/Etapa3/wwwroot/condominio.js/gerenciararea.js`
- `origin/main:src/Etapa3/wwwroot/condominio.js/gerenciareserva.js`
- `origin/main:src/Etapa3/wwwroot/condominio.js/login.js`
- `origin/main:src/Etapa3/wwwroot/condominio.js/reserva.js`

Na estrutura consolidada, essas telas foram migradas para o front React/Vite em `src/GestaoPro.Web/src/App.tsx`, que chama a API unica por `/api`.

## MongoDB

Todos os controllers ativos usam MongoDB por repositories/services registrados no `Program.cs`.

Foi adicionado o endpoint:

```text
GET /api/Health/mongodb
```

Resultado atual do teste local:

```json
{
  "connected": false,
  "database": "gestaopro",
  "message": "MongoDB Atlas não configurado. Atualize MongoDbSettings:ConnectionString com a string real."
}
```

Isso significa que a estrutura esta pronta para MongoDB, mas a aplicacao ainda nao conversa com um Atlas real enquanto `GestaoPro.API/appsettings.json` estiver usando:

```json
"ConnectionString": "mongodb+srv://usuario:senha@cluster.mongodb.net/"
```

## Program.cs

O `Program.cs` principal registra:

- Controllers
- Swagger
- CORS
- JWT
- SignalR
- MongoDB settings
- `IMongoClient`
- `IMongoDatabase`
- Repositories
- Services

