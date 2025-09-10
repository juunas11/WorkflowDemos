namespace WorkflowDemos.Web.ModerationPortal.Services;

public class PowerAutomateIntegrationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public string PartitionKey => "PowerAutomate";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var content = JsonContent.Create(new
        {
            Comments = comments.ToList(),
        });
        // Power Automate does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(configuration["PowerAutomate:ContentModerationWorkflowStartUrl"], content);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(string workflowId)
    {
        var content = JsonContent.Create(new
        {
            CommentId = workflowId,
            IsApproved = true
        });
        // Power Automate does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(configuration["PowerAutomate:ModerationDecisionUrl"], content);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string workflowId)
    {
        var content = JsonContent.Create(new
        {
            CommentId = workflowId,
            IsApproved = false
        });
        // Power Automate does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(configuration["PowerAutomate:ModerationDecisionUrl"], content);
        response.EnsureSuccessStatusCode();
    }
}