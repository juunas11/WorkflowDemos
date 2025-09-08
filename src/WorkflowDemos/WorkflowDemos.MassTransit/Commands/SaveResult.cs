namespace WorkflowDemos.MassTransit.Commands;

public record SaveResult
{
    public required Guid CommentId { get; init; }
    public required bool ApprovedByAi { get; init; }
    public required bool ApprovedByHuman { get; init; }
}
