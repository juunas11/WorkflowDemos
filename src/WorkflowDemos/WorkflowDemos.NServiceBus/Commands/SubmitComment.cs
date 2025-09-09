namespace WorkflowDemos.NServiceBus.Commands;

public record SubmitComment : ICommand
{
    public required Guid CommentId { get; init; }
    public required string CommentText { get; init; }
}
