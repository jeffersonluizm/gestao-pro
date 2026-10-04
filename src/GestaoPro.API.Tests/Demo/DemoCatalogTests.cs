using GestaoPro.API.Controllers;
using GestaoPro.API.Services.Demo;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace GestaoPro.API.Tests.Demo;

// Modo demonstração: uma flag de servidor (Demo:Enabled, desligada por padrão), sem segredo no código.
// As senhas das contas demo e o código do condomínio vêm da configuração ou são sorteados na partida.
public class DemoCatalogTests
{
    private static DemoCatalog Catalog(params (string Key, string? Value)[] settings) =>
        DemoCatalog.FromConfiguration(new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(s => s.Key, s => s.Value))
            .Build());

    [Fact]
    public void Sem_a_flag_o_modo_demo_fica_desligado_e_sem_contas()
    {
        var catalog = Catalog();

        Assert.False(catalog.Enabled);
        Assert.Empty(catalog.Accounts);
    }

    [Fact]
    public void Com_a_flag_e_configuracao_usa_a_senha_e_o_codigo_configurados()
    {
        var catalog = Catalog(("Demo:Enabled", "true"), ("Demo:Senha", "Senha-Da-Demo-2026"), ("Demo:CodigoCondominio", "Codigo-Da-Demo-2026"));

        Assert.True(catalog.Enabled);
        Assert.Equal("Codigo-Da-Demo-2026", catalog.CondominiumCode);
        Assert.Equal(new[] { "sindico", "morador", "porteiro" }, catalog.Accounts.Select(a => a.Role));
        Assert.All(catalog.Accounts, account => Assert.Equal("Senha-Da-Demo-2026", account.Password));
    }

    [Fact]
    public void Com_a_flag_e_sem_configuracao_sorteia_senha_e_codigo_fortes()
    {
        var first = Catalog(("Demo:Enabled", "true"));
        var second = Catalog(("Demo:Enabled", "true"));

        Assert.All(first.Accounts, account => Assert.True(PasswordPolicy.IsStrong(account.Password)));
        Assert.True(CondominiumCodePolicy.IsStrong(first.CondominiumCode));
        Assert.NotEqual(first.Accounts[0].Password, second.Accounts[0].Password);
        Assert.DoesNotContain(first.Accounts, account => account.Password == "Admin@123");
    }

    [Theory]
    [InlineData("Demo:Senha", "Admin@123")]
    [InlineData("Demo:Senha", "12345678")]
    [InlineData("Demo:CodigoCondominio", "123456")]
    public void Senha_ou_codigo_fraco_na_configuracao_impede_a_partida(string key, string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Catalog(("Demo:Enabled", "true"), (key, value)));

        Assert.Contains(key.Replace(':', '_').Replace("_", "__"), error.Message);
    }

    [Fact]
    public void Contas_demo_sao_reconhecidas_pelo_email_so_com_a_flag_ligada()
    {
        var on = Catalog(("Demo:Enabled", "true"));
        var off = Catalog();

        Assert.True(on.IsDemoEmail("MORADOR@demo.gestaopro.local"));
        Assert.False(on.IsDemoEmail("maria@condominio.com"));
        Assert.False(off.IsDemoEmail("morador@demo.gestaopro.local"));
        // Protegidas são só as contas publicadas; outro e-mail do domínio demo não ganha proteção.
        Assert.False(on.IsDemoEmail("intruso@demo.gestaopro.local"));
        Assert.True(on.IsReservedDomain("intruso@demo.gestaopro.local"));
        Assert.False(off.IsReservedDomain("intruso@demo.gestaopro.local"));
    }
}

public class DemoControllerTests
{
    private static DemoCatalog Catalog(bool enabled) => DemoCatalog.FromConfiguration(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Demo:Enabled"] = enabled.ToString(),
            ["Demo:Senha"] = "Senha-Da-Demo-2026",
            ["Demo:CodigoCondominio"] = "Codigo-Da-Demo-2026",
        })
        .Build());

    [Fact]
    public void Sem_a_flag_o_endpoint_responde_404()
    {
        var result = new DemoController(Catalog(enabled: false)).Get();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Com_a_flag_devolve_as_contas_e_o_codigo_do_condominio()
    {
        var result = new DemoController(Catalog(enabled: true)).Get();

        var body = Assert.IsType<DemoInfoResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("Codigo-Da-Demo-2026", body.CondominiumCode);
        Assert.Equal(3, body.Accounts.Count);
        Assert.Contains(body.Accounts, account => account.Role == "sindico" && account.Email == "sindico@demo.gestaopro.local" && account.Password == "Senha-Da-Demo-2026");
        // Moradores pré-cadastrados (sem acesso) para experimentar a ativação com o código do condomínio.
        Assert.Equal(3, body.PreRegistered.Count);
        Assert.All(body.PreRegistered, resident => Assert.Matches(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$", resident.Cpf));
    }
}
