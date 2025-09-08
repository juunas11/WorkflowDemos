namespace WorkflowDemos.MassTransit.Messages;

public record CommentRejectedByHuman
{
    public required Guid CommentId { get; init; }
}