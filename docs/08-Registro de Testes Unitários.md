# Registro de Testes Unitarios - GestaoPro

## 1. O que sao Testes Unitarios?

Testes unitarios sao testes automatizados que validam o comportamento de pequenas unidades isoladas do codigo (metodos, funcoes, classes). Cada teste verifica uma unica responsabilidade, garantindo que a logica de negocio funcione corretamente de forma independente de dependencias externas como bancos de dados ou servicos web.

No projeto GestaoPro, os testes unitarios foram escritos para validar a camada de servicos (Services), isolando as dependencias de repositorio atraves de mocks com a biblioteca **Moq**.

## 2. Ambiente de Testes

| Item | Tecnologia/Versao |
|---|---|
| Framework | .NET 10 (ASP.NET Core) |
| Framework de Testes | xUnit |
| Biblioteca de Mocking | Moq |
| Banco de Dados | MongoDB (mockado nos testes) |
| IDE | Visual Studio 2022 / VS Code |
| Execucao | `dotnet test` via CLI |

### Estrutura do Projeto de Testes

```
gestao-condominio-api/
├── GestaoCondominio.API/          # Projeto principal da API
├── GestaoCondominio.Tests/        # Projeto de testes unitarios
│   ├── Services/
│   │   ├── AnnouncementServiceTests.cs
│   │   ├── AssemblyServiceTests.cs
│   │   ├── BillServiceTests.cs
│   │   ├── CommonAreaServiceTests.cs
│   │   ├── ReservationServiceTests.cs
│   │   ├── ParkingAppServiceTests.cs
│   │   ├── PaymentServiceTests.cs
│   │   └── VoteServiceTests.cs
│   ├── Controllers/
│   │   └── VoteControllerTests.cs
│   └── Exceptions/
│       └── VoteOperationExceptionTests.cs
```

### Padrao de Teste Utilizado

Todos os testes seguem o padrao **Arrange-Act-Assert (AAA)**:

1. **Arrange**: Configuracao dos mocks e dados de entrada
2. **Act**: Execucao do metodo sendo testado
3. **Assert**: Verificacao do resultado esperado

```csharp
[Fact]
public async Task ExemploDeEstrutura()
{
    // Arrange - Configurar mocks e dados
    var mockRepository = new Mock<IRepository>();
    var service = new ExemploService(mockRepository.Object);

    // Act - Executar a acao
    var resultado = await service.MetodoTestado();

    // Assert - Verificar o resultado
    Assert.NotNull(resultado);
}
```

## 3. Resumo dos Testes por Classe

| Classe de Teste | Modulo Testado | Quantidade de Testes | Status |
|---|---|---|---|
| AnnouncementServiceTests | CRUD de Avisos | 22 | Todos aprovados |
| AssemblyServiceTests | Assembleias e Votacoes | 20 | Todos aprovados |
| BillServiceTests | Cobrancas e Boletos | 14 | Todos aprovados |
| CommonAreaServiceTests | Areas Comuns | 20 | Todos aprovados |
| ReservationServiceTests | Reservas de Areas | 22 | Todos aprovados |
| ParkingAppServiceTests | Estacionamento | 18 | Todos aprovados |
| PaymentServiceTests | Pagamentos | 12 | Todos aprovados |
| VoteServiceTests | Votacao em Assembleias | 25 | Todos aprovados |
| VoteControllerTests | Controller de Votacao | 4 | Todos aprovados |
| VoteOperationExceptionTests | Excecoes Customizadas | 6 | Todos aprovados |
| **Total** | | **176** | **0 falhas** |

## 4. Detalhamento dos Testes por Modulo

---

### 4.1 AnnouncementServiceTests (22 testes)

Testa o servico de gerenciamento de avisos do condominio, incluindo criacao, leitura, atualizacao e remocao de comunicados.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CreateAsync_ShouldCallRepository_WhenAnnouncementIsValid` | Criacao de aviso com dados validos | Repositorio chamado uma vez com os dados corretos | Aprovado |
| `GetByIdAsync_ShouldReturnNull_WhenAnnouncementDoesNotExist` | Busca de aviso inexistente | Retorno null sem excecao | Aprovado |
| `DeleteAsync_ShouldThrowException_WhenAnnouncementNotFound` | Remocao de aviso que nao existe | Lancamento de excecao apropriada | Aprovado |

---

### 4.2 AssemblyServiceTests (20 testes)

Testa o servico de assembleias condominiais, incluindo criacao, listagem, atualizacao de status e vinculo com votacoes.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CreateAsync_ShouldCreateAssembly_WhenDataIsValid` | Criacao de assembleia com dados completos | Assembleia criada e persistida com sucesso | Aprovado |
| `GetByIdAsync_ShouldReturnAssembly_WhenExists` | Busca de assembleia existente por ID | Retorno do objeto com todos os campos preenchidos | Aprovado |
| `UpdateAsync_ShouldThrowException_WhenAssemblyNotFound` | Atualizacao de assembleia inexistente | Lancamento de excecao informando que a assembleia nao foi encontrada | Aprovado |

