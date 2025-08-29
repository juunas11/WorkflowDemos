using Temporalio.Client;

namespace WorkflowDemos.Web.ModerationPortal.Services;

public class TemporalEventService(ITemporalClient temporalClient) : IOrchestratorEventService
{
    public string PartitionKey => "Temporal";

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
