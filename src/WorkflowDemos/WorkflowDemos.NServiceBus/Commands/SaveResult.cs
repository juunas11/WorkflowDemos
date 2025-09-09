namespace WorkflowDemos.NServiceBus.Commands;

public record SaveResult : ICommand
{
    public required Guid CommentId { get; init; }
    public required bool ApprovedByAi { get; init; }
    public required bool ApprovedByHuman { get; init; }
}
