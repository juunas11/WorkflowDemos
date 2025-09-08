namespace WorkflowDemos.MassTransit.Commands;

public record StoreInitialCommentState
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
}
