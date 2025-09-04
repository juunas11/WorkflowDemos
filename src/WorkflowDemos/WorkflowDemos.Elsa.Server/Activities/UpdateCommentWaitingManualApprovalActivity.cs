using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using WorkflowDemos.Elsa.Server.Types;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.Elsa.Server.Activities;

[Activity(
    Namespace = "ContentModeration",
    Category = "Moderation/Storage",
    DisplayName = "Update comment waiting manual approval",
    Description = "Updates the comment state to indicate it is waiting for manual approval.")]
public class UpdateCommentWaitingManualApprovalActivity : CodeActivity
{
    [Input(
        Description = "The comment to store")]
    public Input<Comment> Comment { get; set; } = null!;

    //[Input(
    //    Description = "The ID of the workflow instance handling the manual approval")]
    //public Input<string> WorkflowInstanceId { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // TODO: Check if this is right
        var workflowInstanceId = context.GetVariable<string>("WorkflowInstanceId");
        var dataStorageService = context.GetRequiredService<IDataStorageService>();
        var comment = Comment.Get(context);
        //var workflowInstanceId = WorkflowInstanceId.Get(context);

        var entity = await dataStorageService.GetEntityAsync(Constants.StoragePartitionKey, comment.Id);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.PendingHumanReview;
        entity.ManualApprovalWorkflowId = workflowInstanceId;
        await dataStorageService.UpdateEntityAsync(entity);
    }
}