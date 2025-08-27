using Azure;
using Azure.Data.Tables;

namespace WorkflowDemos.Shared.DataStorage;

public class WorkflowEntity : ITableEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public required string Comment { get; set; }
    public required WorkflowState State { get; set; }
    //public required string ApprovalUrl { get; set; }
    //public string? ApproveRequestBody { get; set; }
    //public string? RejectRequestBody { get; set; }
}
