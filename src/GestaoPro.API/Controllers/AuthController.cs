using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using GestaoPro.API.Services.Demo;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthAppService _service;
    private readonly IUserDirectory _users;
    private readonly SelfServiceAuthService _selfService;
    private readonly CondominiumAttemptLimiter _condominiumAttempts;

    public AuthController(
        AuthAppService service,
        IUserDirectory users,
        SelfServiceAuthService selfService,
        CondominiumAttemptLimiter condominiumAttempts,
        DemoCatalog? demo = null)
    {
        _service = service;
        _users = users;
        _selfService = selfService;
        _condominiumAttempts = condominiumAttempts;
        _demo = new DemoGuard(demo);
    }

    private readonly DemoGuard _demo;

    // Conta de demonstração compartilhada: senha e acesso não mudam (403).
    private async Task<IActionResult?> DenyDemoAccountAsync(string userId)
    {
        var user = await _users.GetByIdAsync(userId);
        return _demo.IsProtectedEmail(user?.Email) ? DemoGuard.Forbidden(DemoGuard.ProtectedAccountMessage) : null;
    }

    [HttpPost("login")]
    [EnableRateLimiting(AuthRateLimit.Login)]
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
        if (WeakPassword(request.Password) is { } weak) return weak;

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
    [EnableRateLimiting(AuthRateLimit.ChangePassword)]
    public async Task<IActionResult> ChangePassword([FromBody] AuthChangePasswordRequest request)
    {
        // A regra da senha nova não é segredo: responde com ela antes de olhar a conta.
        if (WeakPassword(request.NewPassword) is { } weak) return weak;

        try
        {
            await _selfService.ChangePasswordOnFirstAccessAsync(request);
            return NoContent();
        }
        catch (Exception)
        {
            // Rota anônima: só a mensagem única, nunca o texto da exceção.
            return BadRequest(new { message = SelfServiceAuthService.ChangePasswordRefusedMessage });
        }
    }

    [HttpPatch("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] AuthResetPasswordRequest request)
    {
        if (await DenyManageUserAsync(request.UserId) is { } denied) return denied;
        if (await DenyDemoAccountAsync(request.UserId) is { } demo) return demo;
        if (WeakPassword(request.NewPassword) is { } weak) return weak;

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
    [EnableRateLimiting(AuthRateLimit.Register)]
    public async Task<IActionResult> CheckCpf([FromBody] CheckCpfRequest request)
    {
        if (DenyTooManyCondominiumAttempts(request.CondominiumId) is { } tooMany) return tooMany;

        try
        {
            var result = await _selfService.CheckCpfAsync(request);
            return Ok(result);
        }
        catch (Exception)
        {
            return BadRequest(new { message = SelfServiceAuthService.CheckRefusedMessage });
        }
    }

    [HttpPost("register")]
    [EnableRateLimiting(AuthRateLimit.Register)]
    public async Task<IActionResult> SelfRegister([FromBody] SelfRegisterRequest request)
    {
        if (WeakPassword(request.Password) is { } weak) return weak;
        if (DenyTooManyCondominiumAttempts(request.CondominiumId) is { } tooMany) return tooMany;

        try
        {
            var result = await _selfService.SelfRegisterAsync(request);
            return Ok(result);
        }
        catch (Exception)
        {
            // Inclui e-mail já usado e falhas do banco: a resposta não diferencia os motivos.
            return BadRequest(new { message = SelfServiceAuthService.RegisterRefusedMessage });
        }
    }

    [HttpDelete("credentials/{userId}")]
    public async Task<IActionResult> Deactivate(string userId)
    {
        if (await DenyManageUserAsync(userId) is { } denied) return denied;
        if (await DenyDemoAccountAsync(userId) is { } demo) return demo;

        var result = await _service.DeactivateAsync(userId);
        return result ? NoContent() : NotFound();
    }

    private IActionResult? WeakPassword(string? password) =>
        PasswordPolicy.IsStrong(password) ? null : BadRequest(new { message = PasswordPolicy.WeakPasswordMessage });

    // Balde por condomínio do check-cpf e do cadastro (além do balde por IP): 429 com Retry-After.
    private IActionResult? DenyTooManyCondominiumAttempts(string condominiumId)
    {
        if (_condominiumAttempts.TryAcquire(condominiumId, out var retryAfter)) return null;

        AuthRateLimit.SetRetryAfter(Response, retryAfter);
        return StatusCode(StatusCodes.Status429TooManyRequests, new { message = AuthRateLimit.TooManyAttemptsMessage });
    }

    // Credenciais são escritas da administração (middleware): só de usuário do próprio condomínio (outro: 404).
    private async Task<ActionResult?> DenyManageUserAsync(string userId)
    {
        var caller = this.Caller();
        var user = await _users.GetByIdAsync(userId);
        return this.DenyManage(caller, user?.CondominiumId, caller.IsAdministrator);
    }
}

