using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.NServiceBus.Events;

namespace WorkflowDemos.NServiceBus.Sagas;

public class CommentModerationSaga :
    Saga<CommentModerationSagaData>,
    IAmStartedByMessages<SubmitComment>,
    IHandleMessages<CommentInitialStateStored>,
    IHandleMessages<CommentApprovedByAi>,
    IHandleMessages<CommentRejectedByAi>,
    IHandleMessages<CommentApprovedByHuman>,
    IHandleMessages<CommentRejectedByHuman>,
    IHandleMessages<ResultSaved>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<CommentModerationSagaData> mapper)
    {
        mapper.MapSaga(saga => saga.CommentId)
            .ToMessage<SubmitComment>(message => message.CommentId)
            .ToMessage<CommentApprovedByAi>(message => message.CommentId)
            .ToMessage<CommentApprovedByHuman>(message => message.CommentId)
            .ToMessage<CommentInitialStateStored>(message => message.CommentId)
            .ToMessage<CommentRejectedByAi>(message => message.CommentId)
            .ToMessage<CommentRejectedByHuman>(message => message.CommentId)
            .ToMessage<ResultSaved>(message => message.CommentId);
    }

    public Task Handle(SubmitComment message, IMessageHandlerContext context)
    {
        Data.CurrentState = CommentModerationState.StoringInitialState;
        Data.CommentId = message.CommentId;
        Data.CommentText = message.CommentText;
        Data.ApprovedByAi = false;
        Data.ApprovedByHuman = false;

        return context.Send(new StoreInitialCommentState
        {
            CommentId = Data.CommentId,
            CommentText = Data.CommentText!,
        });
    }

    public Task Handle(CommentInitialStateStored message, IMessageHandlerContext context)
    {
        if (Data.CurrentState != CommentModerationState.StoringInitialState)
        {
            throw new InvalidOperationException($"Invalid state: {Data.CurrentState}");
        }

        Data.CurrentState = CommentModerationState.ReviewingWithAi;
        return context.Send(new ReviewCommentWithAi
        {
            CommentId = Data.CommentId,
            CommentText = Data.CommentText!,
        });
    }

    public Task Handle(CommentApprovedByAi message, IMessageHandlerContext context)
    {
        if (Data.CurrentState != CommentModerationState.ReviewingWithAi)
        {
            throw new InvalidOperationException($"Invalid state: {Data.CurrentState}");
        }

        Data.CurrentState = CommentModerationState.SavingResult;
        Data.ApprovedByAi = true;
        return context.Send(new SaveResult
        {
            CommentId = Data.CommentId,
            ApprovedByAi = true,
            ApprovedByHuman = false,
        });
    }

    public async Task Handle(CommentRejectedByAi message, IMessageHandlerContext context)
    {
        if (Data.CurrentState != CommentModerationState.ReviewingWithAi)
        {
            throw new InvalidOperationException($"Invalid state: {Data.CurrentState}");
        }

        Data.CurrentState = CommentModerationState.PendingHumanReview;
        Data.ApprovedByAi = false;
        await context.Send(new SetCommentPendingHumanReview
        {
            CommentId = Data.CommentId,
        });
        await context.Send(new EmailModerator
        {
            CommentId = Data.CommentId,
        });
    }

    public Task Handle(CommentApprovedByHuman message, IMessageHandlerContext context)
    {
        if (Data.CurrentState != CommentModerationState.PendingHumanReview)
        {
            throw new InvalidOperationException($"Invalid state: {Data.CurrentState}");
        }

        Data.CurrentState = CommentModerationState.SavingResult;
        Data.ApprovedByHuman = true;
        return context.Send(new SaveResult
        {
            CommentId = Data.CommentId,
            ApprovedByAi = false,
            ApprovedByHuman = true,
        });
    }

    public Task Handle(CommentRejectedByHuman message, IMessageHandlerContext context)
    {
        if (Data.CurrentState != CommentModerationState.PendingHumanReview)
        {
            throw new InvalidOperationException($"Invalid state: {Data.CurrentState}");
        }

        Data.CurrentState = CommentModerationState.SavingResult;
        Data.ApprovedByHuman = false;

        return context.Send(new SaveResult
        {
            CommentId = Data.CommentId,
            ApprovedByAi = false,
            ApprovedByHuman = false,
        });
    }

    public Task Handle(ResultSaved message, IMessageHandlerContext context)
    {
        if (Data.CurrentState != CommentModerationState.SavingResult)
        {
            throw new InvalidOperationException($"Invalid state: {Data.CurrentState}");
        }

        Data.CurrentState = CommentModerationState.Completed;
        MarkAsComplete();
        return Task.CompletedTask;
    }
}