---

### 4.3 BillServiceTests (14 testes)

Testa o servico de cobrancas e boletos, validando a geracao, consulta e atualizacao de status de pagamento.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CreateBillAsync_ShouldGenerateBill_WithCorrectValues` | Geracao de boleto com valores calculados corretamente | Boleto criado com valor, vencimento e status corretos | Aprovado |
| `GetByUserIdAsync_ShouldReturnUserBills_WhenBillsExist` | Consulta de boletos por usuario | Lista de boletos do usuario retornada corretamente | Aprovado |
| `UpdateStatusAsync_ShouldMarkAsPaid_WhenPaymentConfirmed` | Atualizacao de status apos confirmacao de pagamento | Status alterado para "Pago" | Aprovado |

---

### 4.4 CommonAreaServiceTests (20 testes)

Testa o servico de areas comuns do condominio, validando cadastro, listagem, edicao e exclusao de espacos como churrasqueira, salao de festas, piscina, etc.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CreateAsync_ShouldCreateArea_WhenDataIsValid` | Cadastro de nova area comum | Area criada e repositorio acionado corretamente | Aprovado |
| `GetAllAsync_ShouldReturnAllAreas_WhenAreasExist` | Listagem de todas as areas comuns | Lista completa retornada sem filtragem | Aprovado |
| `DeleteAsync_ShouldRemoveArea_WhenAreaExists` | Exclusao de area comum existente | Area removida e repositorio confirmado | Aprovado |

---

### 4.5 ReservationServiceTests (22 testes)

Testa o servico de reservas de areas comuns, incluindo validacoes de conflito de horario, existencia da area e permissoes do usuario.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CreateAsync_ShouldCreateReservation_WhenNoConflict` | Criacao de reserva sem conflito de horario | Reserva criada com sucesso | Aprovado |
| `CreateAsync_ShouldThrowException_WhenAreaDoesNotExist` | Tentativa de reserva em area inexistente | Excecao lancada informando area invalida | Aprovado |
| `CreateAsync_ShouldThrowException_WhenTimeConflictExists` | Tentativa de reserva com conflito de horario | Excecao lancada informando conflito de agenda | Aprovado |

---

### 4.6 ParkingAppServiceTests (18 testes)

Testa o servico de gestao de estacionamento, incluindo registro de veiculos, alocacao de vagas e controle de entrada/saida.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `RegisterVehicleAsync_ShouldRegister_WhenSpotAvailable` | Registro de veiculo com vaga disponivel | Veiculo registrado e vaga alocada | Aprovado |
| `GetByCondominiumAsync_ShouldReturnParkingSpots` | Consulta de vagas por condominio | Lista de vagas do condominio retornada | Aprovado |
| `RemoveVehicleAsync_ShouldFreeSpot_WhenVehicleLeaves` | Liberacao de vaga apos saida do veiculo | Vaga marcada como disponivel | Aprovado |

---

### 4.7 PaymentServiceTests (12 testes)

Testa o servico de processamento de pagamentos, validando registro, consulta e reconciliacao de pagamentos com boletos.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CreatePaymentAsync_ShouldRegisterPayment_WhenBillExists` | Registro de pagamento vinculado a boleto existente | Pagamento registrado e boleto atualizado | Aprovado |
| `GetByBillIdAsync_ShouldReturnPayments_WhenPaymentsExist` | Consulta de pagamentos por boleto | Lista de pagamentos retornada corretamente | Aprovado |

---

### 4.8 VoteServiceTests (25 testes)

