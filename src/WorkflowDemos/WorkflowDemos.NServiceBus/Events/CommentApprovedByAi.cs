namespace WorkflowDemos.NServiceBus.Events;

public record CommentApprovedByAi : IEvent
{
    public required Guid CommentId { get; init; }
}
