using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using WorkflowDemos.Elsa.Server.Types;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.Elsa.Server.Activities;

[Activity(
    Namespace = "ContentModeration",
    Category = "Moderation/AI",
    DisplayName = "Check comment with AI",
    Description = "Checks a comment using an AI content moderation service.")]
[FlowNode("Approved", "Rejected")]
public class CheckCommentWithAiActivity : Activity
{
    [Input(
        Description = "The comment to check")]
    public Input<Comment> Comment { get; set; } = null!;

    [Output(
        Description = "The updated comment")]
    public Output<Comment> UpdatedComment { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var comment = Comment.Get(context);

        var contentModerationService = context.GetRequiredService<IContentModerationService>();
        var isApproved = await contentModerationService.CheckCommentAsync(comment.Text);

        comment.ApprovedByAi = isApproved;
        UpdatedComment.Set(context, comment);

        await context.CompleteActivityWithOutcomesAsync(isApproved ? "Approved" : "Rejected");
    }
}
