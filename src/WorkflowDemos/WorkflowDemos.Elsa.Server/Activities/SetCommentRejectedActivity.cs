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
    DisplayName = "Set comment rejected",
    Description = "Sets the comment state to rejected in the data storage service.")]
public class SetCommentRejectedActivity : CodeActivity
{
    [Input(
        Description = "The comment to update")]
    public Input<Comment> Comment { get; set; } = null!;

    [Output(
        Description = "The updated comment")]
    public Output<Comment> UpdatedComment { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var dataStorageService = context.GetRequiredService<IDataStorageService>();
        var comment = Comment.Get(context);

        var entity = await dataStorageService.GetEntityAsync(Constants.StoragePartitionKey, comment.Id);
        if (entity == null)
        {
            throw new InvalidOperationException("Could not find workflow entity");
        }

        entity.State = ModerationState.Rejected;
        await dataStorageService.UpdateEntityAsync(entity);

        comment.ApprovedByHuman = false;
        UpdatedComment.Set(context, comment);
    }
}