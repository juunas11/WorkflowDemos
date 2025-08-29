namespace WorkflowDemos.Web.ModerationPortal.Services;

public class OrchestratorEventSender(
    IEnumerable<IOrchestratorEventService> orchestratorEventServices)
{
    private readonly Dictionary<string, IOrchestratorEventService> servicesByPartitionKey =
        orchestratorEventServices.ToDictionary(s => s.PartitionKey, StringComparer.OrdinalIgnoreCase);

    public async Task ApproveAsync(string partitionKey, string workflowId)
    {
        if (servicesByPartitionKey.TryGetValue(partitionKey, out var service))
        {
            await service.ApproveAsync(workflowId);
        }
        else
        {
            throw new InvalidOperationException($"No orchestrator event service found for partition key '{partitionKey}'.");
        }
    }

    public async Task RejectAsync(string partitionKey, string workflowId)
    {
        if (servicesByPartitionKey.TryGetValue(partitionKey, out var service))
        {
            await service.RejectAsync(workflowId);
        }
        else
        {
            throw new InvalidOperationException($"No orchestrator event service found for partition key '{partitionKey}'.");
        }
    }
}
