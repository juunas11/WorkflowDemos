using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowDemos.DataStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTableStorageService(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton(new TableServiceClient(connectionString));
        services.AddSingleton<IDataStorageService, TableStorageService>();
        return services;
    }
}
