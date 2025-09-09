namespace WorkflowDemos.NServiceBus.Commands;

public record ReviewCommentWithAi : ICommand
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
}
