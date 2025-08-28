using Temporalio.Workflows;

namespace WorkflowDemos.Temporal;

[Workflow]
public class ManualModerationWorkflow
{
    private bool approved = false;
    private bool rejected = false;

    [WorkflowRun]
    public async Task<Comment> RunAsync(Comment comment)
    {
        if (comment.ApprovedByAi)
        {
            // Already approved by AI, no need for manual review
            return comment;
        }

        var workflowId = Workflow.Info.WorkflowId;
        var defaultActivityOptions = new ActivityOptions
        {
            ScheduleToCloseTimeout = TimeSpan.FromMinutes(5),
        };

        await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.StoreInitialWorkflowStateAsync(workflowId, comment.Text), defaultActivityOptions);

        await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.EmailModeratorAsync(workflowId), new()
        {
            ScheduleToCloseTimeout = TimeSpan.FromMinutes(30),
        });

        var gotResult = await Workflow.WaitConditionAsync(() => approved || rejected, TimeSpan.FromDays(7));
        if (gotResult && approved)
        {
            comment.ApprovedByHuman = true;
            await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.SetWorkflowApprovedAsync(workflowId), defaultActivityOptions);
        }
        else
        {
            // Timed out or rejected
            comment.ApprovedByHuman = false;
            await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.SetWorkflowRejectedAsync(workflowId), defaultActivityOptions);
        }

        return comment;
    }

    [WorkflowSignal]
    public Task Approve()
    {
        approved = true;
        return Task.CompletedTask;
    }

    [WorkflowSignal]
    public Task Reject()
    {
        rejected = true;
        return Task.CompletedTask;
    }
}