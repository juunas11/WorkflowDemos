using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;

namespace WorkflowDemos.Elsa.Server.Activities;

[Activity(
    Namespace = "ContentModeration",
    Category = "Moderation/Human",
    DisplayName = "Wait for manual approval",
    Description = "Waits for a human to approve or reject the comment.")]
[FlowNode("Approved", "Rejected")]
public class WaitManualApprovalActivity : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var bookmark = context.CreateBookmark("ManualApproval");

        var isApproved = bookmark.GetPayload<bool>();

        await context.CompleteActivityWithOutcomesAsync(isApproved ? "Approved" : "Rejected");
    }
}