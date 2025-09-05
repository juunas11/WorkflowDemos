using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using WorkflowDemos.Elsa.Server.Types;
using WorkflowDemos.Shared.DataStorage;

namespace WorkflowDemos.Elsa.Server.Activities;

[Activity(
    Namespace = "ContentModeration",
    Category = "Moderation/Storage",
    DisplayName = "Store initial comment state",
    Description = "Stores the initial state of a comment in the data storage service.")]
public class StoreInitialCommentStateActivity : CodeActivity
{
    [Input(
        Description = "The comment to store")]
    public Input<Comment> Comment { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var dataStorageService = context.GetRequiredService<IDataStorageService>();

        var comment = Comment.Get(context);

        await dataStorageService.CreateEntityAsync(new CommentEntity
        {
            PartitionKey = Constants.StoragePartitionKey,
            RowKey = comment.Id,
            Comment = comment.Text,
            ManualApprovalWorkflowId = null,
            State = ModerationState.PendingAiReview,
        });
    }
}