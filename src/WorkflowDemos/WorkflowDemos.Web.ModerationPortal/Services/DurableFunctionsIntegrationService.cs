namespace WorkflowDemos.Web.ModerationPortal.Services;

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
        var response = await _httpClient.PostAsJsonAsync(configuration["DurableFunctions:ModerationDecisionUrl"], new
        {
            InstanceId = workflowId,
            IsApproved = true
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string workflowId)
    {
        var response = await _httpClient.PostAsJsonAsync(configuration["DurableFunctions:ModerationDecisionUrl"], new
        {
            InstanceId = workflowId,
            IsApproved = false
        });
        response.EnsureSuccessStatusCode();
    }
}