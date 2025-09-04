using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using WorkflowDemos.Elsa.Server.Types;
using WorkflowDemos.Shared.Email;

namespace WorkflowDemos.Elsa.Server.Activities;

[Activity(
    Namespace = "ContentModeration",
    Category = "Moderation/Email",
    DisplayName = "Email moderator",
    Description = "Sends an email to the moderator indicating that a comment requires manual moderation.")]
public class EmailModeratorActivity : CodeActivity
{
    [Input(
        Description = "The comment that requires moderation.")]
    public Input<Comment> Comment { get; set; } = null!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var emailService = context.GetRequiredService<IEmailService>();
        var comment = Comment.Get(context);
        await emailService.SendModerationRequiredEmailAsync(Constants.StoragePartitionKey, comment.Id);
    }
}