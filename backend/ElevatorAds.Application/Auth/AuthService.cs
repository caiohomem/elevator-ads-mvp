using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElevatorAds.Domain.Entities;
using ElevatorAds.Domain.Enums;
using ElevatorAds.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ElevatorAds.Application.Auth;

public sealed class AuthService
{
    public const string RoleClaim = "role";
    public const string UsernameClaim = JwtRegisteredClaimNames.UniqueName;
    public const string UserIdClaim = JwtRegisteredClaimNames.Sub;

    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;

    public AuthService(IUserRepository userRepository, IConfiguration configuration, TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _timeProvider = timeProvider;
    }

    public async Task<LoginOutcome> ValidateAndLoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginOutcome.Failure("Username and password are required.");
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username.Trim());
        if (user is null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return LoginOutcome.Failure("Invalid username or password.");
        }

        var token = GenerateToken(user, out var expiresAt);
        return LoginOutcome.Success(new LoginResponse(token, user.Role.ToString(), expiresAt));
    }

    public string GenerateToken(User user, out DateTime expiresAt)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "ElevatorAds";
        var audience = jwtSection["Audience"] ?? "ElevatorAds.Clients";
        var lifetimeValue = jwtSection["LifetimeMinutes"];
        var lifetimeMinutes = int.TryParse(lifetimeValue, out var parsedLifetime) ? parsedLifetime : 60;
        var secret = _configuration["JWT:Secret"]
            ?? Environment.GetEnvironmentVariable("JWT__Secret")
            ?? throw new InvalidOperationException("JWT secret is not configured. Set the JWT__Secret environment variable.");

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException("JWT secret must be at least 32 bytes for HS256 signing.");
        }

        expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(lifetimeMinutes);

        var claims = new List<Claim>
        {
            new(UserIdClaim, user.Id.ToString()),
            new(UsernameClaim, user.Username),
            new(RoleClaim, user.Role.ToString())
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: _timeProvider.GetUtcNow().UtcDateTime,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public sealed record LoginOutcome(bool IsSuccess, string? Error, LoginResponse? Response)
    {
        public static LoginOutcome Success(LoginResponse response) => new(true, null, response);

        public static LoginOutcome Failure(string error) => new(false, error, null);
    }
}
