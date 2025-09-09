namespace WorkflowDemos.NServiceBus.Events;

public record CommentInitialStateStored : IEvent
{
    public required Guid CommentId { get; init; }
}