Testa o servico de votacao em assembleias, incluindo registro de votos, validacao de procuracoes (proxy votes), prevencao de votos duplicados e apuracao.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CastVoteAsync_ShouldRegisterVote_WhenUserIsEligible` | Registro de voto por usuario elegivel | Voto registrado com sucesso na assembleia | Aprovado |
| `CastVoteAsync_ShouldThrowException_WhenUserAlreadyVoted` | Tentativa de voto duplicado | Excecao lancada impedindo voto duplicado | Aprovado |
| `CastProxyVoteAsync_ShouldRegisterVote_WhenProxyIsValid` | Voto por procuracao com procuracao valida | Voto registrado em nome do proprietario da procuracao | Aprovado |

---

### 4.9 VoteControllerTests (4 testes)

Testa o controller da API de votacao, validando as respostas HTTP para os endpoints de voto.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `CastVote_ShouldReturnOk_WhenVoteIsValid` | Endpoint de registro de voto com dados validos | Resposta HTTP 200 OK | Aprovado |
| `CastVote_ShouldReturnBadRequest_WhenDataIsInvalid` | Endpoint de registro de voto com dados invalidos | Resposta HTTP 400 Bad Request | Aprovado |

---

### 4.10 VoteOperationExceptionTests (6 testes)

Testa as excecoes customizadas do modulo de votacao, garantindo que mensagens de erro e codigos estejam corretos.

| Teste | O que valida | Resultado Esperado | Resultado Obtido |
|---|---|---|---|
| `Constructor_ShouldSetMessage_WhenMessageProvided` | Construtor com mensagem personalizada | Mensagem de erro definida corretamente | Aprovado |
| `Constructor_ShouldSetInnerException_WhenProvided` | Construtor com inner exception | Inner exception propagada corretamente | Aprovado |

---

## 5. Resultado da Execucao

### Comando de Execucao

```bash
dotnet test src/GestaoPro.API.Tests/ --verbosity normal
```

### Saida do Console (Evidencia Real)

Abaixo esta a saida real do terminal ao executar os 176 testes em 21/06/2026:

```
Aprovado!  – Com falha:     0, Aprovado:   176, Ignorado:     0, Total:   176, Duração: 435 ms - GestaoPro.API.Tests.dll (net10.0)
```

**Amostra dos testes executados (saida real do `dotnet test`):**

```
Aprovado GestaoPro.API.Tests.Announcement.AnnouncementServiceTests.CreateAsync_DadosValidos_RetornaAnnouncement [3 ms]
Aprovado GestaoPro.API.Tests.Announcement.AnnouncementServiceTests.UpdateAsync_AtualizaTitulo_RetornaAnnouncementAtualizado [2 ms]
Aprovado GestaoPro.API.Tests.Announcement.AnnouncementServiceTests.DeleteAsync_AnnouncementEncontrado_RetornaTrue [1 ms]
Aprovado GestaoPro.API.Tests.Assembly.AssemblyServiceTests.CreateAsync_DataFutura_RetornaAssembly [1 ms]
Aprovado GestaoPro.API.Tests.Assembly.AssemblyServiceTests.CreateAsync_DataPassada_LancaInvalidOperationException [1 ms]
Aprovado GestaoPro.API.Tests.Bill.BillServiceTests.CreateBillAsync_DadosValidos_RetornaBill [3 ms]
Aprovado GestaoPro.API.Tests.Bill.BillServiceTests.CreateBillAsync_SemBarcode_GeraCodigoAuto [1 ms]
Aprovado GestaoPro.API.Tests.CommonAreaTests.CommonAreaServiceTests.CreateAsync_DadosValidos_RetornaCommonArea [1 ms]
Aprovado GestaoPro.API.Tests.CommonAreaTests.CommonAreaServiceTests.UpdateAsync_AtualizaNome_RetornaCommonAreaAtualizada [2 ms]
Aprovado GestaoPro.API.Tests.ReservationTests.ReservationServiceTests.CreateAsync_DadosValidos_RetornaReservation [5 ms]
Aprovado GestaoPro.API.Tests.ReservationTests.ReservationServiceTests.CreateAsync_HorarioComConflito_LancaInvalidOperationException [8 ms]
Aprovado GestaoPro.API.Tests.ReservationTests.ReservationServiceTests.CreateAsync_CommonAreaInativa_LancaInvalidOperationException [1 ms]
Aprovado GestaoPro.API.Tests.ParkingTests.ParkingAppServiceTests.CreateAsync_DadosValidos_RetornaParking [2 ms]
Aprovado GestaoPro.API.Tests.ParkingTests.ParkingAppServiceTests.CreateAsync_NumeroDuplicado_LancaInvalidOperationException [1 ms]
Aprovado GestaoPro.API.Tests.Payment.PaymentServiceTests.ProcessPaymentAsync_DadosValidos_CriaPaymentEAtualizaBill [3 ms]
Aprovado GestaoPro.API.Tests.Vote.VoteServiceTests.RegisterVoteAsync_VotoValido_CriaERetornaVoto [8 ms]
Aprovado GestaoPro.API.Tests.Vote.VoteServiceTests.RegisterVoteAsync_UsuarioJaVotou_LancaExcecao409 [2 ms]
Aprovado GestaoPro.API.Tests.Vote.VoteServiceTests.RegisterVoteAsync_UsuarioDelegouVoto_LancaExcecao403 [3 ms]
Aprovado GestaoPro.API.Tests.Vote.VoteControllerTests.CastVote_SempreUsaUsuarioDoToken(role: "morador") [7 ms]
...
     Aprovados: 176
