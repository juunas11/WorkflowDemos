using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.NServiceBus.Events;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.NServiceBus.Handlers;

public class ReviewCommentWithAiHandler(
    IContentModerationService moderationService,
    ILogger<ReviewCommentWithAiHandler> logger) : IHandleMessages<ReviewCommentWithAi>
{
    public async Task Handle(ReviewCommentWithAi message, IMessageHandlerContext context)
    {
        var result = await moderationService.CheckCommentAsync(message.CommentText);
        if (result)
        {
            logger.LogInformation("Comment {CommentId} approved by AI", message.CommentId);
            await context.Publish(new CommentApprovedByAi
            {
                CommentId = message.CommentId,
            });
        }
        else
        {
            logger.LogInformation("Comment {CommentId} rejected by AI", message.CommentId);
            await context.Publish(new CommentRejectedByAi
            {
                CommentId = message.CommentId,
            });
        }
    }
}
