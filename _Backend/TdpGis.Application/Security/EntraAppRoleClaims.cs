using System.Security.Claims;

namespace TdpGis.Application.Security;

/// <summary>
///     Entra ID emits app roles in the <c>roles</c> claim; inbound claim mapping may also use
///     <see cref="ClaimTypes.Role" />.
///     Prefer this helper over <see cref="ClaimsPrincipal.IsInRole(string)" /> when RoleClaimType may not match.
/// </summary>
public static class EntraAppRoleClaims
{
    private static readonly string[] RoleClaimTypes =
    [
        "roles",
        ClaimTypes.Role
    ];

    public static bool HasRole(ClaimsPrincipal? user, string roleValue)
    {
        if (user?.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(roleValue))
            return false;

        return user.Claims.Where(claim => IsRoleClaimType(claim.Type))
            .Any(claim => string.Equals(claim.Value, roleValue, StringComparison.Ordinal));
    }

    private static bool IsRoleClaimType(string type)
    {
        return RoleClaimTypes.Any(t => type == t);
    }
}