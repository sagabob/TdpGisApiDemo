using TdpGis.Application.Abstractions;

namespace TdpGis.Application.Common;

/// <summary>Shared workspace access-token checks for GIS query use cases.</summary>
public static class WorkspaceAccessGuard
{
    public static async Task<GisQueryFailureKind?> ValidateAsync(
        IGisConfigurationService configurationService,
        Guid workspaceId,
        string? workspaceAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return GisQueryFailureKind.InvalidWorkspaceId;

        if (string.IsNullOrWhiteSpace(workspaceAccessToken))
            return GisQueryFailureKind.MissingWorkspaceAccessToken;

        var validToken = await configurationService.GetValidWorkspaceAccessTokenAsync(
            workspaceId,
            workspaceAccessToken.Trim(),
            cancellationToken);

        return validToken is null ? GisQueryFailureKind.InvalidWorkspaceAccessToken : null;
    }
}
