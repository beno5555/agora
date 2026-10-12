using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using colosseumbackend.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace colosseumbackend.Services;

public class JwtTokenService
{
    private readonly JwtOptions  _jwtOptions;
    private readonly AuthOptions _authOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions, IOptions<AuthOptions> authOptions)
    {
        _jwtOptions  = jwtOptions.Value;
        _authOptions = authOptions.Value;
    }

    public (string Token, DateTime ExpiresAt) CreateSessionToken(string wallet)
    {
        var expiresAt   = DateTime.UtcNow + _authOptions.SessionLifetime;
        var credentials = new SigningCredentials(SigningKey(_jwtOptions), SecurityAlgorithms.HmacSha256);
        var token       = new JwtSecurityToken(
            issuer:             _jwtOptions.Issuer,
            audience:           _jwtOptions.Audience,
            claims:             [new Claim(ClaimTypes.NameIdentifier, wallet)],
            expires:            expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public static SymmetricSecurityKey SigningKey(JwtOptions jwtOptions)
    {
        return new SymmetricSecurityKey(Convert.FromBase64String(jwtOptions.SigningKey));
    }
}
