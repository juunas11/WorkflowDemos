
namespace WorkflowDemos.Web.ModerationPortal.Services;

public interface IOrchestratorEventService
{
    string PartitionKey { get; }

    Task ApproveAsync(string rowKey);
    Task RejectAsync(string rowKey);
}