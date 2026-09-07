using TdpGis.Application.Abstractions;
using TdpGis.Domain;

namespace TdpGis.Application.Common;

/// <summary>Shared workspace access-token checks for GIS query use cases.</summary>
public static class WorkspaceAccessGuard
{
    public static async Task<WorkspaceAccessValidation> ValidateAsync(
        IGisConfigurationService configurationService,
        Guid workspaceId,
        string? workspaceAccessToken,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return WorkspaceAccessValidation.Failed(GisQueryFailureKind.InvalidWorkspaceId);

        if (string.IsNullOrWhiteSpace(workspaceAccessToken))
            return WorkspaceAccessValidation.Failed(GisQueryFailureKind.MissingWorkspaceAccessToken);

        var validToken = await configurationService.GetValidWorkspaceAccessTokenAsync(
            workspaceId,
            workspaceAccessToken.Trim(),
            cancellationToken);

        return validToken is null
            ? WorkspaceAccessValidation.Failed(GisQueryFailureKind.InvalidWorkspaceAccessToken)
            : WorkspaceAccessValidation.Ok(validToken);
    }
}

public readonly struct WorkspaceAccessValidation
{
    public GisQueryFailureKind? FailureKind { get; private init; }
    public GisWorkspaceAccessToken? Token { get; private init; }

    public bool IsValid => FailureKind is null && Token is not null;

    public static WorkspaceAccessValidation Failed(GisQueryFailureKind kind)
    {
        return new WorkspaceAccessValidation { FailureKind = kind };
    }

    public static WorkspaceAccessValidation Ok(GisWorkspaceAccessToken token)
    {
        return new WorkspaceAccessValidation { Token = token };
    }
}