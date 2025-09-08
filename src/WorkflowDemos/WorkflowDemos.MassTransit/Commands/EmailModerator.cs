namespace WorkflowDemos.MassTransit.Commands;

public record EmailModerator
{
    public required Guid CommentId { get; init; }
}
