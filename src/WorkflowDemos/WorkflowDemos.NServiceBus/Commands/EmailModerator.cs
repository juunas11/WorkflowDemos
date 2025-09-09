namespace WorkflowDemos.NServiceBus.Commands;

public record EmailModerator : ICommand
{
    public required Guid CommentId { get; init; }
}
