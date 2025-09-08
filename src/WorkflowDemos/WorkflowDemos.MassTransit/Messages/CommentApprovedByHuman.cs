namespace WorkflowDemos.MassTransit.Messages;

public record CommentApprovedByHuman
{
    public required Guid CommentId { get; init; }
}
