using System.Text.RegularExpressions;

namespace GestaoPro.API.Tests.Users;

// Matrícula gerada no cadastro pela administração. Com 4 dígitos, duas contas no mesmo ano colidiam e o
// cadastro falhava com "e-mail, CPF ou matrícula já cadastrado", sem nada repetido no que a pessoa digitou.
public class RegistrationNumberTests
{
    [Fact]
    public async Task Gera_no_formato_COND_ano_e_seis_digitos()
    {
        var number = await RegistrationNumber.NextAsync(_ => Task.FromResult(false), year: 2026);

        Assert.Matches(new Regex(@"^COND-2026-\d{6}$"), number);
    }

    [Fact]
    public async Task Na_colisao_tenta_de_novo_ate_achar_uma_livre()
    {
        var tentativas = new List<string>();

        var number = await RegistrationNumber.NextAsync(candidate =>
        {
            tentativas.Add(candidate);
            return Task.FromResult(tentativas.Count < 3);
        });

        Assert.Equal(3, tentativas.Count);
        Assert.Equal(tentativas[^1], number);
    }

    [Fact]
    public async Task Sem_matricula_livre_explica_o_problema_sem_culpar_os_dados_digitados()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RegistrationNumber.NextAsync(_ => Task.FromResult(true)));

        Assert.Equal(RegistrationNumber.ExhaustedMessage, error.Message);
        Assert.DoesNotContain("CPF", error.Message);
    }
}
