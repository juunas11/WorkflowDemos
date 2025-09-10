namespace WorkflowDemos.Web.ModerationPortal.Services;

public class LogicAppsIntegrationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public string PartitionKey => "LogicApps";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var content = JsonContent.Create(new
        {
            Comments = comments.ToList(),
        });
        // Logic Apps does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(configuration["LogicApps:ContentModerationWorkflowStartUrl"], content);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(string workflowId)
    {
        var content = JsonContent.Create(new
        {
            CommentId = workflowId,
            IsApproved = true
        });
        // Logic Apps does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(configuration["LogicApps:ModerationDecisionUrl"], content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string workflowId)
    {
        var content = JsonContent.Create(new
        {
            CommentId = workflowId,
            IsApproved = false
        });
        // Logic Apps does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(configuration["LogicApps:ModerationDecisionUrl"], content);
        response.EnsureSuccessStatusCode();
    }
}