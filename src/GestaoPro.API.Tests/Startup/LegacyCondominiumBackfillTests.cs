using GestaoPro.API.Tests.Access;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using static GestaoPro.API.Tests.Access.TestSession;

namespace GestaoPro.API.Tests.Startup;

// Visitantes e vagas antigos não têm condominiumId e a administração recebe 404 ao editá-los.
// O preenchimento deriva o condomínio do morador (ou de quem reservou, ou da unidade) e grava só esse campo,
// e só em quem continua sem condomínio (UpdateOne com filtro), registro a registro.
public class LegacyCondominiumBackfillTests
{
    private readonly Mock<ILegacyCondominiumStore> _store = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly Mock<IUnitService> _units = new();
    private readonly Mock<ICondominiumService> _condominiums = new();

    private static readonly BsonValue VisitanteAntigo = new BsonObjectId(ObjectId.Parse("665f1c2e9b1e8a0000000e01"));
    private static readonly BsonValue VisitanteIdLegado = new BsonString("visitante-legado-123");
    private static readonly BsonValue VisitanteOrfao = new BsonObjectId(ObjectId.Parse("665f1c2e9b1e8a0000000e02"));

    public LegacyCondominiumBackfillTests()
    {
        _store.Setup(s => s.GetVisitorsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyVisitor>());
        _store.Setup(s => s.GetParkingsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyParking>());
        _store.Setup(s => s.SetVisitorCondominiumAsync(It.IsAny<BsonValue>(), It.IsAny<string>())).ReturnsAsync(true);
        _store.Setup(s => s.SetParkingCondominiumAsync(It.IsAny<BsonValue>(), It.IsAny<string>())).ReturnsAsync(true);
        _users.Setup(u => u.GetByIdAsync("joao")).ReturnsAsync(new UserResponse { Id = "joao", CondominiumId = CondominioA });
        _users.Setup(u => u.GetByIdAsync("carlos")).ReturnsAsync(new UserResponse { Id = "carlos", CondominiumId = CondominioB });
        _units.Setup(u => u.GetByIdAsync("unidade-b"))
            .ReturnsAsync(new UnitResponse("unidade-b", CondominioB, "901", null, 9, default, new List<string>(), 0, null, null, DateTime.UtcNow));
        CondominiumsInSystem(CondominioA, CondominioB);
    }

    private LegacyCondominiumBackfill Backfill() => new(
        _store.Object, _users.Object, _units.Object, _condominiums.Object,
        NullLogger<LegacyCondominiumBackfill>.Instance);

    private void CondominiumsInSystem(params string[] ids) =>
        _condominiums.Setup(c => c.GetAllAsync())
            .ReturnsAsync(ids.Select(id => new CondominiumResponse { Id = id }).ToList());

    [Fact]
    public async Task Visitante_sem_condominio_recebe_o_do_morador_e_o_sem_morador_fica_sem()
    {
        _store.Setup(s => s.GetVisitorsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyVisitor>
        {
            new(VisitanteAntigo, "joao"),
            new(VisitanteOrfao, "sumiu"),
        });

        var result = await Backfill().RunAsync();

        _store.Verify(s => s.SetVisitorCondominiumAsync(VisitanteAntigo, CondominioA), Times.Once);
        _store.Verify(s => s.SetVisitorCondominiumAsync(VisitanteOrfao, It.IsAny<string>()), Times.Never);
        Assert.Equal(1, result.VisitorsUpdated);
        Assert.Equal(1, result.VisitorsLeft);
    }

    // Se outro processo preencheu o campo entre a leitura e a escrita, o filtro do UpdateOne não casa: nada muda.
    [Fact]
    public async Task Registro_preenchido_por_outro_processo_nao_conta_como_atualizado()
    {
        _store.Setup(s => s.GetVisitorsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyVisitor> { new(VisitanteAntigo, "joao") });
        _store.Setup(s => s.SetVisitorCondominiumAsync(VisitanteAntigo, CondominioA)).ReturnsAsync(false);

        var result = await Backfill().RunAsync();

        Assert.Equal(0, result.VisitorsUpdated);
        Assert.Equal(0, result.VisitorsLeft);
    }

    // Um registro com problema (id legado que não é ObjectId, erro do banco) não interrompe os demais.
    [Fact]
    public async Task Falha_em_um_registro_nao_interrompe_os_outros()
    {
        _store.Setup(s => s.GetVisitorsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyVisitor>
        {
            new(VisitanteIdLegado, "joao"),
            new(VisitanteAntigo, "carlos"),
        });
        _store.Setup(s => s.SetVisitorCondominiumAsync(VisitanteIdLegado, It.IsAny<string>()))
            .ThrowsAsync(new FormatException("id legado"));

        var result = await Backfill().RunAsync();

        _store.Verify(s => s.SetVisitorCondominiumAsync(VisitanteAntigo, CondominioB), Times.Once);
        Assert.Equal(1, result.VisitorsUpdated);
        Assert.Equal(1, result.VisitorsLeft);
    }

