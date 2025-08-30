
namespace WorkflowDemos.Web.ModerationPortal.Services;

public interface IOrchestratorIntegrationService
{
    string PartitionKey { get; }

    Task SubmitCommentsAsync(IEnumerable<string> comments);
    Task ApproveAsync(string workflowId);
    Task RejectAsync(string workflowId);
}