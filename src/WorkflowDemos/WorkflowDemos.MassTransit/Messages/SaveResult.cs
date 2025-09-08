namespace WorkflowDemos.MassTransit.Messages;

public record SaveResult
{
    public required Guid CommentId { get; init; }
    public required bool ApprovedByAi { get; set; }
    public required bool ApprovedByHuman { get; set; }
}