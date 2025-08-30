using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.DurableFunctions;

public class ContentModerationWorkflowFunctions(
    IContentModerationService contentModerationService,
    IDataStorageService dataStorageService)
{
    [Function(nameof(ContentModerationWorkflow))]
    public async Task<List<string>> ContentModerationWorkflow(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Get set of comments as input
        var input = context.GetInput<WorkflowInput>();
        if (input?.Comments == null || input.Comments.Count == 0)
        {
            return new List<string>();
        }

        var comments = input.Comments;
        // Set ID for all comments
        foreach (var comment in comments)
        {
            comment.Id = context.NewGuid().ToString();
        }

        var storeInitialStateTasks = comments.ConvertAll(comment =>
            context.CallActivityAsync(nameof(StoreInitialCommentState), comment));
        await Task.WhenAll(storeInitialStateTasks);

        // Run all comments through AI content filtering
        var contentFilteringTasks = comments.ConvertAll(comment => context.CallActivityAsync<Comment>(nameof(CheckComment), comment));
        // Wait for all content filtering tasks to complete
        comments = (await Task.WhenAll(contentFilteringTasks)).ToList();

        var updateCommentStateTasks = comments
            .Where(comment => comment.ApprovedByAi)
            .Select(comment =>
                context.CallActivityAsync(nameof(SetCommentApprovedByAi), comment.Id))
            .ToList();
        await Task.WhenAll(updateCommentStateTasks);

        // Any comments that get a negative result go into a manual approval process (email moderator, wait for response)
        var manualApprovalTasks = comments.ConvertAll(comment =>
        {
            if (comment.ApprovedByAi)
            {
                // Already approved by AI, no need for manual review
                return Task.FromResult(comment);
            }

            return context.CallSubOrchestratorAsync<Comment>(nameof(ManualModerationWorkflowFunctions.ManualModerationWorkflow), comment);
        });
        // Wait for all manual approval tasks to complete
        comments = (await Task.WhenAll(manualApprovalTasks)).ToList();

        // Return accepted comments
        return comments
            .Where(comment => comment.ApprovedByAi || comment.ApprovedByHuman)
            .Select(comment => comment.Text)
            .ToList();
    }

    [Function(nameof(StoreInitialCommentState))]
    public async Task StoreInitialCommentState(
        [ActivityTrigger] Comment comment,
        FunctionContext executionContext)
    {
        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = "DurableFunctions",
            RowKey = comment.Id,
            Comment = comment.Text,
            State = ModerationState.PendingAiReview,
            ManualApprovalWorkflowId = null,
        });
    }

    [Function(nameof(CheckComment))]
    public async Task<Comment> CheckComment(
        [ActivityTrigger] Comment comment,
        FunctionContext executionContext)
    {
        comment.ApprovedByAi = await contentModerationService.CheckCommentAsync(comment.Text);
        return comment;
    }

    [Function(nameof(SetCommentApprovedByAi))]
    public async Task SetCommentApprovedByAi(
        [ActivityTrigger] string commentId,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync("DurableFunctions", commentId);
        entity!.State = ModerationState.ApprovedByAi;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function("ContentModerationWorkflow_HttpStart")]
    public static async Task<HttpResponseData> ContentModerationWorkflowHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var requestBody = await req.ReadFromJsonAsync<WorkflowInput>();

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ContentModerationWorkflow),
            requestBody);

        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    public class WorkflowInput
    {
        public required List<Comment> Comments { get; set; }
    }
}
