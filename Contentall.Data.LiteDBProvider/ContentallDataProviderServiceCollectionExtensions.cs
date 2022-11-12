using Contentall.Data.Provider.Abstractions;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace Contentall.Data.LiteDBProvider;

public static partial class ContentallDataProviderServiceCollectionExtensions
{
    public static IServiceCollection AddLiteDBProvider(this IServiceCollection services, ConnectionString connection)
    {
        services.AddSingleton<EntitiesContainer, LiteDB>(sp =>
        {
            return new LiteDB(connection);
        });
        return services;
    }
}