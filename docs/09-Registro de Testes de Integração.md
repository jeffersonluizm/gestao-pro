# Registro de Testes de Integracao - GestaoPro

## 1. O que sao Testes de Integracao?

Testes de integracao sao testes automatizados que verificam se diferentes modulos ou componentes de um sistema funcionam corretamente quando integrados. Ao contrario dos testes unitarios, que validam unidades isoladas, os testes de integracao focam na **interacao entre camadas**, como a comunicacao entre servicos e repositorios, a validacao cruzada de dados entre colecoes do banco e o fluxo completo de uma operacao de negocio que envolve multiplas dependencias.

No contexto do GestaoPro, a estrategia de testes de integracao foi implementada em dois niveis:

1. **Testes automatizados com mocks**: Validam a integracao logica entre servicos e repositorios utilizando Moq, garantindo que os servicos orquestrem corretamente as chamadas aos repositorios e apliquem as regras de negocio que dependem de dados de multiplas colecoes.

2. **Testes manuais via Swagger**: Validam a integracao completa end-to-end da API com o banco de dados MongoDB Atlas real, verificando persistencia, autenticacao JWT e respostas HTTP.

## 2. Ambiente de Testes de Integracao

| Item | Tecnologia |
|---|---|
| Framework | .NET 10 (ASP.NET Core) |
| Framework de Testes | xUnit |
| Mocking | Moq |
| Banco de Dados (producao) | MongoDB Atlas |
| Testes Manuais | Swagger UI / Postman |
| Autenticacao | JWT Bearer Token |

## 3. Estrategia de Integracao com Mocks

Os testes do GestaoPro utilizam a biblioteca **Moq** para simular os repositorios (camada de acesso a dados). Embora os repositorios sejam mockados, os testes validam a **integracao logica** entre:

- **Servico e multiplos repositorios**: Um servico que precisa consultar dados de diferentes colecoes para tomar decisoes
- **Validacoes cruzadas**: Regras de negocio que dependem da existencia ou estado de entidades em outras colecoes
- **Orquestracao de operacoes**: Fluxos que envolvem leitura, validacao e escrita em sequencia

### Padrao de Teste de Integracao com Mocks

```csharp
[Fact]
public async Task OperacaoIntegrada_DeveValidarDependencias_AntesDeExecutar()
{
    // Arrange - Configurar multiplos repositorios mockados
    var mockRepoA = new Mock<IRepositoryA>();
    var mockRepoB = new Mock<IRepositoryB>();
    
    mockRepoA.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
             .ReturnsAsync(new EntidadeA { Id = "123", Status = "Ativo" });
    
    mockRepoB.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
             .ReturnsAsync((EntidadeB)null); // Simula ausencia
    
    var service = new ServicoIntegrado(mockRepoA.Object, mockRepoB.Object);

    // Act & Assert - Verificar que a integracao entre os modulos funciona
    var exception = await Assert.ThrowsAsync<BusinessException>(
        () => service.ExecutarOperacao("123", "456")
    );
    
    Assert.Contains("dependencia nao encontrada", exception.Message);
}
```

## 4. Cenarios de Integracao Testados

---

### 4.1 ReservationService - Integracao com CommonAreaRepository

O servico de reservas valida a existencia e disponibilidade da area comum antes de permitir a criacao de uma reserva. Esta integracao envolve a consulta ao repositorio de areas comuns e ao repositorio de reservas existentes.

**Fluxo de integracao testado:**

```
Usuario solicita reserva
    -> ReservationService recebe a requisicao
        -> Consulta CommonAreaRepository (area existe?)
            -> Consulta ReservationRepository (conflito de horario?)
                -> Cria a reserva ou rejeita com erro
```

#### Cenarios validados:

| Cenario | Repositorios Envolvidos | O que valida | Resultado |
|---|---|---|---|
| Area comum existe e nao ha conflito | CommonAreaRepository + ReservationRepository | Reserva criada com sucesso apos validacoes cruzadas | Aprovado |
| Area comum nao existe | CommonAreaRepository | Excecao lancada antes de verificar conflitos | Aprovado |
| Area existe mas ha conflito de horario | CommonAreaRepository + ReservationRepository | Excecao lancada informando indisponibilidade | Aprovado |
| Area existe mas esta inativa/em manutencao | CommonAreaRepository | Excecao lancada informando area indisponivel | Aprovado |

