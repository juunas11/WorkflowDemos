
namespace WorkflowDemos.Moderation;

public interface IContentModerationService
{
    Task<bool> CheckCommentAsync(string comment);
}
