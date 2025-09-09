using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.NServiceBus.Events;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.NServiceBus.Handlers;

public class StoreInitialCommentStateHandler(
    IDataStorageService dataStorageService,
    ILogger<StoreInitialCommentStateHandler> logger) : IHandleMessages<StoreInitialCommentState>
{
    public async Task Handle(StoreInitialCommentState message, IMessageHandlerContext context)
    {
        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = Constants.PartitionKey,
            RowKey = message.CommentId.ToString(),
            Comment = message.CommentText,
            State = ModerationState.PendingAiReview,
            ManualApprovalWorkflowId = message.CommentId.ToString(),
        });
        logger.LogInformation("Stored initial state for comment {CommentId}", message.CommentId);
        await context.Publish(new CommentInitialStateStored
        {
            CommentId = message.CommentId,
        });
    }
}