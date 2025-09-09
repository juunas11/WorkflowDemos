namespace WorkflowDemos.NServiceBus.Events;

public record CommentRejectedByAi : IEvent
{
    public required Guid CommentId { get; init; }
}
