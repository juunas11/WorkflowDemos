using Microsoft.Extensions.Logging;

namespace WorkflowDemos.Email;

public class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
    {
        logger.LogInformation("Mock email sent to {To} with subject '{Subject}' and body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}