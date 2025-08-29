using Azure;
using Azure.Data.Tables;

namespace WorkflowDemos.Shared.DataStorage;

public class CommentEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public string? ManualApprovalWorkflowId { get; set; }
    public required string Comment { get; set; }
    public required ModerationState State { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
