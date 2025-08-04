
namespace WorkflowDemos.Moderation;

public class MockContentModerationService : IContentModerationService
{
    public Task<bool> CheckCommentAsync(string comment)
    {
        return Task.FromResult(Random.Shared.Next(0, 2) == 0);
    }
}