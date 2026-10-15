using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GestaoPro.API.Models;
using GestaoPro.API.Services.Access;
using GestaoPro.API.Services.Demo;
using GestaoPro.API.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GestaoPro.API.Services;

public class TokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly DemoCatalog _demo;

    public TokenService(IOptions<JwtSettings> jwtSettings, DemoCatalog? demo = null)
    {
        _jwtSettings = jwtSettings.Value;
        _demo = demo ?? DemoCatalog.Disabled;
    }

    public (string token, DateTime expiresAt) GenerateToken(Credential credential)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, credential.UserId),
            new Claim(ClaimTypes.Role, credential.Role),
            new Claim(Caller.CondominiumIdClaim, credential.CondominiumId),
            new Claim("registrationNumber", credential.RegistrationNumber),
            new Claim("mustChangePassword", credential.MustChangePassword.ToString())
        };
        // Com o modo demonstração ligado, o token vale só na época atual (o reset recria contas e dados).
        if (_demo.Enabled) claims.Add(new Claim(DemoCatalog.EpochClaim, _demo.Epoch));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}

