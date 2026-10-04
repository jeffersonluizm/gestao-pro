using GestaoPro.API.Tests.Access;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using static GestaoPro.API.Tests.Access.TestSession;
using ParkingModel = GestaoPro.API.Models.Parking;
using VisitorModel = GestaoPro.API.Models.Visitor;

namespace GestaoPro.API.Tests.Startup;

// Visitantes e vagas antigos não têm condominiumId e a administração recebe 404 ao editá-los.
// O preenchimento deriva o condomínio do morador (ou de quem reservou, ou da unidade) e só mexe em quem não tem.
public class LegacyCondominiumBackfillTests
{
    private readonly Mock<IVisitorRepository> _visitors = new();
    private readonly Mock<IParkingRepository> _parkings = new();
    private readonly Mock<IUserDirectory> _users = new();
    private readonly Mock<IUnitService> _units = new();
    private readonly Mock<ICondominiumService> _condominiums = new();

    public LegacyCondominiumBackfillTests()
    {
        _visitors.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VisitorModel>());
        _parkings.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>());
        _users.Setup(u => u.GetByIdAsync("joao")).ReturnsAsync(new UserResponse { Id = "joao", CondominiumId = CondominioA });
        _users.Setup(u => u.GetByIdAsync("carlos")).ReturnsAsync(new UserResponse { Id = "carlos", CondominiumId = CondominioB });
        _units.Setup(u => u.GetByIdAsync("unidade-b"))
            .ReturnsAsync(new UnitResponse("unidade-b", CondominioB, "901", null, 9, default, new List<string>(), 0, null, null, DateTime.UtcNow));
        CondominiumsInSystem(CondominioA, CondominioB);
    }

    private LegacyCondominiumBackfill Backfill() => new(
        _visitors.Object, _parkings.Object, _users.Object, _units.Object, _condominiums.Object,
        NullLogger<LegacyCondominiumBackfill>.Instance);

    private void CondominiumsInSystem(params string[] ids) =>
        _condominiums.Setup(c => c.GetAllAsync())
            .ReturnsAsync(ids.Select(id => new CondominiumResponse { Id = id }).ToList());

    [Fact]
    public async Task Visitante_sem_condominio_recebe_o_do_morador_e_os_demais_ficam_intactos()
    {
        _visitors.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<VisitorModel>
        {
            new() { Id = "v-antigo", ResidentId = "joao", CondominiumId = null },
            new() { Id = "v-ok", ResidentId = "carlos", CondominiumId = CondominioB },
            new() { Id = "v-orfao", ResidentId = "sumiu", CondominiumId = "" },
        });

        var result = await Backfill().RunAsync();

        _visitors.Verify(r => r.UpdateAsync("v-antigo", It.Is<VisitorModel>(v => v.CondominiumId == CondominioA)), Times.Once);
        _visitors.Verify(r => r.UpdateAsync("v-ok", It.IsAny<VisitorModel>()), Times.Never);
        _visitors.Verify(r => r.UpdateAsync("v-orfao", It.IsAny<VisitorModel>()), Times.Never);
        Assert.Equal(1, result.VisitorsUpdated);
        Assert.Equal(1, result.VisitorsLeft);
    }

    [Fact]
    public async Task Vaga_sem_condominio_deriva_do_morador_de_quem_reservou_ou_da_unidade()
    {
        _parkings.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel>
        {
            new() { Id = "p-morador", Number = "1", ResidentId = "joao" },
            new() { Id = "p-reserva", Number = "2", ReservedByUserId = "carlos" },
            new() { Id = "p-unidade", Number = "3", UnitId = "unidade-b" },
            new() { Id = "p-ok", Number = "4", CondominiumId = CondominioA, ResidentId = "carlos" },
        });

        var result = await Backfill().RunAsync();

        _parkings.Verify(r => r.UpdateAsync("p-morador", It.Is<ParkingModel>(p => p.CondominiumId == CondominioA)), Times.Once);
        _parkings.Verify(r => r.UpdateAsync("p-reserva", It.Is<ParkingModel>(p => p.CondominiumId == CondominioB)), Times.Once);
        _parkings.Verify(r => r.UpdateAsync("p-unidade", It.Is<ParkingModel>(p => p.CondominiumId == CondominioB)), Times.Once);
        _parkings.Verify(r => r.UpdateAsync("p-ok", It.IsAny<ParkingModel>()), Times.Never);
        Assert.Equal(3, result.ParkingsUpdated);
        Assert.Equal(0, result.ParkingsLeft);
    }

    [Fact]
    public async Task Vaga_sem_vinculo_vai_para_o_unico_condominio_do_sistema()
    {
        CondominiumsInSystem(CondominioA);
        _parkings.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { new() { Id = "p-solta", Number = "9" } });

        var result = await Backfill().RunAsync();

        _parkings.Verify(r => r.UpdateAsync("p-solta", It.Is<ParkingModel>(p => p.CondominiumId == CondominioA)), Times.Once);
        Assert.Equal(1, result.ParkingsUpdated);
    }

    [Fact]
    public async Task Vaga_sem_vinculo_com_varios_condominios_fica_sem_e_e_contada()
    {
        _parkings.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ParkingModel> { new() { Id = "p-solta", Number = "9" } });

        var result = await Backfill().RunAsync();

        _parkings.Verify(r => r.UpdateAsync(It.IsAny<string>(), It.IsAny<ParkingModel>()), Times.Never);
        Assert.Equal(0, result.ParkingsUpdated);
        Assert.Equal(1, result.ParkingsLeft);
    }

    // Roda na partida em segundo plano: banco fora do ar só gera log, a API continua de pé.
    [Fact]
    public async Task Falha_no_preenchimento_nao_derruba_a_API()
    {
        _visitors.Setup(r => r.GetAllAsync()).ThrowsAsync(new TimeoutException("Mongo fora do ar"));
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
        _visitors.Verify(r => r.GetAllAsync(), Times.Once);
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
            Assert.True(factory.Services.GetRequiredService<IServiceProviderIsService>().IsService(typeof(LegacyCondominiumBackfill)));
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }
}
