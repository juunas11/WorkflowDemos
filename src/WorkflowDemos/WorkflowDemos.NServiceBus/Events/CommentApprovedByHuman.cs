namespace WorkflowDemos.NServiceBus.Events;

public record CommentApprovedByHuman : IEvent
{
    public required Guid CommentId { get; init; }
}
