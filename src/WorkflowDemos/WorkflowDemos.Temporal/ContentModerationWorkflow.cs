using Temporalio.Workflows;
using WorkflowDemos.Temporal.Dtos;

namespace WorkflowDemos.Temporal;

[Workflow]
public class ContentModerationWorkflow
{
    [WorkflowRun]
    public async Task<ContentModerationWorkflowOutput> RunAsync(ContentModerationWorkflowInput input)
    {
        if (input?.Comments == null || input.Comments.Count == 0)
        {
            return new ContentModerationWorkflowOutput(new List<string>());
        }

        var comments = input.Comments;
        // Set ID for all comments
        foreach (var comment in comments)
        {
            comment.Id = Workflow.NewGuid().ToString();
        }

        var defaultActivityOptions = new ActivityOptions
        {
            ScheduleToCloseTimeout = TimeSpan.FromMinutes(5),
        };

        var storeInitialStateTasks = comments.ConvertAll(comment =>
            Workflow.ExecuteActivityAsync(
                (ContentModerationActivities x) => x.StoreInitialCommentStateAsync(new StoreInitialCommentStateInput(comment.Id, comment.Text)), defaultActivityOptions));
        await Workflow.WhenAllAsync(storeInitialStateTasks);

        var contentFilteringTasks = input.Comments.ConvertAll(comment =>
        Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.CheckCommentAsync(new CheckCommentInput(comment)), defaultActivityOptions));
        comments = (await Workflow.WhenAllAsync(contentFilteringTasks))
            .Select(x => x.Comment)
            .ToList();

        var updateCommentStateTasks = comments
            .Where(comment => comment.ApprovedByAi)
            .Select(comment =>
                Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.SetCommentApprovedByAiAsync(new SetCommentApprovedByAiInput(comment.Id)), defaultActivityOptions))
            .ToList();
        await Workflow.WhenAllAsync(updateCommentStateTasks);

        var manualApprovalTasks = comments.ConvertAll(comment =>
        {
            if (comment.ApprovedByAi)
            {
                // Already approved by AI, no need for manual review
                return Task.FromResult(new ManualModerationWorkflowOutput(comment));
            }

            return Workflow.ExecuteChildWorkflowAsync((ManualModerationWorkflow x) => x.RunAsync(new ManualModerationWorkflowInput(comment)), new()
            {
                ParentClosePolicy = ParentClosePolicy.Terminate,
            });
        });
        comments = (await Workflow.WhenAllAsync(manualApprovalTasks))
            .Select(x => x.Comment)
            .ToList();

        return new ContentModerationWorkflowOutput(comments
            .Where(c => c.ApprovedByAi || c.ApprovedByHuman)
            .Select(c => c.Text)
            .ToList());
    }
}
