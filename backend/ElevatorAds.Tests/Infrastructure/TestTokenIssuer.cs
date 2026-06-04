using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElevatorAds.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace ElevatorAds.Tests.Infrastructure;

public static class TestTokenIssuer
{
    public const string Issuer = "ElevatorAds.Tests";
    public const string Audience = "ElevatorAds.Clients";

    public static string IssueToken(UserRole role, string username = "test-user", Guid? userId = null)
    {
        var secret = TestWebApplicationFactory.TestJwtSecret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new("role", role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string IssueAdminToken() => IssueToken(UserRole.Admin, "admin@test");
    public static string IssueOperatorToken() => IssueToken(UserRole.Operator, "operator@test");
    public static string IssueViewerToken() => IssueToken(UserRole.Viewer, "viewer@test");
}
