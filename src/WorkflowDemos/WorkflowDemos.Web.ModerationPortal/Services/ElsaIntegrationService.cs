namespace WorkflowDemos.Web.ModerationPortal.Services;

public class ElsaIntegrationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    public string PartitionKey => "Elsa";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var content = JsonContent.Create(new
        {
            Comments = comments.Select(c => new
            {
                Text = c
            }).ToList(),
            DoManualReview = false
        });
        // ELSA does not support chunked transfer encoding
        // This works around that
        await content.LoadIntoBufferAsync();

        var response = await _httpClient.PostAsync(configuration["Elsa:ContentModerationWorkflowStartUrl"], content);
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(string workflowId)
    {
        var approveTriggerUrl = workflowId.Split(';')[0];

        var response = await _httpClient.PostAsJsonAsync(approveTriggerUrl, new { });
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string workflowId)
    {
        var rejectTriggerUrl = workflowId.Split(';')[1];

        var response = await _httpClient.PostAsJsonAsync(rejectTriggerUrl, new { });
        response.EnsureSuccessStatusCode();
    }
}