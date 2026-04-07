using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using TdpGis.Api.GisQuery;
using TdpGis.Application.Abstractions;
using TdpGis.Domain;
using Xunit;

namespace TdpGis.Api.Tests.GisQuery;

public class GisWorkspaceAccessTests
{
    [Fact]
    public async Task TryValidateAsync_returns_400_when_workspace_is_empty()
    {
        var repository = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        var request = new DefaultHttpContext().Request;

        var result =
            await GisWorkspaceAccess.TryValidateAsync(repository.Object, Guid.Empty, request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Value.StatusCode.Should().Be(400);
        result.Value.Message.Should().Contain("workspace id");
    }

    [Fact]
    public async Task TryValidateAsync_returns_400_when_access_token_missing()
    {
        var repository = new Mock<IGisConfigurationService>(MockBehavior.Strict);
        var request = new DefaultHttpContext().Request;

        var result =
            await GisWorkspaceAccess.TryValidateAsync(repository.Object, Guid.NewGuid(), request,
                CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.StatusCode.Should().Be(400);
        result.Value.Message.Should().Contain("Authorization: Bearer");
    }

    [Fact]
    public async Task TryValidateAsync_returns_401_when_token_invalid()
    {
        var workspaceId = Guid.NewGuid();
        var request = new DefaultHttpContext().Request;
        request.Headers[GisWorkspaceAccess.AccessTokenHeader] = "bad-token";

        var repository = new Mock<IGisConfigurationService>();
        repository
            .Setup(r => r.GetValidWorkspaceAccessTokenAsync(workspaceId, "bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GisWorkspaceAccessToken?)null);

        var result =
            await GisWorkspaceAccess.TryValidateAsync(repository.Object, workspaceId, request, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task TryValidateAsync_returns_null_when_token_is_valid()
    {
        var workspaceId = Guid.NewGuid();
        var request = new DefaultHttpContext().Request;
        request.Headers[GisWorkspaceAccess.AccessTokenHeader] = "good-token";

        var tokenRow = new GisWorkspaceAccessToken
        {
            Id = Guid.NewGuid(),
            GisWorkspaceId = workspaceId,
            Name = "test",
            AccessToken = "good-token",
            ExpiredDateTime = DateTime.UtcNow.AddHours(1),
            IsActive = true,
            IsPublic = false,
            GisWorkspace = new GisWorkspace { Id = workspaceId, Name = "workspace" }
        };

        var repository = new Mock<IGisConfigurationService>();
        repository
            .Setup(r => r.GetValidWorkspaceAccessTokenAsync(workspaceId, "good-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenRow);

        var result =
            await GisWorkspaceAccess.TryValidateAsync(repository.Object, workspaceId, request, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveAccessToken_returns_header_value_when_present()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers[GisWorkspaceAccess.AccessTokenHeader] = " header-token ";
        request.Headers.Authorization = "Bearer ignored";

        var token = GisWorkspaceAccess.ResolveAccessToken(request);

        token.Should().Be("header-token");
    }

    [Fact]
    public void ResolveAccessToken_returns_bearer_value_when_custom_header_missing()
    {
        var request = new DefaultHttpContext().Request;
        request.Headers.Authorization = "Bearer bearer-token";

        var token = GisWorkspaceAccess.ResolveAccessToken(request);

        token.Should().Be("bearer-token");
    }
}