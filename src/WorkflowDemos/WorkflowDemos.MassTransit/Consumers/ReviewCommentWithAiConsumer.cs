using MassTransit;
using WorkflowDemos.MassTransit.Messages;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.MassTransit.Consumers;

public class ReviewCommentWithAiConsumer(
    IContentModerationService moderationService,
    ILogger<ReviewCommentWithAiConsumer> logger) : IConsumer<ReviewCommentWithAi>
{
    public async Task Consume(ConsumeContext<ReviewCommentWithAi> context)
    {
        var result = await moderationService.CheckCommentAsync(context.Message.CommentText);

        if (result)
        {
            logger.LogInformation("Comment {CommentId} approved by AI", context.Message.CommentId);
            await context.Publish<CommentApprovedByAi>(new
            {
                CommentId = context.Message.CommentId,
            });
        }
        else
        {
            logger.LogInformation("Comment {CommentId} rejected by AI", context.Message.CommentId);
            await context.Publish<CommentRejectedByAi>(new
            {
                CommentId = context.Message.CommentId,
            });
        }
    }
}
