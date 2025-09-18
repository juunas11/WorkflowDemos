namespace WorkflowDemos.NServiceBus.Sagas;

public class CommentModerationSagaData : ContainSagaData
{
    public CommentModerationState CurrentState { get; set; }

    public Guid CommentId { get; set; }
    public string? CommentText { get; set; }
    public bool ApprovedByAi { get; set; }
    public bool ApprovedByHuman { get; set; }

    // V2
    public bool? DoManualReview { get; set; } = null;
}
