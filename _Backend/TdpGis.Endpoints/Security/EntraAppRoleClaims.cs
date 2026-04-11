using System.Security.Claims;

namespace TdpGis.Endpoints.Security;

/// <summary>
/// Entra ID emits app roles in the <c>roles</c> claim; inbound claim mapping may also use <see cref="ClaimTypes.Role"/>.
/// ASP.NET Core <see cref="ClaimsPrincipal.IsInRole(string)"/> only works when <see cref="ClaimsIdentity.RoleClaimType"/>
/// matches — which is easy to get wrong — so we match known role claim types explicitly.
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

        foreach (var claim in user.Claims)
        {
            if (!IsRoleClaimType(claim.Type))
                continue;
            if (string.Equals(claim.Value, roleValue, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static bool IsRoleClaimType(string type)
    {
        foreach (var t in RoleClaimTypes)
        {
            if (type == t)
                return true;
        }

        return false;
    }
}
