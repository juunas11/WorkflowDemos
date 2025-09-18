using MassTransit;
using WorkflowDemos.MassTransit.Commands;
using WorkflowDemos.MassTransit.Messages;

namespace WorkflowDemos.MassTransit.Sagas;

public class CommentModerationStateMachine : MassTransitStateMachine<CommentModerationState>
{
    public CommentModerationStateMachine()
    {
        Event(() => CommentSubmitted, x => x.CorrelateById(context => context.Message.CommentId));
        Event(() => CommentSubmittedV2, x => x.CorrelateById(context => context.Message.CommentId));
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
                    context.Saga.DoManualReview = true; // V1 always does manual review if AI rejects
                })
                .PublishAsync(context => context.Init<StoreInitialCommentState>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    CommentText = context.Message.CommentText,
                }))
                .TransitionTo(StoringInitialState),
            When(CommentSubmittedV2)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CommentId;
                    context.Saga.CommentText = context.Message.CommentText;
                    context.Saga.DoManualReview = context.Message.DoManualReview;
                })
                .PublishAsync(context => context.Init<StoreInitialCommentState>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    CommentText = context.Message.CommentText,
                }))
                .TransitionTo(StoringInitialState)
        );

        During(StoringInitialState,
            When(CommentInitialStateStored)
                .PublishAsync(context => context.Init<ReviewCommentWithAi>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    CommentText = context.Saga.CommentText!,
                }))
                .TransitionTo(ReviewingWithAi)
        );

        During (ReviewingWithAi,
            When(CommentApprovedByAi)
                .PublishAsync(context => context.Init<SaveResult>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    ApprovedByAi = true,
                    ApprovedByHuman = false,
                }))
                .TransitionTo(SavingResult),
            When(CommentRejectedByAi)
                .IfElse(context => context.Saga.DoManualReview.GetValueOrDefault(true),
                    then => then
                        .PublishAsync(context => context.Init<SetCommentPendingHumanReview>(new
                        {
                            CommentId = context.Saga.CorrelationId,
                        }))
                        .PublishAsync(context => context.Init<EmailModerator>(new
                        {
                            CommentId = context.Saga.CorrelationId,
                        }))
                        .TransitionTo(PendingHumanReview),
                    @else => @else
                        .PublishAsync(context => context.Init<SaveResult>(new
                        {
                            CommentId = context.Saga.CorrelationId,
                            ApprovedByAi = false,
                            ApprovedByHuman = false,
                        }))
                        .TransitionTo(SavingResult))
        );

        During(PendingHumanReview,
            When(CommentApprovedByHuman)
                .PublishAsync(context => context.Init<SaveResult>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    ApprovedByAi = false,
                    ApprovedByHuman = true,
                }))
                .TransitionTo(SavingResult),
            When(CommentRejectedByHuman)
                .PublishAsync(context => context.Init<SaveResult>(new
                {
                    CommentId = context.Saga.CorrelationId,
                    ApprovedByAi = false,
                    ApprovedByHuman = false,
                }))
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
    public Event<SubmitCommentV2> CommentSubmittedV2 { get; private set; } = null!;
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
