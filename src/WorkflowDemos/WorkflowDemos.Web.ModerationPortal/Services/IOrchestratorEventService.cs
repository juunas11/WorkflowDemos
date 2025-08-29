
namespace WorkflowDemos.Web.ModerationPortal.Services;

public interface IOrchestratorEventService
{
    string PartitionKey { get; }

    Task ApproveAsync(string workflowId);
    Task RejectAsync(string workflowId);
}