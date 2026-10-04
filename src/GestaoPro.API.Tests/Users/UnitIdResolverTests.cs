using static GestaoPro.API.Tests.Access.TestSession;
using UnitModel = GestaoPro.API.Models.Unit;

namespace GestaoPro.API.Tests.Users;

// Cadastro e edição de usuário aceitam a unidade por id ou por rótulo ("201", "A 201").
// O id de unidade de outro condomínio era aceito e o morador entrava na unidade de lá.
public class UnitIdResolverTests
{
    private const string UnidadeA = "665f1c2e9b1e8a0000000a11";
    private const string UnidadeB = "665f1c2e9b1e8a0000000b11";

    private static readonly Dictionary<string, UnitModel> Units = new()
    {
        [UnidadeA] = new UnitModel { Id = UnidadeA, CondominiumId = CondominioA, Number = "201" },
        [UnidadeB] = new UnitModel { Id = UnidadeB, CondominiumId = CondominioB, Number = "901" },
    };

    private static Task<string?> Resolve(string condominiumId, string? unitIdOrLabel, string? unitLabel = null) =>
        UnitIdResolver.ResolveAsync(
            condominiumId, unitIdOrLabel, unitLabel,
            id => Task.FromResult(Units.GetValueOrDefault(id)),
            (condominium, label) => Task.FromResult(Units.Values.FirstOrDefault(u => u.CondominiumId == condominium && u.Number == label)));

    [Fact]
    public async Task Id_de_unidade_do_proprio_condominio_e_aceito()
    {
        Assert.Equal(UnidadeA, await Resolve(CondominioA, UnidadeA));
    }

    [Fact]
    public async Task Id_de_unidade_de_outro_condominio_ou_inexistente_nao_e_aceito()
    {
        Assert.Null(await Resolve(CondominioA, UnidadeB));
        Assert.Null(await Resolve(CondominioA, "665f1c2e9b1e8a0000000c99"));
    }

    [Fact]
    public async Task Rotulo_e_procurado_so_no_condominio_informado()
    {
        Assert.Equal(UnidadeA, await Resolve(CondominioA, "201"));
        Assert.Null(await Resolve(CondominioA, "901"));
        Assert.Equal(UnidadeA, await Resolve(CondominioA, null, "201"));
        Assert.Null(await Resolve(CondominioA, null, null));
    }
}
