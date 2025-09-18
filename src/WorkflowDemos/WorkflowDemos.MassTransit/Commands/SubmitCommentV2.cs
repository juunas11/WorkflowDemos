namespace WorkflowDemos.MassTransit.Commands;

public record SubmitCommentV2
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
    public required bool DoManualReview { get; set; }
}