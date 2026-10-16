namespace GestaoPro.API.Tests.Middleware;

// O middleware pergunta em toda escrita se já existe administrador. Depois que existe, a resposta não volta
// a ser "não" (e, se voltasse, manter "sim" só deixa a instalação anônima fechada): guarda o "sim" em memória.
public class CachedAdministratorLookupTests
{
    private sealed class CountingLookup(params bool[] answers) : IAdministratorLookup
    {
        public int Calls { get; private set; }

        public Task<bool> HasAnyAdministratorAsync() => Task.FromResult(answers[Math.Min(Calls++, answers.Length - 1)]);
    }

    [Fact]
    public async Task Depois_do_primeiro_sim_nao_consulta_mais_o_banco()
    {
        var inner = new CountingLookup(true);
        var cached = new CachedAdministratorLookup(inner);

        for (var i = 0; i < 5; i++) Assert.True(await cached.HasAnyAdministratorAsync());

        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Enquanto_a_resposta_e_nao_continua_consultando()
    {
        var inner = new CountingLookup(false, false, true);
        var cached = new CachedAdministratorLookup(inner);

        Assert.False(await cached.HasAnyAdministratorAsync());
        Assert.False(await cached.HasAnyAdministratorAsync());
        Assert.True(await cached.HasAnyAdministratorAsync());
        Assert.True(await cached.HasAnyAdministratorAsync());

        Assert.Equal(3, inner.Calls);
    }
}