    [Fact]
    public async Task Vaga_sem_condominio_deriva_do_morador_de_quem_reservou_ou_da_unidade()
    {
        var porMorador = new BsonString("p-morador");
        var porReserva = new BsonString("p-reserva");
        var porUnidade = new BsonString("p-unidade");
        _store.Setup(s => s.GetParkingsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyParking>
        {
            new(porMorador, "joao", null, null),
            new(porReserva, null, "carlos", null),
            new(porUnidade, null, null, "unidade-b"),
        });

        var result = await Backfill().RunAsync();

        _store.Verify(s => s.SetParkingCondominiumAsync(porMorador, CondominioA), Times.Once);
        _store.Verify(s => s.SetParkingCondominiumAsync(porReserva, CondominioB), Times.Once);
        _store.Verify(s => s.SetParkingCondominiumAsync(porUnidade, CondominioB), Times.Once);
        Assert.Equal(3, result.ParkingsUpdated);
        Assert.Equal(0, result.ParkingsLeft);
    }

    [Fact]
    public async Task Vaga_sem_vinculo_vai_para_o_unico_condominio_do_sistema()
    {
        CondominiumsInSystem(CondominioA);
        var solta = new BsonString("p-solta");
        _store.Setup(s => s.GetParkingsWithoutCondominiumAsync()).ReturnsAsync(new List<LegacyParking> { new(solta, null, null, null) });

        var result = await Backfill().RunAsync();

        _store.Verify(s => s.SetParkingCondominiumAsync(solta, CondominioA), Times.Once);
        Assert.Equal(1, result.ParkingsUpdated);
    }

    [Fact]
    public async Task Vaga_sem_vinculo_com_varios_condominios_fica_sem_e_e_contada()
    {
        _store.Setup(s => s.GetParkingsWithoutCondominiumAsync())
            .ReturnsAsync(new List<LegacyParking> { new(new BsonString("p-solta"), null, null, null) });

        var result = await Backfill().RunAsync();

        _store.Verify(s => s.SetParkingCondominiumAsync(It.IsAny<BsonValue>(), It.IsAny<string>()), Times.Never);
        Assert.Equal(0, result.ParkingsUpdated);
        Assert.Equal(1, result.ParkingsLeft);
    }

    // Filtro do UpdateOne: o _id e condominiumId ausente, nulo ou vazio ($in com null casa campo ausente).
    [Fact]
    public void Filtro_de_escrita_so_casa_registro_ainda_sem_condominio()
    {
        var filter = MongoLegacyCondominiumStore.WithoutCondominium(VisitanteIdLegado);

        Assert.Equal(VisitanteIdLegado, filter["_id"]);
        var accepted = filter["condominiumId"]["$in"].AsBsonArray;
        Assert.Contains(BsonNull.Value, accepted);
        // Vazio ou só com espaços conta como sem condomínio.
        var blank = Assert.Single(accepted.OfType<BsonRegularExpression>());
        Assert.Matches(blank.Pattern, "");
        Assert.Matches(blank.Pattern, "   ");
        Assert.DoesNotMatch(blank.Pattern, "665f1c2e9b1e8a00000000a1");
    }

    [Fact]
    public void Condominio_gravado_como_ObjectId_quando_valido()
    {
        Assert.IsType<BsonObjectId>(MongoLegacyCondominiumStore.CondominiumValue(CondominioA));
        Assert.IsType<BsonString>(MongoLegacyCondominiumStore.CondominiumValue("cond-legado"));
    }

    // Roda na partida em segundo plano: banco fora do ar só gera log, a API continua de pé.
    [Fact]
    public async Task Falha_no_preenchimento_nao_derruba_a_API()
    {
        _store.Setup(s => s.GetVisitorsWithoutCondominiumAsync()).ThrowsAsync(new TimeoutException("Mongo fora do ar"));
        var services = new ServiceCollection()
            .AddScoped(_ => Backfill())
            .BuildServiceProvider();
        var initializer = new LegacyCondominiumBackfillInitializer(
            services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<LegacyCondominiumBackfillInitializer>.Instance);

        await initializer.StartAsync(CancellationToken.None);
        var exception = await Record.ExceptionAsync(() => initializer.ExecuteTask!);
        await initializer.StopAsync(CancellationToken.None);

        Assert.Null(exception);
        _store.Verify(s => s.GetVisitorsWithoutCondominiumAsync(), Times.Once);
    }

    // Registrado na partida: o serviço de fundo existe e o preenchimento está no contêiner.
    // MongoDB inalcançável e content root temporário, como nos outros testes de partida.
    [Fact]
    public async Task Preenchimento_e_registrado_como_servico_de_partida()
    {
        var contentRoot = Directory.CreateTempSubdirectory("gestaopro-teste-").FullName;
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseContentRoot(contentRoot);
                builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500");
                builder.UseSetting("MongoDbSettings:DatabaseName", "gestaopro-teste");
                builder.UseSetting("JwtSettings:SecretKey", "chave-de-teste-com-mais-de-32-caracteres-0123456789");
                // O VoteIndexInitializer roda dentro da partida e não é o alvo aqui (removido como nos outros testes).
                builder.ConfigureTestServices(services => services.Remove(services.Single(d =>
                    d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(VoteIndexInitializer))));
            });

            Assert.Single(factory.Services.GetServices<IHostedService>().OfType<LegacyCondominiumBackfillInitializer>());
            // Os repositórios reais acessam o banco ao serem criados: confere só o registro, sem instanciar.
            var isService = factory.Services.GetRequiredService<IServiceProviderIsService>();
            Assert.True(isService.IsService(typeof(LegacyCondominiumBackfill)));
            Assert.True(isService.IsService(typeof(ILegacyCondominiumStore)));
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
