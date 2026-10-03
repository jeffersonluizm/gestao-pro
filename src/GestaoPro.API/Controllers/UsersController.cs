using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using MongoDB.Bson;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserAppService _service;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserAppService service, ILogger<UsersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllActive() =>
        Ok(await _service.GetAllActiveAsync());

    [HttpGet("inactive")]
    public async Task<IActionResult> GetAllInactive() =>
        Ok(await _service.GetAllInactiveAsync());

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetByCondominium(string condominiumId) =>
        Ok(await _service.GetByCondominiumAsync(condominiumId));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _service.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            var created = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(string id)
    {
        id = id.Trim();
        _logger.LogInformation("Solicitação para desativar usuário {UserId}", id);

        if (!ObjectId.TryParse(id, out _))
            return BadRequest(new { message = "Identificador de usuário inválido." });

        try
        {
            var result = await _service.DeactivateAsync(id);
            return result
                ? NoContent()
                : NotFound(new { message = "Usuário não encontrado." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao desativar usuário {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Não foi possível desativar o usuário. Tente novamente." });
        }
    }

    [HttpPatch("{id}/change-password")]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var result = await _service.ChangePasswordAsync(id, request);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id)
    {
        try
        {
            var result = await _service.ResetPasswordAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id}/password")]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] ResetPasswordRequest request)
    {
        try
        {
            var result = await _service.UpdatePasswordByAuthAsync(id, request.NewPassword);
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/credentials")]
    public async Task<IActionResult> CreateCredentialForExistingUser(string id, [FromBody] CreateUserCredentialRequest request)
    {
        try
        {
            var user = await _service.CreateCredentialForExistingUserAsync(id, request);
            return user is null ? NotFound() : Ok(new
            {
                message = "Credencial criada com sucesso.",
                user.RegistrationNumber
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

