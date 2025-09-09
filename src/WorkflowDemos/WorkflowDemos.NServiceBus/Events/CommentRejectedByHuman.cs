namespace WorkflowDemos.NServiceBus.Events;

public record CommentRejectedByHuman : IEvent
{
    public required Guid CommentId { get; init; }
}
