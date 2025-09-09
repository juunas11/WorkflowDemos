namespace WorkflowDemos.NServiceBus.Sagas;

public enum CommentModerationState
{
    StoringInitialState,
    ReviewingWithAi,
    PendingHumanReview,
    SavingResult,
    Completed
}