using Temporalio.Activities;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.Temporal;

public class ContentModerationActivities(
    IContentModerationService contentModerationService,
    IEmailService emailService,
    IDataStorageService dataStorageService)
{
    private const string PartitionKey = "Temporal";

    [Activity]
    public async Task<Comment> CheckCommentAsync(Comment comment)
    {
        comment.ApprovedByAi = await contentModerationService.CheckCommentAsync(comment.Text);
        return comment;
    }

    [Activity]
    public async Task EmailModeratorAsync(string commentId)
    {
        await emailService.SendModerationRequiredEmailAsync(commentId);
    }

    [Activity]
    public async Task StoreInitialCommentStateAsync(string commentId, string comment)
    {
        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = PartitionKey,
            RowKey = commentId,
            Comment = comment,
            State = ModerationState.PendingAiReview,
            ManualApprovalWorkflowId = null,
        });
    }

    [Activity]
    public async Task UpdateCommentWaitingManualApprovalAsync(string commentId, string workflowId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, commentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.PendingHumanReview;
        entity.ManualApprovalWorkflowId = workflowId;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetCommentApprovedByAiAsync(string commentId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, commentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.ApprovedByAi;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetCommentApprovedByHumanAsync(string commentId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, commentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.ApprovedByHuman;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetCommentRejectedAsync(string commentId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, commentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.Rejected;
        await dataStorageService.UpdateEntityAsync(entity);
    }
}
