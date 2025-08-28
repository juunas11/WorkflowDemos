using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.DurableFunctions;

public class ContentModerationFunctions(
    IEmailService emailService,
    IContentModerationService contentModerationService,
    IDataStorageService dataStorageService,
    IConfiguration configuration)
{
    [Function(nameof(MainOrchestrator))]
    public async Task<List<string>> MainOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ContentModerationFunctions));

        // Get set of comments as input
        var input = context.GetInput<WorkflowInput>();
        if (input?.Comments == null || input.Comments.Count == 0)
        {
            logger.LogInformation("No comments provided for moderation.");
            return new List<string>();
        }

        var comments = input.Comments;
        logger.LogInformation("Received {CommentCount} comments for moderation.", comments.Count);

        // Run all comments through AI content filtering
        var contentFilteringTasks = comments.ConvertAll(comment => context.CallActivityAsync<Comment>(nameof(CheckComment), comment));
        // Wait for all content filtering tasks to complete
        comments = (await Task.WhenAll(contentFilteringTasks)).ToList();
        logger.LogInformation("Content filtering completed for all comments.");

        // Any comments that get a negative result go into a manual approval process (email moderator, wait for response)
        var manualApprovalTasks = comments.ConvertAll(comment => context.CallSubOrchestratorAsync<Comment>(nameof(ManualModerationOrchestrator), comment));
        // Wait for all manual approval tasks to complete
        comments = (await Task.WhenAll(manualApprovalTasks)).ToList();
        logger.LogInformation("Manual moderation completed for comments needing approval.");

        // Return accepted comments
        return comments
            .Where(comment => comment.ApprovedByAi || comment.ApprovedByHuman)
            .Select(comment => comment.Text)
            .ToList();
    }

    [Function(nameof(CheckComment))]
    public async Task<bool> CheckComment(
        [ActivityTrigger] string comment,
        FunctionContext executionContext)
    {
        return await contentModerationService.CheckCommentAsync(comment);
    }

    [Function(nameof(ManualModerationOrchestrator))]
    public async Task<Comment> ManualModerationOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ManualModerationOrchestrator));

        // Get the comment to be moderated
        var comment = context.GetInput<Comment>()!;
        logger.LogInformation("Starting manual moderation for comment: {Comment}", comment.Text);

        if (comment.ApprovedByAi)
        {
            // Already approved by AI, no need for manual review
            return comment;
        }

        // Simulate sending an email to a moderator and waiting for a response
        await context.CallActivityAsync(nameof(EmailModerator), context.InstanceId);

        // TODO: Handle timeout error
        var isApproved = await context.WaitForExternalEvent<bool>("ManualModerationResponse", TimeSpan.FromDays(1));
        logger.LogInformation("Manual moderation result for comment '{Comment}': {IsApproved}", comment.Text, isApproved);

        comment.ApprovedByHuman = isApproved;
        return comment;
    }

    [Function(nameof(EmailModerator))]
    public async Task EmailModerator(
        [ActivityTrigger] string instanceId,
        FunctionContext executionContext)
    {
        await emailService.SendEmailAsync(
            configuration["ModeratorEmail"]!,
            "Manual Moderation Required",
            $"A comment requires manual moderation. Please review it in the moderation portal: https://example.com/moderation.\n\nPartition key: DurableFunctions\n\nRow key: {instanceId}");
    }

    [Function(nameof(StoreComments))]
    public async Task StoreComments(
        [ActivityTrigger] List<CommentIdAndText> comments,
        FunctionContext executionContext)
    {
        foreach (var comment in comments)
        {
            await dataStorageService.CreateEntityAsync(new WorkflowEntity
            {
                PartitionKey = "DurableFunctions",
                RowKey = comment.Id.ToString(),
                Comment = comment.Text,
                State = WorkflowState.WaitingApproval,
                //ApprovalUrl = "http://localhost:7260/api/ManualModerationOrchestrator_SendManualModerationResponse",
                //ApproveRequestBody = $$"""
                //{
                //    "IsApproved": true,
                //    "CommentId": "{CommentId}"
                //}
                //""",
                //RejectRequestBody = """
                //{
                //}
                //"""
            });
        }
    }

    [Function(nameof(MarkCommentsAccepted))]
    public async Task MarkCommentsAccepted(
        [ActivityTrigger] List<CommentIdAndText> acceptedComments,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(MarkCommentsAccepted));
        logger.LogInformation("Marking {Count} comments as accepted.", acceptedComments.Count);

        foreach (var comment in acceptedComments)
        {
            var entity = await dataStorageService.GetEntityAsync("DurableFunctions", comment.Id.ToString());
            if (entity == null)
            {
                logger.LogWarning("No entity found for comment ID: {CommentId}", comment.Id);
                continue;
            }

            entity.State = WorkflowState.Approved;
            await dataStorageService.UpdateEntityAsync(entity);
            logger.LogInformation("Marked comment '{CommentText}' (ID: {CommentId}) as accepted.", comment.Text, comment.Id);
        }
    }

    [Function("MainOrchestrator_HttpStart")]
    public static async Task<HttpResponseData> MainOrchestratorHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("MainOrchestrator_HttpStart");

        var requestBody = await req.ReadFromJsonAsync<WorkflowInput>();

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(MainOrchestrator),
            requestBody);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        // Returns an HTTP 202 response with an instance management payload.
        // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    [Function("ManualModerationOrchestrator_SendManualModerationResponse")]
    public static async Task<HttpResponseData> ManualModerationOrchestratorSendManualModerationResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("ManualModerationOrchestrator_SendManualModerationResponse");

        var requestBody = await req.ReadFromJsonAsync<ManualModerationResponse>();

        await client.RaiseEventAsync(
            requestBody!.InstanceId,
            "ManualModerationResponse",
            requestBody.IsApproved);

        logger.LogInformation("Raised event to orchestration with ID = '{instanceId}'.", requestBody.InstanceId);

        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    public class WorkflowInput
    {
        public required List<Comment> Comments { get; set; }
    }

    public class Comment
    {
        public required string Text { get; set; }
        public bool ApprovedByAi { get; set; }
        public bool ApprovedByHuman { get; set; }
    }

    //public class ManualModerationResponse
    //{
    //    public required bool IsApproved { get; set; }
    //    public required string InstanceId { get; set; }
    //    public required string CommentId { get; set; }
    //}

    //public class CommentIdAndText
    //{
    //    public required Guid Id { get; set; }
    //    public required string Text { get; set; }
    //}
}