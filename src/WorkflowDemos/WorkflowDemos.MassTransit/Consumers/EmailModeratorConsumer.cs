using MassTransit;
using WorkflowDemos.MassTransit.Commands;
using WorkflowDemos.Shared.Email;

namespace WorkflowDemos.MassTransit.Consumers;

public class EmailModeratorConsumer(
    IEmailService emailService,
    ILogger<EmailModeratorConsumer> logger) : IConsumer<EmailModerator>
{
    public async Task Consume(ConsumeContext<EmailModerator> context)
    {
        await emailService.SendModerationRequiredEmailAsync(Constants.PartitionKey, context.Message.CommentId.ToString());
        logger.LogInformation("Sent moderation email for comment {CommentId}", context.Message.CommentId);
    }
}
