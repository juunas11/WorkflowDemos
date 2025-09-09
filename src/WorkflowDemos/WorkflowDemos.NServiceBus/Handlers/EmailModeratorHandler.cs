using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.Shared.Email;

namespace WorkflowDemos.NServiceBus.Handlers;

public class EmailModeratorHandler(
    IEmailService emailService,
    ILogger<EmailModeratorHandler> logger) : IHandleMessages<EmailModerator>
{
    public async Task Handle(EmailModerator message, IMessageHandlerContext context)
    {
        await emailService.SendModerationRequiredEmailAsync(Constants.PartitionKey, message.CommentId.ToString());
        logger.LogInformation("Sent moderation email for comment {CommentId}", message.CommentId);
    }
}
