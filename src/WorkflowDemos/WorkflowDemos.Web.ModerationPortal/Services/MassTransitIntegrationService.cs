namespace WorkflowDemos.Web.ModerationPortal.Services;

public class MassTransitIntegrationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient();
    private readonly string _startUrl = configuration["MassTransit:ContentModerationWorkflowStartUrl"]!;
    private readonly string _approveUrlTemplate = configuration["MassTransit:ModerationApproveUrlTemplate"]!;
    private readonly string _rejectUrlTemplate = configuration["MassTransit:ModerationRejectUrlTemplate"]!;

    public string PartitionKey => "MassTransit";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var response = await httpClient.PostAsJsonAsync(_startUrl, new
        {
            Comments = comments.ToList(),
            DoManualReview = false
        });
        response.EnsureSuccessStatusCode();
    }

    public async Task ApproveAsync(string workflowId)
    {
        var response = await httpClient.PostAsJsonAsync(string.Format(_approveUrlTemplate, workflowId), new { });
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectAsync(string workflowId)
    {
        var response = await httpClient.PostAsJsonAsync(string.Format(_rejectUrlTemplate, workflowId), new { });
        response.EnsureSuccessStatusCode();
    }
}
