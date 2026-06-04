using ElevatorAds.Application.Auth;
using ElevatorAds.Domain.Enums;

namespace ElevatorAds.Api.Auth;

public static class AuthPolicies
{
    public const string AdminPolicy = "AdminPolicy";
    public const string OperatorPolicy = "OperatorPolicy";
    public const string ViewerPolicy = "ViewerPolicy";

    public const string AdminRole = nameof(UserRole.Admin);
    public const string OperatorRole = nameof(UserRole.Operator);
    public const string ViewerRole = nameof(UserRole.Viewer);

    public const string AuthenticatedUser = "AuthenticatedUser";
}

public static class AuthClaimTypes
{
    public const string Role = AuthService.RoleClaim;
}
