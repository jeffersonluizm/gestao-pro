using Microsoft.AspNetCore.Mvc;
using GestaoPro.API.DTOs;
using GestaoPro.API.Services;
using GestaoPro.API.Services.Access;
using GestaoPro.API.Services.Demo;
using MongoDB.Bson;

namespace GestaoPro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserAppService _service;
    private readonly IUserDirectory _directory;
    private readonly ILogger<UsersController> _logger;

    private readonly DemoGuard _demo;

    public UsersController(UserAppService service, IUserDirectory directory, ILogger<UsersController> logger, DemoCatalog? demo = null)
    {
        _service = service;
        _directory = directory;
        _logger = logger;
        _demo = new DemoGuard(demo);
    }

    // Conta de demonstração compartilhada: senha, perfil e acesso não mudam (403).
    private async Task<ActionResult?> DenyDemoAccountAsync(string id)
    {
        var user = await _directory.GetByIdAsync(id);
        return _demo.IsProtectedEmail(user?.Email) ? DemoGuard.Forbidden(DemoGuard.ProtectedAccountMessage) : null;
    }

    // Leituras: só usuários do condomínio do token. Administração recebe os dados completos;
    // os demais recebem o resumo (sem CPF, e-mail, telefone), que basta para as telas mostrarem nomes.
    [HttpGet]
    public async Task<IActionResult> GetAllActive()
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        return ForCaller(caller, await _directory.GetAllActiveAsync());
    }

    [HttpGet("inactive")]
    public async Task<IActionResult> GetAllInactive()
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!caller.IsAdministrator) return this.Forbidden();

        return ForCaller(caller, await _directory.GetAllInactiveAsync());
    }

    [HttpGet("condominium/{condominiumId}")]
    public async Task<IActionResult> GetByCondominium(string condominiumId)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!string.Equals(caller.CondominiumId, condominiumId, StringComparison.Ordinal)) return NotFound();

        return ForCaller(caller, await _directory.GetByCondominiumAsync(condominiumId));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;

        var user = await _directory.GetByIdAsync(id);
        if (user is null) return NotFound();

        return this.Deny(ResourceAccess.Decide(caller, user.Id, user.CondominiumId, caller.IsAdministrator))
            ?? Ok(user);
    }

    // Escritas da administração só em usuário do próprio condomínio (outro: 404).
    // Sem login só chega aqui a credencial do cadastro inicial, liberada pelo middleware.
    private async Task<ActionResult?> DenyManageUserAsync(string id)
    {
        if (User.Identity?.IsAuthenticated != true) return null;

        var caller = this.Caller();
        var user = await _directory.GetByIdAsync(id);
        if (user is null) return NotFound();
        return this.DenyManage(caller, user.CondominiumId, caller.IsAdministrator);
    }

    private OkObjectResult ForCaller(Caller caller, IEnumerable<UserResponse> users)
    {
        var sameCondominium = users
            .Where(user => string.Equals(user.CondominiumId, caller.CondominiumId, StringComparison.Ordinal))
            .ToList();

        return caller.IsAdministrator
            ? Ok(sameCondominium)
            : Ok(sameCondominium.Select(UserSummaryResponse.From).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Sem login só chega aqui o cadastro inicial (antes de existir administrador; ver ApiAccessMiddleware).
        if (User.Identity?.IsAuthenticated == true)
        {
            var caller = this.Caller();
            if (this.DenyManage(caller, request.CondominiumId, caller.IsAdministrator) is { } denied) return denied;
        }
        if (!string.IsNullOrEmpty(request.Password) && WeakPassword(request.Password) is { } weak) return weak;

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
        if (await DenyManageUserAsync(id) is { } denied) return denied;
        if ((request.Role is not null || request.IsActive is not null || request.Email is not null || request.MustChangePassword is not null) &&
            await DenyDemoAccountAsync(id) is { } demo) return demo;

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
        if (await DenyManageUserAsync(id) is { } denied) return denied;
        if (await DenyDemoAccountAsync(id) is { } demo) return demo;

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
        // Qualquer papel troca a própria senha (com a senha atual); a de outro id, nunca.
        var caller = this.Caller();
        if (!caller.HasSession) return this.Deny(AccessDecision.InvalidSession)!;
        if (!string.Equals(caller.UserId, id, StringComparison.Ordinal)) return this.Forbidden();
        if (await DenyDemoAccountAsync(id) is { } demo) return demo;
        if (WeakPassword(request.NewPassword) is { } weak) return weak;

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
        if (await DenyManageUserAsync(id) is { } denied) return denied;
        if (await DenyDemoAccountAsync(id) is { } demo) return demo;

        try
        {
            var temporaryPassword = await _service.ResetPasswordAsync(id);
            return temporaryPassword is null
                ? NotFound()
                : Ok(new { temporaryPassword, message = "Senha redefinida. Entregue a senha temporária à pessoa; ela cria uma nova no próximo acesso." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPatch("{id}/password")]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] ResetPasswordRequest request)
    {
        if (await DenyManageUserAsync(id) is { } denied) return denied;
        if (await DenyDemoAccountAsync(id) is { } demo) return demo;
        if (WeakPassword(request.NewPassword) is { } weak) return weak;

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
        if (await DenyManageUserAsync(id) is { } denied) return denied;
        if (await DenyDemoAccountAsync(id) is { } demo) return demo;
        if (WeakPassword(request.Password) is { } weak) return weak;

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

    private ActionResult? WeakPassword(string? password) =>
        PasswordPolicy.IsStrong(password) ? null : BadRequest(new { message = PasswordPolicy.WeakPasswordMessage });
}
