using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace WorkflowDemos.DurableFunctions;

public static class ContentModerationFunctions
{
    [Function(nameof(MainOrchestrator))]
    public static async Task<List<int>> MainOrchestrator(
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
    public static bool CheckComment(
        [ActivityTrigger] string comment,
        FunctionContext executionContext)
    {
        // TODO
        return true;
    }

    [Function(nameof(ManualModerationOrchestrator))]
    public static async Task<bool> ManualModerationOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(ManualModerationOrchestrator));

        // Get the comment to be moderated
        var comment = context.GetInput<string>();
        logger.LogInformation("Starting manual moderation for comment: {Comment}", comment);

        // Simulate sending an email to a moderator and waiting for a response
        await context.CallActivityAsync(nameof(EmailModerator), approvalLink);
        logger.LogInformation("Manual moderation result for comment '{Comment}': {IsApproved}", comment, isApproved);

        // TODO: Handle timeout error
        var isApproved = await context.WaitForExternalEvent<bool>("ManualModerationResponse", TimeSpan.FromDays(1));

        return isApproved;
    }

    [Function(nameof(EmailModerator))]
    public static void EmailModerator(
        [ActivityTrigger] string comment,
        FunctionContext executionContext)
    {
    }

    [Function(nameof(StoreAcceptedComments))]
    public static async Task StoreAcceptedComments(
        [ActivityTrigger] List<string> acceptedComments,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(StoreAcceptedComments));
        logger.LogInformation("Storing {Count} accepted comments.", acceptedComments.Count);

    }

    [Function("MainOrchestrator_HttpStart")]
    public static async Task<HttpResponseData> HttpStart(
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

    public class OrchestrationInput
    {
        public required List<string> Comments { get; set; }
    }
}