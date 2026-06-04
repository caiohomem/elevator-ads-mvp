using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Application.Auth;

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, string Role, DateTime ExpiresAt);

public sealed record AuthenticatedUser(Guid Id, string Username, UserRole Role);
