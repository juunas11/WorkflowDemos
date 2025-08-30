using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using static WorkflowDemos.DurableFunctions.ContentModerationWorkflowFunctions;

namespace WorkflowDemos.DurableFunctions;

public class ManualModerationWorkflowFunctions(
    IEmailService emailService,
    IDataStorageService dataStorageService,
    IConfiguration configuration)
{
    [Function(nameof(ManualModerationWorkflow))]
    public async Task<Comment> ManualModerationWorkflow(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Get the comment to be moderated
        var comment = context.GetInput<Comment>()!;

        if (comment.ApprovedByAi)
        {
            // Already approved by AI, no need for manual review
            return comment;
        }

        // Update comment state to waiting for manual approval
        await context.CallActivityAsync(nameof(UpdateCommentWaitingManualApproval), new UpdateCommentWaitingManualApprovalInput
        {
            CommentId = comment.Id,
            OrchestratorInstanceId = context.InstanceId,
        });

        // Send email to moderator
        await context.CallActivityAsync(nameof(EmailModerator), comment.Id);

        try
        {
            var approvalTask = context.WaitForExternalEvent<bool>("Approve", TimeSpan.FromDays(1));
            var rejectTask = context.WaitForExternalEvent<bool>("Reject", TimeSpan.FromDays(1));

            var winner = await Task.WhenAny(approvalTask, rejectTask);
            comment.ApprovedByHuman = winner == approvalTask && approvalTask.Result;
        }
        catch (TimeoutException)
        {
            // Timeout occurred
        }

        if (comment.ApprovedByHuman)
        {
            await context.CallActivityAsync(nameof(SetCommentApprovedByHuman), comment.Id);
        }
        else
        {
            // Timed out or rejected
            await context.CallActivityAsync(nameof(SetCommentRejected), comment.Id);
        }

        return comment;
    }

    [Function(nameof(EmailModerator))]
    public async Task EmailModerator(
        [ActivityTrigger] string commentId,
        FunctionContext executionContext)
    {
        await emailService.SendEmailAsync(
            configuration["ModeratorEmail"]!,
            "Manual Moderation Required",
            $"A comment requires manual moderation. Please review it in the moderation portal: {configuration["ModerationPortalUrl"]}.\n\nPartition key: DurableFunctions\n\nRow key: {commentId}");
    }

    [Function(nameof(UpdateCommentWaitingManualApproval))]
    public async Task UpdateCommentWaitingManualApproval(
        [ActivityTrigger] UpdateCommentWaitingManualApprovalInput input,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync("DurableFunctions", input.CommentId);
        entity!.State = ModerationState.PendingHumanReview;
        entity.ManualApprovalWorkflowId = input.OrchestratorInstanceId;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function(nameof(SetCommentApprovedByHuman))]
    public async Task SetCommentApprovedByHuman(
        [ActivityTrigger] string commentId,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync("DurableFunctions", commentId);
        entity!.State = ModerationState.ApprovedByHuman;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function(nameof(SetCommentRejected))]
    public async Task SetCommentRejected(
        [ActivityTrigger] string commentId,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync("DurableFunctions", commentId);
        entity!.State = ModerationState.Rejected;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function("ManualModerationWorkflow_Approve")]
    public static async Task<HttpResponseData> ManualModerationWorkflowApprove(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var requestBody = await req.ReadFromJsonAsync<ApproveOrRejectInput>();

        await client.RaiseEventAsync(
            requestBody!.InstanceId,
            "Approve",
            true);

        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("ManualModerationWorkflow_Reject")]
    public static async Task<HttpResponseData> ManualModerationWorkflowReject(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var requestBody = await req.ReadFromJsonAsync<ApproveOrRejectInput>();
        await client.RaiseEventAsync(
            requestBody!.InstanceId,
            "Reject",
            true);
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    public class UpdateCommentWaitingManualApprovalInput
    {
        public required string CommentId { get; set; }
        public required string OrchestratorInstanceId { get; set; }
    }

    public class ApproveOrRejectInput
    {
        public required string InstanceId { get; set; }
    }
}
