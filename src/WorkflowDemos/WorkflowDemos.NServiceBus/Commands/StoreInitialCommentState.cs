namespace WorkflowDemos.NServiceBus.Commands;

public record StoreInitialCommentState : ICommand
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
}