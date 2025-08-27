using Temporalio.Workflows;

namespace WorkflowDemos.Temporal;

[Workflow]
public class ContentModerationWorkflow
{
    [WorkflowRun]
    public async Task<List<string>> RunAsync(WorkflowInput input)
    {
        if (input?.Comments == null || input.Comments.Count == 0)
        {
            return new List<string>();
        }

        var comments = input.Comments;

        var contentFilteringTasks = input.Comments.ConvertAll(comment => Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.CheckCommentAsync(comment), new()
        {
            ScheduleToCloseTimeout = TimeSpan.FromMinutes(5),
        }));
        comments = (await Workflow.WhenAllAsync(contentFilteringTasks)).ToList();

        var manualApprovalTasks = comments.ConvertAll(comment => Workflow.ExecuteChildWorkflowAsync((ManualModerationWorkflow x) => x.RunAsync(comment), new()
        {
            ParentClosePolicy = ParentClosePolicy.Terminate,
        }));
        comments = (await Workflow.WhenAllAsync(manualApprovalTasks)).ToList();

        return comments
            .Where(c => c.ApprovedByAi || c.ApprovedByHuman)
            .Select(c => c.Text)
            .ToList();
    }
}

public class WorkflowInput
{
    public required List<Comment> Comments { get; set; }
}

public class Comment
{
    public required string Text { get; set; }
    public bool ApprovedByAi { get; set; }
    public bool ApprovedByHuman { get; set; }
}
