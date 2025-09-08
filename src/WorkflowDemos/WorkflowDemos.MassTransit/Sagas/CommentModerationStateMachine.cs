using MassTransit;
using WorkflowDemos.MassTransit.Messages;

namespace WorkflowDemos.MassTransit.Sagas;

public class CommentModerationStateMachine : MassTransitStateMachine<CommentModerationState>
{
    public CommentModerationStateMachine()
    {
        Event(() => CommentSubmitted, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => CommentInitialStateStored, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => CommentApprovedByAi, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => CommentRejectedByAi, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => CommentApprovedByHuman, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => CommentRejectedByHuman, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => ResultSaved, x => x.CorrelateById(context => context.Message.CommentId));

        InstanceState(x => x.CurrentState);

        Initially(
            When(CommentSubmitted)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CommentId;
                    context.Saga.CommentText = context.Message.CommentText;
                })
                .PublishAsync(context => context.Init<StoreInitialCommentState>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    CommentText = context.Message.CommentText,
                }))
                .TransitionTo(StoringInitialState));

        During(StoringInitialState,
            When(CommentInitialStateStored)
                .Publish(context => new ReviewCommentWithAi
                {
                    CommentId = context.Saga.CorrelationId,
                    CommentText = context.Saga.CommentText!,
                })
                .TransitionTo(ReviewingWithAi)
        );

        During (ReviewingWithAi,
            When(CommentApprovedByAi)
                .Publish(context => new SaveResult
                {
                    CommentId = context.Saga.CorrelationId,
                    ApprovedByAi = true,
                    ApprovedByHuman = false,
                })
                .TransitionTo(SavingResult),
            When(CommentRejectedByAi)
                .PublishAsync(context => context.Init<EmailModerator>(new
                {
                    CommentId = context.Saga.CorrelationId,
                }))
                .TransitionTo(PendingHumanReview)
        );

        During(PendingHumanReview,
            When(CommentApprovedByHuman)
                .Publish(context => new SaveResult
                {
                    CommentId = context.Saga.CorrelationId,
                    ApprovedByAi = false,
                    ApprovedByHuman = true,
                })
                .TransitionTo(SavingResult),
            When(CommentRejectedByHuman)
                .Publish(context => new SaveResult
                {
                    CommentId = context.Saga.CorrelationId,
                    ApprovedByAi = false,
                    ApprovedByHuman = false,
                })
                .TransitionTo(SavingResult)
        );

        During(SavingResult,
            When(ResultSaved)
                .Then(context =>
                {
                    context.Saga.ApprovedByAi = context.Message.ApprovedByAi;
                    context.Saga.ApprovedByHuman = context.Message.ApprovedByHuman;
                })
                .Finalize()
        );
    }

    public Event<SubmitComment> CommentSubmitted { get; private set; } = null!;
    public Event<CommentInitialStateStored> CommentInitialStateStored { get; private set; } = null!;
    public Event<CommentApprovedByAi> CommentApprovedByAi { get; private set; } = null!;
    public Event<CommentRejectedByAi> CommentRejectedByAi { get; private set; } = null!;
    public Event<CommentApprovedByHuman> CommentApprovedByHuman { get; private set; } = null!;
    public Event<CommentRejectedByHuman> CommentRejectedByHuman { get; private set; } = null!;
    public Event<ResultSaved> ResultSaved { get; private set; } = null!;

    public State StoringInitialState { get; private set; } = null!;
    public State ReviewingWithAi { get; private set; } = null!;
    public State PendingHumanReview { get; private set; } = null!;
    public State SavingResult { get; private set; } = null!;
}

public class CommentModerationState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;

    public string? CommentText { get; set; }
    public bool ApprovedByAi { get; set; }
    public bool ApprovedByHuman { get; set; }
}
