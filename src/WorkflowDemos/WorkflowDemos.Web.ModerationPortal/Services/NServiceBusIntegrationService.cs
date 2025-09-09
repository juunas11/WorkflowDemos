namespace WorkflowDemos.Web.ModerationPortal.Services;

public class NServiceBusIntegrationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IOrchestratorIntegrationService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient();
    private readonly string _startUrl = configuration["NServiceBus:ContentModerationWorkflowStartUrl"]!;
    private readonly string _approveUrlTemplate = configuration["NServiceBus:ModerationApproveUrlTemplate"]!;
    private readonly string _rejectUrlTemplate = configuration["NServiceBus:ModerationRejectUrlTemplate"]!;

    public string PartitionKey => "NServiceBus";

    public async Task SubmitCommentsAsync(IEnumerable<string> comments)
    {
        var response = await httpClient.PostAsJsonAsync(_startUrl, new
        {
            Comments = comments.ToList(),
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