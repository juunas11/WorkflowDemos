using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using WorkflowDemos.DurableFunctions.Dtos;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Moderation;

namespace WorkflowDemos.DurableFunctions;

public class ContentModerationWorkflowFunctions(
    IContentModerationService contentModerationService,
    IDataStorageService dataStorageService)
{
    [Function(nameof(ContentModerationWorkflow))]
    public async Task<ContentModerationWorkflowOutput> ContentModerationWorkflow(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        // Get set of comments as input
        var input = context.GetInput<ContentModerationWorkflowInput>();
        if (input?.Comments == null || input.Comments.Count == 0)
        {
            return new ContentModerationWorkflowOutput(new List<string>());
        }

        var comments = input.Comments;
        // Set ID for all comments
        foreach (var comment in comments)
        {
            comment.Id = context.NewGuid().ToString();
        }

        var storeInitialStateTasks = comments.ConvertAll(comment =>
            context.CallActivityAsync(nameof(StoreInitialCommentState), new StoreInitialCommentStateInput(comment.Id, comment.Text)));
        await Task.WhenAll(storeInitialStateTasks);

        // Run all comments through AI content filtering
        var contentFilteringTasks = comments.ConvertAll(comment => context.CallActivityAsync<CheckCommentOutput>(nameof(CheckComment), new CheckCommentInput(comment)));
        // Wait for all content filtering tasks to complete
        comments = (await Task.WhenAll(contentFilteringTasks))
            .Select(x => x.Comment)
            .ToList();

        var updateCommentStateTasks = comments
            .Where(comment => comment.ApprovedByAi)
            .Select(comment =>
                context.CallActivityAsync(nameof(SetCommentApprovedByAi), new SetCommentApprovedByAiInput(comment.Id)))
            .ToList();
        await Task.WhenAll(updateCommentStateTasks);

        // Any comments that get a negative result go into a manual approval process (email moderator, wait for response)
        var manualApprovalTasks = comments.ConvertAll(comment =>
        {
            if (comment.ApprovedByAi)
            {
                // Already approved by AI, no need for manual review
                return Task.FromResult(new ManualModerationWorkflowOutput(comment));
            }

            return context.CallSubOrchestratorAsync<ManualModerationWorkflowOutput>(
                nameof(ManualModerationWorkflowFunctions.ManualModerationWorkflow),
                new ManualModerationWorkflowInput(comment));
        });
        // Wait for all manual approval tasks to complete
        comments = (await Task.WhenAll(manualApprovalTasks))
            .Select(x => x.Comment)
            .ToList();

        // Return accepted comments
        return new ContentModerationWorkflowOutput(comments
            .Where(comment => comment.ApprovedByAi || comment.ApprovedByHuman)
            .Select(comment => comment.Text)
            .ToList());
    }

    [Function(nameof(StoreInitialCommentState))]
    public async Task StoreInitialCommentState(
        [ActivityTrigger] StoreInitialCommentStateInput input,
        FunctionContext executionContext)
    {
        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = Constants.PartitionKey,
            RowKey = input.CommentId,
            Comment = input.CommentText,
            State = ModerationState.PendingAiReview,
            ManualApprovalWorkflowId = null,
        });
    }

    [Function(nameof(CheckComment))]
    public async Task<CheckCommentOutput> CheckComment(
        [ActivityTrigger] CheckCommentInput input,
        FunctionContext executionContext)
    {
        var comment = input.Comment;
        comment.ApprovedByAi = await contentModerationService.CheckCommentAsync(comment.Text);
        return new CheckCommentOutput(comment);
    }

    [Function(nameof(SetCommentApprovedByAi))]
    public async Task SetCommentApprovedByAi(
        [ActivityTrigger] SetCommentApprovedByAiInput input,
        FunctionContext executionContext)
    {
        var entity = await dataStorageService.GetEntityAsync(Constants.PartitionKey, input.CommentId);
        entity!.State = ModerationState.ApprovedByAi;
        await dataStorageService.UpdateEntityAsync(entity);
    }

    [Function("ContentModerationWorkflow_HttpStart")]
    public static async Task<HttpResponseData> ContentModerationWorkflowHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        FunctionContext executionContext)
    {
        var requestBody = await req.ReadFromJsonAsync<ContentModerationWorkflowInput>();

        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
            nameof(ContentModerationWorkflow),
            requestBody);

        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }
}
