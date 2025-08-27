using Microsoft.Extensions.Configuration;
using Temporalio.Activities;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.Temporal;

public class ContentModerationActivities(
    IContentModerationService contentModerationService,
    IEmailService emailService,
    IDataStorageService dataStorageService,
    IConfiguration configuration)
{
    private const string PartitionKey = "Temporal";

    [Activity]
    public async Task<Comment> CheckCommentAsync(Comment comment)
    {
        comment.ApprovedByAi = await contentModerationService.CheckCommentAsync(comment.Text);
        return comment;
    }

    [Activity]
    public async Task EmailModeratorAsync(string workflowId)
    {
        await emailService.SendEmailAsync(
            configuration["ModeratorEmail"]!,
            "Manual Moderation Required",
            $"A comment requires manual moderation. Please review it in the moderation portal: {configuration["ModerationPortalUrl"]}.\n\nPartition key: {PartitionKey}\n\nRow key: {workflowId}");
    }

    [Activity]
    public async Task StoreInitialWorkflowStateAsync(string workflowId, string comment)
    {
        await dataStorageService.CreateEntityAsync(new WorkflowEntity
        {
            PartitionKey = PartitionKey,
            RowKey = workflowId,
            Comment = comment,
            State = WorkflowState.WaitingApproval,
        });
    }

    [Activity]
    public async Task SetWorkflowApprovedAsync(string workflowId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, workflowId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = WorkflowState.Approved;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Activity]
    public async Task SetWorkflowRejectedAsync(string workflowId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, workflowId);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = WorkflowState.Rejected;
        await dataStorageService.UpdateEntityAsync(entity);
    }
}
