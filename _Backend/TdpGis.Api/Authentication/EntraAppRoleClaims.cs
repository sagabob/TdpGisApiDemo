using System.Security.Claims;

namespace TdpGis.Api.Authentication;

/// <summary>
///     Entra ID emits app roles in the <c>roles</c> claim; inbound claim mapping may also use
///     <see cref="ClaimTypes.Role" />.
///     ASP.NET Core <see cref="ClaimsPrincipal.IsInRole(string)" /> only works when
///     <see cref="ClaimsIdentity.RoleClaimType" />
///     matches — which is easy to get wrong — so we match known role claim types explicitly.
/// </summary>
public static class EntraAppRoleClaims
{
    private static readonly string[] RoleClaimTypes =
    [
        "roles",
        ClaimTypes.Role // same value as http://schemas.microsoft.com/ws/2008/06/identity/claims/role
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