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

public class DurableFunctionsIntegrationService(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    public string PartitionKey => "DurableFunctions";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var response = await _httpClient.PostAsJsonAsync(configuration["DurableFunctions:ContentModerationWorkflowStartUrl"], new
        {
            Comments = comments.Select(c => new
            {
                Text = c
            }).ToList(),
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(string workflowId)
    {
        var response = await _httpClient.PostAsJsonAsync(configuration["DurableFunctions:ApproveUrl"], new
        {
            InstanceId = workflowId
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string workflowId)
    {
        var response = await _httpClient.PostAsJsonAsync(configuration["DurableFunctions:RejectUrl"], new
        {
            InstanceId = workflowId
        });
        response.EnsureSuccessStatusCode();
    }
}