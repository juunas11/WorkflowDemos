namespace WorkflowDemos.MassTransit.Messages;

public record EmailModerator
{
    public required Guid CommentId { get; init; }
}
