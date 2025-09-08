namespace WorkflowDemos.MassTransit.Messages;

public record CommentInitialStateStored
{
    public required Guid CommentId { get; init; }
}
