using FluentEmail.Core;

namespace WorkflowDemos.Shared.Email;

public class MailgunEmailService(
    IFluentEmail fluentEmail,
    string moderatorEmail,
    string moderationPortalUrl) : IEmailService
{
    public async Task SendModerationRequiredEmailAsync(string partitionKey, string rowKey)
    {
        await fluentEmail
            .To(moderatorEmail)
            .Subject("Manual Moderation Required")
            .Body($"A comment requires manual moderation. Please review it in the moderation portal: {moderationPortalUrl}.\n\nPartition key: {partitionKey}\n\nRow key: {rowKey}")
            .SendAsync();
    }
}
