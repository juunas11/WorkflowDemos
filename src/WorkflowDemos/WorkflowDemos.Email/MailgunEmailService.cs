using FluentEmail.Core;

namespace WorkflowDemos.Email;

public class MailgunEmailService(IFluentEmail fluentEmail) : IEmailService
{
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await fluentEmail
            .To(to)
            .Subject(subject)
            .Body(body, isHtml: false)
            .SendAsync();
    }
}
