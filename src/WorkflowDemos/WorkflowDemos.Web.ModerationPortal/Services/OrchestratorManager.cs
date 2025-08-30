namespace WorkflowDemos.Web.ModerationPortal.Services;

public class OrchestratorManager(
    IEnumerable<IOrchestratorIntegrationService> orchestratorIntegrationServices)
{
    private readonly Dictionary<string, IOrchestratorIntegrationService> servicesByPartitionKey =
        orchestratorIntegrationServices.ToDictionary(s => s.PartitionKey, StringComparer.OrdinalIgnoreCase);

    public async Task SubmitCommentsAsync(string partitionKey, IEnumerable<string> comments)
    {
        if (servicesByPartitionKey.TryGetValue(partitionKey, out var service))
        {
            await service.SubmitCommentsAsync(comments);
        }
        else
        {
            throw new InvalidOperationException($"No orchestrator integration service found for partition key '{partitionKey}'.");
        }
    }

    public async Task ApproveAsync(string partitionKey, string workflowId)
    {
        if (servicesByPartitionKey.TryGetValue(partitionKey, out var service))
        {
            await service.ApproveAsync(workflowId);
        }
        else
        {
            throw new InvalidOperationException($"No orchestrator integration service found for partition key '{partitionKey}'.");
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
