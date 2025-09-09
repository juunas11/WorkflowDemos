using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.NServiceBus.Handlers;

public class SetCommentPendingHumanReviewHandler(
    IDataStorageService dataStorageService,
    ILogger<SetCommentPendingHumanReviewHandler> logger) : IHandleMessages<SetCommentPendingHumanReview>
{
    public async Task Handle(SetCommentPendingHumanReview message, IMessageHandlerContext context)
    {
        var comment = await dataStorageService.GetEntityAsync(Constants.PartitionKey, message.CommentId.ToString());
        if (comment == null)
        {
            logger.LogError("Comment {CommentId} not found in storage", message.CommentId);
            return;
        }

        comment.State = ModerationState.PendingHumanReview;
        await dataStorageService.UpdateEntityAsync(comment);

        logger.LogInformation("Set comment {CommentId} to pending human review", message.CommentId);
    }
}
