using MassTransit;
using WorkflowDemos.MassTransit.Messages;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.MassTransit.Consumers;

public class SaveResultConsumer(
    IDataStorageService dataStorageService,
    ILogger<EmailModeratorConsumer> logger) : IConsumer<ResultSaved>
{
    public async Task Consume(ConsumeContext<ResultSaved> context)
    {
        var comment = await dataStorageService.GetEntityAsync("MassTransit", context.Message.CommentId.ToString());
        if (comment == null)
        {
            logger.LogWarning("Could not find comment {CommentId} to update state", context.Message.CommentId);
            return;
        }

        comment.State = (context.Message.ApprovedByAi, context.Message.ApprovedByHuman) switch
        {
            (true, _) => ModerationState.ApprovedByAi,
            (false, true) => ModerationState.ApprovedByHuman,
            _ => ModerationState.Rejected,
        };
        await dataStorageService.UpdateEntityAsync(comment);
        logger.LogInformation("Updated comment {CommentId} state to {State}", context.Message.CommentId, comment.State);

        await context.Publish<ResultSaved>(new
        {
            context.Message.CommentId,
            context.Message.ApprovedByAi,
            context.Message.ApprovedByHuman,
        });
    }
}