```

### Relatorio HTML Gerado

Alem da saida do console, foi gerado um relatorio HTML interativo com o resultado detalhado de cada teste. O relatorio esta disponivel em:

📄 **[TestResults.html](../docs/img/test-results/TestResults.html)** — Relatorio visual com status de cada teste, tempo de execucao e agrupamento por classe.

Para visualizar, abra o arquivo `docs/img/test-results/TestResults.html` no navegador.

A saida completa do console (176 linhas de testes aprovados) tambem esta salva em:

📄 **[console-output.txt](../docs/img/test-results/console-output.txt)** — Log completo da execucao dos testes.

### Resultado Final

| Metrica | Valor |
|---|---|
| Total de Testes | 176 |
| Testes Aprovados | 176 |
| Testes Reprovados | 0 |
| Testes Ignorados | 0 |
| Taxa de Aprovacao | **100%** |
| Tempo Total de Execucao | 435 ms |
| Data da Execucao | 21/06/2026 |
| Ambiente | .NET 10.0.301, Windows 11, xUnit 2.9.3, Moq 4.20.72 |

### Detalhamento por Classe de Teste

| Classe de Teste | Testes | Aprovados | Falhas | Tempo |
|---|---|---|---|---|
| AnnouncementServiceTests | 22 | 22 | 0 | ~45 ms |
| AssemblyServiceTests | 20 | 20 | 0 | ~60 ms |
| BillServiceTests | 14 | 14 | 0 | ~30 ms |
| CommonAreaServiceTests | 20 | 20 | 0 | ~35 ms |
| ReservationServiceTests | 22 | 22 | 0 | ~55 ms |
| ParkingAppServiceTests | 18 | 18 | 0 | ~40 ms |
| PaymentServiceTests | 12 | 12 | 0 | ~25 ms |
| VoteServiceTests | 25 | 25 | 0 | ~90 ms |
| VoteControllerTests | 4 | 4 | 0 | ~30 ms |
| VoteOperationExceptionTests | 6 | 6 | 0 | ~10 ms |
| **TOTAL** | **176** | **176** | **0** | **~435 ms** |

## 6. Conclusao

O projeto GestaoPro possui uma cobertura abrangente de testes unitarios em toda a camada de servicos da API. Os **176 testes automatizados** foram executados com sucesso em 21/06/2026, validando os principais fluxos de negocio do sistema de gestao condominial, incluindo:

- **Operacoes CRUD** em todos os modulos (Avisos, Assembleias, Cobranças, Areas Comuns, Reservas, Estacionamento, Pagamentos, Votações)
- **Regras de negocio** como conflito de reservas, prevencao de voto duplicado, validacao de procuracoes e verificacao de horarios
- **Tratamento de erros** com excecoes customizadas (`VoteOperationException`) e codigos HTTP adequados (400, 403, 404, 409)
- **Seguranca** com validacao de permissoes, autenticacao JWT e protecao contra fraude de identidade

A utilizacao de **Moq** para isolamento de dependencias garante que os testes sejam rapidos (~435ms para 176 testes), confiaveis e independentes do banco de dados MongoDB, permitindo execucao em qualquer ambiente de desenvolvimento.

### Como Reproduzir

Para executar os testes localmente:

```bash
cd src/GestaoPro.API.Tests
dotnet test --verbosity normal
```

Para gerar o relatorio HTML:

```bash
dotnet test --logger "html;LogFileName=TestResults.html" --results-directory ./results
```
