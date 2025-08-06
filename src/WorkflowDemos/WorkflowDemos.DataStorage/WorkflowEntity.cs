namespace WorkflowDemos.DataStorage;

public class WorkflowEntity
{
    public required string PartitionKey { get; set; }
    public required string RowKey { get; set; }
    public required WorkflowState State { get; set; }
    public required string ApprovalUrl { get; set; }
    public string? ApproveRequestBody { get; set; }
    public string? RejectRequestBody { get; set; }
}
