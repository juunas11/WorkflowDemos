
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowDemos.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMailgunEmailService(this IServiceCollection services, string fromEmail, string domain, string apiKey)
    {
        services.AddTransient<IEmailService, MailgunEmailService>();
        services.AddFluentEmail(fromEmail)
            .AddMailGunSender(domain, apiKey);

        //services.AddSingleton<IEmailService, MockEmailService>();

        return services;
    }
}