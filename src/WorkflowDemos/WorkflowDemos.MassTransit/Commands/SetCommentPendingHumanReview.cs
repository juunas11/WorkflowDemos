namespace WorkflowDemos.MassTransit.Commands;

public record SetCommentPendingHumanReview
{
    public required Guid CommentId { get; init; }
}