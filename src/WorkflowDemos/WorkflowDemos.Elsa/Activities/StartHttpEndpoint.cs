using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.Elsa.Activities;

public class StartHttpEndpoint
{
}

[Action(DisplayName = "Comment moderation with AI", Outcomes = new[] { "Approved", "Rejected" })]
public class CommentAiModerationActivity(IContentModerationService contentModerationService) : Activity
{
    //[ActivityInput]
    //public string? Comment { get; set; }

    public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
    {
        var comment = context.GetVariable<string>("Comment");
        var approved = await contentModerationService.CheckCommentAsync(Comment ?? "");
        var outcome = approved ? "Approved" : "Rejected";
        return Outcomes(outcome);
    }
}