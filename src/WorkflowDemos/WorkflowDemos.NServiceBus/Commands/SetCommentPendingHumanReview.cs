namespace WorkflowDemos.NServiceBus.Commands;

public record SetCommentPendingHumanReview : ICommand
{
    public required Guid CommentId { get; init; }
}
