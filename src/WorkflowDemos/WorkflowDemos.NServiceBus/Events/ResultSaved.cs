namespace WorkflowDemos.NServiceBus.Events;

public record ResultSaved : IEvent
{
    public required Guid CommentId { get; init; }
}