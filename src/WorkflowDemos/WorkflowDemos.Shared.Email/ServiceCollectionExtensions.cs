
using FluentEmail.Core;
using Microsoft.Extensions.DependencyInjection;

namespace WorkflowDemos.Shared.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMailgunEmailService(
        this IServiceCollection services,
        string fromEmail,
        string domain,
        string apiKey,
        string moderatorEmail,
        string moderationPortalUrl)
    {
        services.AddTransient<IEmailService>(sp => new MailgunEmailService(sp.GetRequiredService<IFluentEmail>(), moderatorEmail, moderationPortalUrl));
        services.AddFluentEmail(fromEmail)
            .AddMailGunSender(domain, apiKey);

        //services.AddSingleton<IEmailService, MockEmailService>();

        return services;
    }
}