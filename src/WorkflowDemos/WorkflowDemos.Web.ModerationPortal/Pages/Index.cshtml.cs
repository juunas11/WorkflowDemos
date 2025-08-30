using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Web.ModerationPortal.Services;

namespace WorkflowDemos.Web.ModerationPortal.Pages
{
    public class IndexModel(
        IDataStorageService dataStorageService,
        OrchestratorManager orchestratorManager) : PageModel
    {
        public List<CommentEntity> Items { get; set; } = [];

        public async Task OnGet()
        {
            Items = await dataStorageService.GetAllEntitiesAsync();
        }

        public async Task<IActionResult> OnPostSubmitComment(string orchestrator, string comments)
        {
            var commentsArray = comments.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);

            await orchestratorManager.SubmitCommentsAsync(orchestrator, commentsArray);
            await Task.Delay(1000);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApprove(string partitionKey, string workflowId)
        {
            await orchestratorManager.ApproveAsync(partitionKey, workflowId);
            await Task.Delay(1000);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReject(string partitionKey, string workflowId)
        {
            await orchestratorManager.RejectAsync(partitionKey, workflowId);
            await Task.Delay(1000);
            return RedirectToPage();
        }
    }
}
