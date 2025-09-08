namespace WorkflowDemos.MassTransit.Messages;

public record CommentRejectedByAi
{
    public required Guid CommentId { get; init; }
}
