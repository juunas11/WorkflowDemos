using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.NServiceBus.Events;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.NServiceBus.Handlers;

public class SaveResultHandler(
    IDataStorageService dataStorageService,
    ILogger<SaveResultHandler> logger) : IHandleMessages<SaveResult>
{
    public async Task Handle(SaveResult message, IMessageHandlerContext context)
    {
        var comment = await dataStorageService.GetEntityAsync(Constants.PartitionKey, message.CommentId.ToString());
        if (comment == null)
        {
            logger.LogWarning("Could not find comment {CommentId} to update state", message.CommentId);
            return;
        }

        comment.State = (message.ApprovedByAi, message.ApprovedByHuman) switch
        {
            (true, _) => ModerationState.ApprovedByAi,
            (false, true) => ModerationState.ApprovedByHuman,
            _ => ModerationState.Rejected,
        };
        await dataStorageService.UpdateEntityAsync(comment);
        logger.LogInformation("Updated comment {CommentId} state to {State}", message.CommentId, comment.State);

        await context.Publish(new ResultSaved
        {
            CommentId = message.CommentId,
        });
    }
}
