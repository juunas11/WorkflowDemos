namespace WorkflowDemos.Web.ModerationPortal.Services;

public class LogicAppsIntegrationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    public string PartitionKey => "LogicApps";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var contentModerationWorkflowStartUrl = configuration["LogicApps:ContentModerationWorkflowStartUrl"];
        if (string.IsNullOrWhiteSpace(contentModerationWorkflowStartUrl))
        {
            throw new InvalidOperationException("Logic Apps content moderation workflow start URL is not configured.");
        }

        foreach (var comment in comments)
        {
            var content = JsonContent.Create(new
            {
                Comment = comment,
            });
            // Logic Apps does not support chunked transfer encoding
            // This works around that
            await content.LoadIntoBufferAsync();
            var response = await _httpClient.PostAsync(contentModerationWorkflowStartUrl, content);
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task ApproveAsync(string workflowId)
    {
        await SendDecisionAsync(workflowId, true);
    }

    public async Task RejectAsync(string workflowId)
    {
        await SendDecisionAsync(workflowId, false);
    }

    private async Task SendDecisionAsync(string callbackUrl, bool isApproved)
    {
        if (string.IsNullOrWhiteSpace(callbackUrl))
        {
            throw new InvalidOperationException("Logic Apps manual approval callback URL is missing.");
        }

        var content = JsonContent.Create(new
        {
            IsApproved = isApproved
        });
        await content.LoadIntoBufferAsync();
        var response = await _httpClient.PostAsync(callbackUrl, content);
        response.EnsureSuccessStatusCode();
    }
}
