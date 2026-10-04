using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthAppService _service;
    private readonly IUserDirectory _users;

    public AuthController(AuthAppService service, IUserDirectory users)
    {
        _service = service;
        _users = users;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _service.LoginAsync(request);

            if (response.MustChangePassword)
                return Ok(new
                {
                    mustChangePassword = true,
                    userId = response.UserId,
                    message = "Você precisa trocar sua senha antes de continuar."
                });

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("credentials")]
    public async Task<IActionResult> CreateCredential([FromBody] CreateCredentialRequest request)
    {
        if (await DenyManageUserAsync(request.UserId) is { } denied) return denied;
        if (this.DenyManage(this.Caller(), request.CondominiumId, staff: true) is { } otherCondominium) return otherCondominium;

        try
        {
            await _service.CreateCredentialAsync(request);
            return Created(string.Empty, null);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] AuthChangePasswordRequest request)
    {
        try
        {
            await _service.ChangePasswordAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] AuthResetPasswordRequest request)
    {
        if (await DenyManageUserAsync(request.UserId) is { } denied) return denied;

        try
        {
            await _service.ResetPasswordAsync(request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("check-cpf")]
    public async Task<IActionResult> CheckCpf([FromBody] CheckCpfRequest request)
    {
        try
        {
            var result = await _service.CheckCpfAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> SelfRegister([FromBody] SelfRegisterRequest request)
    {
        try
        {
            var result = await _service.SelfRegisterAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("credentials/{userId}")]
    public async Task<IActionResult> Deactivate(string userId)
    {
        if (await DenyManageUserAsync(userId) is { } denied) return denied;

        var result = await _service.DeactivateAsync(userId);
        return result ? NoContent() : NotFound();
    }

    // Credenciais são escritas da administração (middleware): só de usuário do próprio condomínio (outro: 404).
    private async Task<ActionResult?> DenyManageUserAsync(string userId)
    {
        var caller = this.Caller();
        var user = await _users.GetByIdAsync(userId);
        return this.DenyManage(caller, user?.CondominiumId, caller.IsAdministrator);
    }
}

