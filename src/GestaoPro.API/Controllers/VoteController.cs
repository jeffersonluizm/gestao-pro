using GestaoPro.API.DTOs;
using GestaoPro.API.Models;
using GestaoPro.API.Repositories;
using GestaoPro.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VoteController : ControllerBase
{
    private readonly IVoteService _voteService;
    private readonly IAssemblyRepository _assemblyRepository;

    public VoteController(IVoteService voteService, IAssemblyRepository assemblyRepository)
    {
        _voteService = voteService;
        _assemblyRepository = assemblyRepository;
    }

    [HttpPost]
    public async Task<IActionResult> CastVote([FromBody] CreateVoteDto dto)
    {
        var currentUserId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized(new { message = "Login obrigatório para votar." });

        dto.UserId = currentUserId;

        if (await _voteService.HasUserVotedAsync(dto.AssemblyId, dto.UserId, dto.TopicIndex))
            return BadRequest(new { message = "Você já votou nesta pauta." });

        try
        {
            var vote = await _voteService.CreateAsync(dto);
            return Created(string.Empty, await ToVoteResponseAsync(vote, currentUserId));
        }
        catch (VoteOperationException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "Ocorreu um erro interno ao processar a votação." });
        }
    }

    [HttpGet("assembly/{assemblyId}")]
    public async Task<IActionResult> GetResults(string assemblyId)
    {
        var allVotes = await _voteService.GetByAssemblyAsync(assemblyId);
        var currentUserId = CurrentUserId();
        var visibleVotes = IsAdministrator()
            ? allVotes
            : allVotes.Where(vote => string.Equals(vote.UserId, currentUserId, StringComparison.Ordinal)).ToList();
        var assembly = await _assemblyRepository.GetByIdAsync(assemblyId);
        var response = new List<Dictionary<string, object?>>();

        foreach (var vote in visibleVotes)
        {
            var voteResponse = await ToVoteResponseAsync(vote, currentUserId);
            AddTopicSummary(voteResponse, allVotes, vote.TopicIndex, assembly);
            response.Add(voteResponse);
        }

        if (assembly?.Topics is not null)
        {
            for (var topicIndex = 0; topicIndex < assembly.Topics.Count; topicIndex++)
            {
                if (response.Any(item => Convert.ToInt32(item["topicIndex"]) == topicIndex))
                    continue;

                var summary = new Dictionary<string, object?>
                {
                    ["assemblyId"] = assemblyId,
                    ["assemblyTitle"] = assembly.Title,
                    ["assemblyDescription"] = assembly.Description,
                    ["topicIndex"] = topicIndex,
                    ["topicDescription"] = assembly.Topics[topicIndex].Title,
                    ["summaryOnly"] = true,
                    ["userId"] = string.Empty,
                    ["voto"] = string.Empty
                };
                AddTopicSummary(summary, allVotes, topicIndex, assembly);
                response.Add(summary);
            }
        }

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var vote = await _voteService.GetByIdAsync(id);
        return vote is null ? NotFound() : Ok(await ToVoteResponseAsync(vote, CurrentUserId()));
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private bool IsAdministrator()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return role is "admin" or "sindico";
    }

    private static void AddTopicSummary(
        Dictionary<string, object?> response,
        IEnumerable<VoteModel> allVotes,
        int topicIndex,
        AssemblyModel? assembly)
    {
        var topicVotes = allVotes.Where(item => item.TopicIndex == topicIndex).ToList();
        var optionCounts = topicVotes
            .GroupBy(item => NormalizeVoteValue(item.Vote), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var configuredOptions = assembly?.Topics.ElementAtOrDefault(topicIndex)?.VoteOptions
            ?? new List<string> { "sim", "não" };
        foreach (var option in configuredOptions.Select(NormalizeVoteValue).Where(option => option.Length > 0))
            optionCounts.TryAdd(option, 0);

        var highestCount = optionCounts.Values.DefaultIfEmpty(0).Max();
        var leaders = optionCounts.Where(item => item.Value == highestCount).Select(item => item.Key).ToList();
        var winner = highestCount == 0 ? "Sem votos" : leaders.Count > 1 ? "Empate" : leaders[0];

        response["optionCounts"] = optionCounts;
        response["yesVotes"] = optionCounts.GetValueOrDefault("sim");
        response["noVotes"] = optionCounts.GetValueOrDefault("não");
        response["abstentionVotes"] = optionCounts.GetValueOrDefault("abstenção");
        response["totalVotes"] = topicVotes.Count;
        response["winner"] = winner;
        response["voteFinished"] = assembly?.VoteFinished ?? false;
    }

    private static string NormalizeVoteValue(string? value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "nao" => "não",
            "abstencao" => "abstenção",
            _ => normalized
        };
    }

    private async Task<Dictionary<string, object?>> ToVoteResponseAsync(VoteModel vote, string? currentUserId)
    {
        var assembly = await _assemblyRepository.GetByIdAsync(vote.AssemblyId);
        var topicDescription = assembly?.Topics.ElementAtOrDefault(vote.TopicIndex)?.Title ?? "Pauta não disponível";
        var isOwnVote = string.Equals(vote.UserId, currentUserId, StringComparison.Ordinal);

        return new Dictionary<string, object?>
        {
            ["id"] = vote.Id,
            ["assemblyId"] = vote.AssemblyId,
            ["assemblyTitle"] = assembly?.Title,
            ["assemblyDescription"] = assembly?.Description,
            ["topicDescription"] = topicDescription,
            ["voteStarted"] = assembly?.VoteStarted ?? false,
            ["voteFinished"] = assembly?.VoteFinished ?? false,
            ["voteStatus"] = assembly?.VoteStatus ?? "Ainda não iniciou",
            ["startDate"] = assembly?.StartDate,
            ["endDate"] = assembly?.EndDate,
            ["voteStartDate"] = assembly?.StartTime,
            ["voteEndDate"] = assembly?.EndTime,
            ["status"] = assembly?.Status,
            ["userId"] = vote.UserId,
            ["topicIndex"] = vote.TopicIndex,
            ["voto"] = isOwnVote ? vote.Vote : "Privado",
            ["createdAt"] = vote.CreatedAt,
            ["summaryOnly"] = false
        };
    }
}
