using MassTransit;
using WorkflowDemos.MassTransit.Commands;
using WorkflowDemos.MassTransit.Messages;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.MassTransit.Consumers;

public class StoreInitialCommentStateConsumer(
    IDataStorageService dataStorageService,
    ILogger<StoreInitialCommentStateConsumer> logger) : IConsumer<StoreInitialCommentState>
{
    public async Task Consume(ConsumeContext<StoreInitialCommentState> context)
    {
        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = Constants.PartitionKey,
            RowKey = context.Message.CommentId.ToString(),
            Comment = context.Message.CommentText,
            State = ModerationState.PendingAiReview,
            ManualApprovalWorkflowId = context.Message.CommentId.ToString(),
        });
        logger.LogInformation("Stored initial state for comment {CommentId}", context.Message.CommentId);
        await context.Publish<CommentInitialStateStored>(new
        {
            CommentId = context.Message.CommentId,
        });
    }
}
