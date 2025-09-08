namespace WorkflowDemos.MassTransit.Messages;

public record CommentApprovedByAi
{
    public required Guid CommentId { get; init; }
}
