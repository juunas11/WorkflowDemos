using Azure;
using Azure.AI.ContentSafety;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowDemos.Shared.Moderation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureContentSafetyModeration(this IServiceCollection services, string endpoint, string apiKey)
    {
        services.AddSingleton(new ContentSafetyClient(new Uri(endpoint), new AzureKeyCredential(apiKey)));
        services.AddSingleton<IContentModerationService, AzureContentSafetyModerationService>();

        //services.AddSingleton<IContentModerationService, MockContentModerationService>();

        return services;
    }
}