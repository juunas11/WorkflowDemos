
namespace WorkflowDemos.Shared.Moderation;

public interface IContentModerationService
{
    Task<bool> CheckCommentAsync(string comment);
}