**Exemplo de teste:**

```csharp
[Fact]
public async Task CreateReservation_ShouldValidateCommonAreaExists_BeforeCreating()
{
    // Arrange
    var mockCommonAreaRepo = new Mock<ICommonAreaRepository>();
    var mockReservationRepo = new Mock<IReservationRepository>();
    
    // Simula area comum inexistente
    mockCommonAreaRepo.Setup(r => r.GetByIdAsync("area-inexistente"))
                      .ReturnsAsync((CommonArea)null);
    
    var service = new ReservationService(
        mockReservationRepo.Object, 
        mockCommonAreaRepo.Object
    );

    var novaReserva = new Reservation
    {
        CommonAreaId = "area-inexistente",
        UserId = "user-123",
        Date = DateTime.Now.AddDays(1)
    };

    // Act & Assert
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => service.CreateAsync(novaReserva)
    );

    // Verifica que o repositorio de reservas NAO foi chamado
    mockReservationRepo.Verify(
        r => r.CreateAsync(It.IsAny<Reservation>()), 
        Times.Never
    );
}
```

Este teste demonstra a integracao entre os servicos: o `ReservationService` consulta o `CommonAreaRepository` **antes** de tentar criar a reserva, e interrompe o fluxo caso a area nao exista, sem acionar o `ReservationRepository`.

---

### 4.2 VoteService - Integracao com Assembly, User e ProxyVote Repositories

O servico de votacao e o cenario de integracao mais complexo do sistema, pois envolve a coordenacao de **tres repositorios diferentes** para uma unica operacao:

**Fluxo de integracao testado:**

```
Usuario solicita registro de voto
    -> VoteService recebe a requisicao
        -> Consulta AssemblyRepository (assembleia existe e esta aberta?)
            -> Consulta UserRepository (usuario e elegivel?)
                -> Consulta VoteRepository (usuario ja votou?)
                    -> [Se proxy] Consulta ProxyVoteRepository (procuracao valida?)
                        -> Registra o voto ou rejeita com erro
```

#### Cenarios validados:

| Cenario | Repositorios Envolvidos | O que valida | Resultado |
|---|---|---|---|
| Voto direto valido | AssemblyRepo + UserRepo + VoteRepo | Voto registrado apos todas as validacoes | Aprovado |
| Assembleia nao encontrada | AssemblyRepository | Excecao lancada na primeira validacao | Aprovado |
| Assembleia encerrada | AssemblyRepository | Excecao informando que a votacao esta fechada | Aprovado |
| Usuario ja votou | AssemblyRepo + UserRepo + VoteRepo | Excecao impedindo voto duplicado | Aprovado |
| Voto por procuracao valida | AssemblyRepo + UserRepo + VoteRepo + ProxyVoteRepo | Voto registrado em nome do procurador | Aprovado |
| Voto por procuracao invalida | AssemblyRepo + ProxyVoteRepo | Excecao informando procuracao invalida | Aprovado |

**Exemplo de teste:**

