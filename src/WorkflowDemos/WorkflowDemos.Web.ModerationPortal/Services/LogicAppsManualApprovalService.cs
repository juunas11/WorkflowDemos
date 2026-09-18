using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;

namespace WorkflowDemos.Web.ModerationPortal.Services;

public sealed class LogicAppsManualApprovalService(
    IDataStorageService dataStorageService,
    IEmailService emailService)
{
    private const string PartitionKey = "LogicApps";

    public async Task<bool> TryRegisterAsync(LogicAppsManualApprovalSubscription request)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, request.CommentId);
        if (entity is null)
        {
            return false;
        }

        entity.State = ModerationState.PendingHumanReview;
        entity.ManualApprovalWorkflowId = request.CallbackUrl;
        await dataStorageService.UpdateEntityAsync(entity);

        await emailService.SendModerationRequiredEmailAsync(PartitionKey, request.CommentId);
        return true;
    }

    public async Task<bool> TryUnregisterAsync(string commentId)
    {
        var entity = await dataStorageService.GetEntityAsync(PartitionKey, commentId);
        if (entity is null)
        {
            return false;
        }

        if (entity.ManualApprovalWorkflowId is null)
        {
            return true;
        }

        entity.ManualApprovalWorkflowId = null;
        await dataStorageService.UpdateEntityAsync(entity);

        return true;
    }
}

public sealed record LogicAppsManualApprovalSubscription(string CommentId, string CallbackUrl);
