using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using WorkflowDemos.Elsa.Server.Types;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.Elsa.Server.Activities;

[Activity(
    Namespace = "ContentModeration",
    Category = "Moderation/Human",
    DisplayName = "Wait for manual approval",
    Description = "Waits for a human to approve or reject the comment.")]
[FlowNode("Approved", "Rejected")]
public class WaitManualApprovalActivity : Activity
{
    [Input(
        Description = "The comment that needs approval")]
    public Input<Comment> Comment { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Create bookmarks to pause the workflow until a signal is received.
        var approveBookmark = context.CreateBookmark("Approve", ApproveAsync);
        var rejectBookmark = context.CreateBookmark("Reject", RejectAsync);

        var dataStorageService = context.GetRequiredService<IDataStorageService>();
        var comment = Comment.Get(context);

        var entity = await dataStorageService.GetEntityAsync(Constants.StoragePartitionKey, comment.Id);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        var approveTriggerUrl = context.GenerateBookmarkTriggerUrl(approveBookmark.Id);
        var rejectTriggerUrl = context.GenerateBookmarkTriggerUrl(rejectBookmark.Id);

        entity.State = ModerationState.PendingHumanReview;
        // Instead of storing the workflow, in ELSA's case we store the URLs to trigger approval or rejection
        entity.ManualApprovalWorkflowId = $"{approveTriggerUrl};{rejectTriggerUrl}";
        await dataStorageService.UpdateEntityAsync(entity);
    }

    public ValueTask ApproveAsync(ActivityExecutionContext context)
    {
        return context.CompleteActivityWithOutcomesAsync("Approved");
    }

    public ValueTask RejectAsync(ActivityExecutionContext context)
    {
        return context.CompleteActivityWithOutcomesAsync("Rejected");
    }
}