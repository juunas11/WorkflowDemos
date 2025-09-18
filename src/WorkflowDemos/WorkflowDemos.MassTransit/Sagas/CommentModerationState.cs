using MassTransit;

namespace WorkflowDemos.MassTransit.Sagas;

public class CommentModerationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public string? CommentText { get; set; }
    public bool ApprovedByAi { get; set; }
    public bool ApprovedByHuman { get; set; }

    // V2
    public bool? DoManualReview { get; set; } = null;
}
