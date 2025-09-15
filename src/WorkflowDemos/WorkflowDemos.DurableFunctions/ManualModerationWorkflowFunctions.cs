using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using WorkflowDemos.DurableFunctions.Dtos;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;

namespace WorkflowDemos.DurableFunctions;

public class ManualModerationWorkflowFunctions(
    IEmailService emailService,
    IDataStorageService dataStorageService)
{
    public const string ModerationDecisionEventName = "ModerationDecision";

    [Function(nameof(ManualModerationWorkflow))]
    public async Task<ManualModerationWorkflowOutput> ManualModerationWorkflow(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Get the comment to be moderated
        var input = context.GetInput<ManualModerationWorkflowInput>()!;
        var comment = input.Comment;

        if (comment.ApprovedByAi)
        {
            // Already approved by AI, no need for manual review
            return new ManualModerationWorkflowOutput(comment);
        }

        // Update comment state to waiting for manual approval
        await context.CallActivityAsync(nameof(UpdateCommentWaitingManualApproval), new UpdateCommentWaitingManualApprovalInput(comment.Id, context.InstanceId));

        // Send email to moderator
        await context.CallActivityAsync(nameof(EmailModerator), new EmailModeratorInput(comment.Id));

        try
        {
            comment.ApprovedByHuman = await context.WaitForExternalEvent<bool>(ModerationDecisionEventName, TimeSpan.FromDays(1));
        }
        catch (TimeoutException)
        {
            // Timeout occurred
        }

        if (comment.ApprovedByHuman)
        {
            await context.CallActivityAsync(nameof(SetCommentApprovedByHuman), new SetCommentApprovedByHumanInput(comment.Id));
        }
        else
        {
            // Timed out or rejected
            await context.CallActivityAsync(nameof(SetCommentRejected), new SetCommentRejectedInput(comment.Id));
        }

        return new ManualModerationWorkflowOutput(comment);
    }

    [Function(nameof(EmailModerator))]
    public async Task EmailModerator(
        [ActivityTrigger] EmailModeratorInput input,
        FunctionContext executionContext)
    {
        await emailService.SendModerationRequiredEmailAsync(Constants.PartitionKey, input.CommentId);
    }

    [Function(nameof(UpdateCommentWaitingManualApproval))]
    public async Task UpdateCommentWaitingManualApproval(
        [ActivityTrigger] UpdateCommentWaitingManualApprovalInput input,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync(Constants.PartitionKey, input.CommentId);
        entity!.State = ModerationState.PendingHumanReview;
        entity.ManualApprovalWorkflowId = input.OrchestratorInstanceId;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function(nameof(SetCommentApprovedByHuman))]
    public async Task SetCommentApprovedByHuman(
        [ActivityTrigger] SetCommentApprovedByHumanInput input,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync(Constants.PartitionKey, input.CommentId);
        entity!.State = ModerationState.ApprovedByHuman;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function(nameof(SetCommentRejected))]
    public async Task SetCommentRejected(
        [ActivityTrigger] SetCommentRejectedInput input,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync(Constants.PartitionKey, input.CommentId);
        entity!.State = ModerationState.Rejected;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function("ManualModerationWorkflow_ModerationDecision")]
    public static async Task<HttpResponseData> ManualModerationWorkflowModerationDecision(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var requestBody = await req.ReadFromJsonAsync<ManualModerationDecisionInput>();

        await client.RaiseEventAsync(
            requestBody!.InstanceId,
            ModerationDecisionEventName,
            requestBody.IsApproved);

        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }
}