```csharp
[Fact]
public async Task CastVote_ShouldValidateAllDependencies_BeforeRegistering()
{
    // Arrange - Configurar todos os repositorios envolvidos
    var mockAssemblyRepo = new Mock<IAssemblyRepository>();
    var mockUserRepo = new Mock<IUserRepository>();
    var mockVoteRepo = new Mock<IVoteRepository>();
    var mockProxyRepo = new Mock<IProxyVoteRepository>();

    // Assembleia existe e esta aberta
    mockAssemblyRepo.Setup(r => r.GetByIdAsync("assembly-01"))
                    .ReturnsAsync(new Assembly 
                    { 
                        Id = "assembly-01", 
                        Status = "Aberta" 
                    });

    // Usuario existe e e elegivel
    mockUserRepo.Setup(r => r.GetByIdAsync("user-01"))
                .ReturnsAsync(new User 
                { 
                    Id = "user-01", 
                    Role = "Proprietario" 
                });

    // Usuario ainda nao votou
    mockVoteRepo.Setup(r => r.GetByUserAndAssemblyAsync("user-01", "assembly-01"))
                .ReturnsAsync((Vote)null);

    var service = new VoteService(
        mockAssemblyRepo.Object,
        mockUserRepo.Object,
        mockVoteRepo.Object,
        mockProxyRepo.Object
    );

    var voto = new Vote
    {
        AssemblyId = "assembly-01",
        UserId = "user-01",
        Option = "Favoravel"
    };

    // Act
    await service.CastVoteAsync(voto);

    // Assert - Verificar que todas as validacoes foram executadas em ordem
    mockAssemblyRepo.Verify(r => r.GetByIdAsync("assembly-01"), Times.Once);
    mockUserRepo.Verify(r => r.GetByIdAsync("user-01"), Times.Once);
    mockVoteRepo.Verify(r => r.GetByUserAndAssemblyAsync("user-01", "assembly-01"), Times.Once);
    mockVoteRepo.Verify(r => r.CreateAsync(It.IsAny<Vote>()), Times.Once);
}
```

---

### 4.3 Outros Cenarios de Integracao Validados

| Modulo | Integracao Testada | Descricao |
|---|---|---|
| BillService + PaymentService | Boleto e Pagamento | Ao registrar pagamento, o servico valida a existencia do boleto e atualiza o status |
| ParkingAppService + UserService | Estacionamento e Usuario | Registro de veiculo valida que o usuario pertence ao condominio |
| AnnouncementService + CondominiumService | Avisos e Condominio | Criacao de aviso valida que o condominio existe |

## 5. Testes de Integracao Manuais (End-to-End)

Alem dos testes automatizados, foram realizados testes de integracao manuais completos utilizando o **Swagger UI** integrado a API e o **Postman** para cenarios mais complexos.

### Fluxo de Teste Manual

1. **Autenticacao**: Login via endpoint `/api/auth/login` para obter token JWT
2. **Autorizacao**: Uso do token Bearer nos endpoints protegidos
3. **Operacoes CRUD**: Teste de criacao, leitura, atualizacao e exclusao em cada modulo
4. **Validacoes de Negocio**: Teste de cenarios de erro (conflito de reserva, voto duplicado, etc.)
5. **Persistencia**: Verificacao direta no MongoDB Atlas de que os dados foram gravados corretamente

### Endpoints Testados Manualmente

| Metodo | Endpoint | Cenario | Resultado |
|---|---|---|---|
| POST | `/api/auth/login` | Login com credenciais validas | 200 OK + Token JWT |
| POST | `/api/auth/login` | Login com credenciais invalidas | 401 Unauthorized |
| GET | `/api/reservations` | Listagem de reservas com autenticacao | 200 OK + Lista |
| POST | `/api/reservations` | Criacao de reserva valida | 201 Created |
| POST | `/api/reservations` | Reserva com conflito de horario | 400 Bad Request |
| POST | `/api/votes` | Registro de voto em assembleia aberta | 200 OK |
| POST | `/api/votes` | Voto duplicado na mesma assembleia | 400 Bad Request |
| GET | `/api/commonareas` | Listagem de areas comuns | 200 OK + Lista |
| POST | `/api/announcements` | Criacao de aviso pelo sindico | 201 Created |
| GET | `/api/bills/{userId}` | Consulta de boletos do usuario | 200 OK + Lista |

## 6. Conclusao

A estrategia de testes de integracao do GestaoPro combina dois niveis complementares:

- **Testes automatizados com mocks (176 testes)**: Garantem que a logica de integracao entre servicos e repositorios esta correta, com execucao rapida e deterministica.
- **Testes manuais via Swagger/Postman**: Validam a integracao completa end-to-end com o banco de dados MongoDB Atlas real, autenticacao JWT e respostas HTTP.

Os cenarios mais criticos de integracao - como a validacao cruzada entre reservas e areas comuns, e a orquestracao de votacao envolvendo assembleias, usuarios e procuracoes - estao cobertos tanto por testes automatizados quanto por validacao manual, garantindo a confiabilidade do sistema em ambiente de producao.
