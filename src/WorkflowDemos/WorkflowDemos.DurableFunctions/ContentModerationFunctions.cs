using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkflowDemos.DataStorage;
using WorkflowDemos.Email;
using WorkflowDemos.Moderation;

namespace WorkflowDemos.DurableFunctions;

public class ContentModerationFunctions(
    IEmailService emailService,
    IContentModerationService contentModerationService,
    IDataStorageService dataStorageService,
    IConfiguration configuration)
{
    [Function(nameof(MainOrchestrator))]
    public async Task<List<int>> MainOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ContentModerationFunctions));

        // Get set of comments as input
        var input = context.GetInput<OrchestrationInput>();
        if (input?.Comments == null || input.Comments.Count == 0)
        {
            logger.LogInformation("No comments provided for moderation.");
            return new List<int>();
        }

        logger.LogInformation("Received {CommentCount} comments for moderation.", input.Comments.Count);

        // Run all comments through AI content filtering
        var contentFilteringTasks = new List<Task<bool>>();
        for (int i = 0; i < input.Comments.Count; i++)
        {
            // Call an activity function to check each comment
            var contentFilteringTask = context.CallActivityAsync<bool>(nameof(CheckComment), input.Comments[i]);
            contentFilteringTasks.Add(contentFilteringTask);
        }

        // Wait for all content filtering tasks to complete
        var results = await Task.WhenAll(contentFilteringTasks);
        logger.LogInformation("Content filtering completed for all comments.");

        var acceptedIndices = results
            .Select((result, index) => new { result, index })
            .Where(x => x.result)
            .Select(x => x.index)
            .ToList();
        logger.LogInformation("Accepted comments indices by AI: {AcceptedIndices}", string.Join(", ", acceptedIndices));

        // Any comments that get a negative result go into a manual approval process (email moderator, wait for response)
        var manualApprovalTasks = new List<(int Index, Task<bool> Task)>();
        for (int i = 0; i < input.Comments.Count; i++)
        {
            if (!results[i])
            {
                // Call a sub-orchestrator for manual approval
                var manualApprovalTask = context.CallSubOrchestratorAsync<bool>(nameof(ManualModerationOrchestrator), input.Comments[i]);
                manualApprovalTasks.Add((i, manualApprovalTask));
            }
        }

        // Wait for all manual approval tasks to complete
        var manualResults = await Task.WhenAll(manualApprovalTasks.Select(x => x.Task));
        logger.LogInformation("Manual moderation completed for comments needing approval.");
        acceptedIndices.AddRange(
            manualResults
                .Select((result, index) => new { result, index })
                .Where(x => x.result)
                .Select(x => manualApprovalTasks[x.index].Index));
        logger.LogInformation("Accepted comments indices after manual moderation: {AcceptedIndices}", string.Join(", ", acceptedIndices));

        // Add accepted comments to a database / Storage Table
        await context.CallActivityAsync(nameof(StoreAcceptedComments), input.Comments.Where((_, index) => acceptedIndices.Contains(index)).ToList());

        // Return indices of accepted comments
        return acceptedIndices;
    }

    [Function(nameof(CheckComment))]
    public async Task<bool> CheckComment(
        [ActivityTrigger] string comment,
        FunctionContext executionContext)
    {
        return await contentModerationService.CheckCommentAsync(comment);
    }

    [Function(nameof(ManualModerationOrchestrator))]
    public async Task<bool> ManualModerationOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ManualModerationOrchestrator));

        // Get the comment to be moderated
        var comment = context.GetInput<string>();
        logger.LogInformation("Starting manual moderation for comment: {Comment}", comment);

        // Simulate sending an email to a moderator and waiting for a response
        await context.CallActivityAsync(nameof(EmailModerator), context.InstanceId);
        logger.LogInformation("Manual moderation result for comment '{Comment}': {IsApproved}", comment, isApproved);

        // TODO: Handle timeout error
        var isApproved = await context.WaitForExternalEvent<bool>("ManualModerationResponse", TimeSpan.FromDays(1));

        return isApproved;
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

    [Function(nameof(StoreAcceptedComments))]
    public async Task StoreAcceptedComments(
        [ActivityTrigger] List<string> acceptedComments,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(StoreAcceptedComments));
        logger.LogInformation("Storing {Count} accepted comments.", acceptedComments.Count);

        foreach (var comment in acceptedComments)
        {
            var entity = new WorkflowEntity
            {
                PartitionKey = "DurableFunctions",
                RowKey = instanceId,
                Comment = comment,
                ApprovalUrl = "http://localhost:7260/api/ManualModerationOrchestrator_SendManualModerationResponse",
                ApproveRequestBody = "",
                RejectRequestBody = "",
                State = WorkflowState.Approved,
            };
            await dataStorageService.CreateEntityAsync(entity);
            logger.LogInformation("Stored comment: {Comment}", comment);
        }
    }

    [Function("MainOrchestrator_HttpStart")]
    public static async Task<HttpResponseData> MainOrchestratorHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger("MainOrchestrator_HttpStart");

        var requestBody = await req.ReadFromJsonAsync<OrchestrationInput>();

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

    public class OrchestrationInput
    {
        public required List<string> Comments { get; set; }
    }

    public class ManualModerationResponse
    {
        public required bool IsApproved { get; set; }
        public required string InstanceId { get; set; }
    }
}