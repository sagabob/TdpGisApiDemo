using Microsoft.Extensions.DependencyInjection;
using TdpGis.Application.UseCases.GetGisWorkspaceEntities;
using TdpGis.Application.UseCases.SearchGisEntity;

namespace TdpGis.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISearchGisEntityUseCase, SearchGisEntityUseCase>();
        services.AddScoped<IGetGisWorkspaceEntitiesUseCase, GetGisWorkspaceEntitiesUseCase>();
        return services;
    }
}