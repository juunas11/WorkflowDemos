using Temporalio.Activities;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;
using WorkflowDemos.Temporal.Dtos;

namespace WorkflowDemos.Temporal;

public class ContentModerationActivities(
    IContentModerationService contentModerationService,
    IEmailService emailService,
    IDataStorageService dataStorageService)
{
    private const string PartitionKey = "Temporal";

    [Activity]
    public async Task<CheckCommentOutput> CheckCommentAsync(CheckCommentInput input)
    {
        var comment = input.Comment;
        comment.ApprovedByAi = await contentModerationService.CheckCommentAsync(comment.Text);
        return new CheckCommentOutput(comment);
    }

    [Activity]
    public async Task EmailModeratorAsync(EmailModeratorInput input)
    {
        await emailService.SendModerationRequiredEmailAsync(PartitionKey, input.CommentId);
    }

    [Activity]
    public async Task StoreInitialCommentStateAsync(StoreInitialCommentStateInput input)
    {
        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = PartitionKey,
            RowKey = input.CommentId,
            Comment = input.CommentText,
            State = ModerationState.PendingAiReview,
            ManualApprovalWorkflowId = null,
        });
    }

    [Activity]
    public async Task UpdateCommentWaitingManualApprovalAsync(UpdateCommentWaitingManualApprovalInput input)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, input.CommentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.PendingHumanReview;
        entity.ManualApprovalWorkflowId = input.WorkflowId;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetCommentApprovedByAiAsync(SetCommentApprovedByAiInput input)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, input.CommentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.ApprovedByAi;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetCommentApprovedByHumanAsync(SetCommentApprovedByHumanInput input)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, input.CommentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.ApprovedByHuman;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetCommentRejectedAsync(SetCommentRejectedInput input)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, input.CommentId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.Rejected;
        await dataStorageService.UpdateEntityAsync(entity);
    }
}
