namespace WorkflowDemos.MassTransit.Messages;

public record SubmitComment
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
}
