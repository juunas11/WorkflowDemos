using Temporalio.Workflows;
using WorkflowDemos.Temporal.Dtos;

namespace WorkflowDemos.Temporal;

[Workflow]
public class ManualModerationWorkflow
{
    private bool approved = false;
    private bool rejected = false;

    [WorkflowRun]
    public async Task<ManualModerationWorkflowOutput> RunAsync(ManualModerationWorkflowInput input)
    {
        var comment = input.Comment;
        var workflowId = Workflow.Info.WorkflowId;
        var defaultActivityOptions = new ActivityOptions
        {
            ScheduleToCloseTimeout = TimeSpan.FromMinutes(5),
        };

        await Workflow.ExecuteActivityAsync(
            (ContentModerationActivities x) => x.UpdateCommentWaitingManualApprovalAsync(new UpdateCommentWaitingManualApprovalInput(comment.Id, workflowId)), defaultActivityOptions);

        await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.EmailModeratorAsync(new EmailModeratorInput(comment.Id)), new()
        {
            ScheduleToCloseTimeout = TimeSpan.FromMinutes(30),
        });

        var gotResult = await Workflow.WaitConditionAsync(() => approved || rejected, TimeSpan.FromDays(1));
        if (gotResult && approved)
        {
            comment.ApprovedByHuman = true;
            await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.SetCommentApprovedByHumanAsync(new SetCommentApprovedByHumanInput(comment.Id)), defaultActivityOptions);
        }
        else
        {
            // Timed out or rejected
            comment.ApprovedByHuman = false;
            await Workflow.ExecuteActivityAsync((ContentModerationActivities x) => x.SetCommentRejectedAsync(new SetCommentRejectedInput(comment.Id)), defaultActivityOptions);
        }

        return new ManualModerationWorkflowOutput(comment);
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