using MassTransit;
using WorkflowDemos.MassTransit.Commands;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.MassTransit.Consumers;

public class SetCommentPendingHumanReviewConsumer(
    IDataStorageService dataStorageService,
    ILogger<SetCommentPendingHumanReviewConsumer> logger) : IConsumer<SetCommentPendingHumanReview>
{
    public async Task Consume(ConsumeContext<SetCommentPendingHumanReview> context)
    {
        var comment = await dataStorageService.GetEntityAsync(Constants.PartitionKey, context.Message.CommentId.ToString());
        if (comment == null)
        {
            logger.LogError("Comment {CommentId} not found in storage", context.Message.CommentId);
            return;
        }

        comment.State = ModerationState.PendingHumanReview;
        await dataStorageService.UpdateEntityAsync(comment);

        logger.LogInformation("Set comment {CommentId} to pending human review", context.Message.CommentId);
    }
}