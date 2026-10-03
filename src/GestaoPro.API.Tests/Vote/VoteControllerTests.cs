using GestaoPro.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestaoPro.API.Tests.Vote;

public class VoteControllerTests
{
    private readonly Mock<IVoteService> _voteService = new();
    private readonly Mock<IAssemblyRepository> _assemblyRepository = new();

    [Theory]
    [InlineData("morador")]
    [InlineData("admin")]
    [InlineData("sindico")]
    public async Task CastVote_SempreUsaUsuarioDoToken(string role)
    {
        var dto = new CreateVoteDto { AssemblyId = "assembly-1", UserId = "outro-usuario", TopicIndex = 1, Vote = "sim" };
        _voteService.Setup(service => service.CreateAsync(It.IsAny<CreateVoteDto>()))
            .ReturnsAsync((CreateVoteDto value) => new VoteModel
            {
                AssemblyId = value.AssemblyId,
                UserId = value.UserId,
                TopicIndex = value.TopicIndex,
                Vote = value.Vote
            });
        _assemblyRepository.Setup(repository => repository.GetByIdAsync("assembly-1"))
            .ReturnsAsync(MakeAssembly());
        var controller = MakeController("usuario-token", role);

        var result = await controller.CastVote(dto);

        Assert.IsType<CreatedResult>(result);
        Assert.Equal("usuario-token", dto.UserId);
        _voteService.Verify(service => service.HasUserVotedAsync("assembly-1", "usuario-token", 1), Times.Once);
    }

    [Fact]
    public async Task GetResults_AgrupaTodosOsVotosPorPautaSemExporVotosDeOutrosUsuarios()
    {
        _voteService.Setup(service => service.GetByAssemblyAsync("assembly-1")).ReturnsAsync(new List<VoteModel>
        {
            new() { Id = "vote-1", AssemblyId = "assembly-1", UserId = "usuario-token", TopicIndex = 0, Vote = "sim" },
            new() { Id = "vote-2", AssemblyId = "assembly-1", UserId = "outro-usuario", TopicIndex = 0, Vote = "não" }
        });
        _assemblyRepository.Setup(repository => repository.GetByIdAsync("assembly-1"))
            .ReturnsAsync(MakeAssembly());
        var controller = MakeController("usuario-token", "morador");

        var result = await controller.GetResults("assembly-1");

        var response = Assert.IsType<OkObjectResult>(result);
        var rows = Assert.IsType<List<Dictionary<string, object?>>>(response.Value);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0]["totalVotes"]);
        Assert.Equal(1, rows[0]["yesVotes"]);
        Assert.Equal(1, rows[0]["noVotes"]);
        Assert.True((bool)rows[1]["summaryOnly"]!);
        Assert.Equal(0, rows[1]["totalVotes"]);
    }

    private VoteController MakeController(string userId, string role)
    {
        var controller = new VoteController(_voteService.Object, _assemblyRepository.Object);
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            }, "Test"))
        };
        return controller;
    }

    private static AssemblyModel MakeAssembly() => new()
    {
        Id = "assembly-1",
        Title = "Assembleia",
        Description = "Teste",
        StartTime = DateTime.UtcNow.AddHours(-1),
        EndTime = DateTime.UtcNow.AddHours(1),
        Topics = new List<TopicModel>
        {
            new() { Title = "Pauta 1", VoteOptions = new List<string> { "sim", "não" } },
            new() { Title = "Pauta 2", VoteOptions = new List<string> { "sim", "não" } }
        }
    };
}
