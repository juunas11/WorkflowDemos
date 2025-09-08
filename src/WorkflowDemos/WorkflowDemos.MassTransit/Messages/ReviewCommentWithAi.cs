namespace WorkflowDemos.MassTransit.Messages;

public record ReviewCommentWithAi
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
}