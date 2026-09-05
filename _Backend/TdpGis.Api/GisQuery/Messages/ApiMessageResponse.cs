namespace TdpGis.Api.GisQuery.Messages;

/// <summary>Standard error payload for GIS query endpoints.</summary>
public sealed class ApiMessageResponse
{
    public required string Message { get; init; }
}