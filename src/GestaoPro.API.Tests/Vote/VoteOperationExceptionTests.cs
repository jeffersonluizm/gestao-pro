namespace GestaoPro.API.Tests.Vote;

public class VoteOperationExceptionTests
{
    [Fact]
    public void Constructor_StatusCodePadrao_Retorna400()
    {
        var ex = new VoteOperationException("algum erro");

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void Constructor_StatusCodePersonalizado_UsaValorFornecido()
    {
        var ex = new VoteOperationException("não encontrado", 404);

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public void Constructor_Mensagem_EhDefinidaCorretamente()
    {
        var ex = new VoteOperationException("usuário já votou", 409);

        Assert.Equal("usuário já votou", ex.Message);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public void Constructor_StatusCodesUsados_SaoPreservados(int statusCode)
    {
        var ex = new VoteOperationException("msg", statusCode);

        Assert.Equal(statusCode, ex.StatusCode);
    }

    [Fact]
    public void VoteOperationException_EHerdaDeException()
    {
        var ex = new VoteOperationException("erro");

        Assert.IsAssignableFrom<Exception>(ex);
    }
}
