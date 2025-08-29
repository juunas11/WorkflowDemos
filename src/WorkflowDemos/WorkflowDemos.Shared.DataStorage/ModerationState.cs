namespace WorkflowDemos.Shared.DataStorage;

public enum ModerationState
{
    PendingAiReview,
    ApprovedByAi,
    PendingHumanReview,
    ApprovedByHuman,
    Rejected
}
