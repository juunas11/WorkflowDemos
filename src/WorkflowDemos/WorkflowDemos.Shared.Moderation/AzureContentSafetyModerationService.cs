using Azure.AI.ContentSafety;

namespace WorkflowDemos.Shared.Moderation;

public class AzureContentSafetyModerationService(ContentSafetyClient contentSafetyClient) : IContentModerationService
{
    public async Task<bool> CheckCommentAsync(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return true;
        }

        var response = await contentSafetyClient.AnalyzeTextAsync(comment);
        var result = response.Value;
        var hate = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Hate)?.Severity ?? 0;
        var selfHarm = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.SelfHarm)?.Severity ?? 0;
        var sexual = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Sexual)?.Severity ?? 0;
        var violence = result.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Violence)?.Severity ?? 0;

        // TODO: Adjust (this is very tight)
        return hate == 0 &&
            selfHarm == 0 &&
            sexual == 0 &&
            violence == 0;
    }
}
