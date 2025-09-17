using Temporalio.Client;

namespace WorkflowDemos.Web.ModerationPortal.Services;

public class TemporalIntegrationService(ITemporalClient temporalClient) : IOrchestratorIntegrationService
{
    public string PartitionKey => "Temporal";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        await temporalClient.StartWorkflowAsync(
            "ContentModerationWorkflow",
            [
                new
                {
                    Comments = comments.Select(c => new
                    {
                        Text = c
                    }).ToList(),
                    DoManualReview = false
                }
            ],
            new WorkflowOptions(Guid.NewGuid().ToString(), "CONTENT_MODERATION_TASK_QUEUE"));
    }

    public async Task ApproveAsync(string workflowId)
    {
        var handle = temporalClient.GetWorkflowHandle(workflowId);
        await handle.SignalAsync("Approve", []);
    }

    public async Task RejectAsync(string workflowId)
    {
        var handle = temporalClient.GetWorkflowHandle(workflowId);
        await handle.SignalAsync("Reject", []);
    }
}
