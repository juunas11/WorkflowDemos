using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Web.ModerationPortal.Services;

namespace WorkflowDemos.Web.ModerationPortal.Pages
{
    public class IndexModel(
        IDataStorageService dataStorageService,
        OrchestratorEventSender orchestratorEventSender) : PageModel
    {
        public List<CommentEntity> Items { get; set; } = [];

        public async Task OnGet()
        {
            Items = await dataStorageService.GetAllEntitiesAsync();
        }

        public async Task<IActionResult> OnPostApprove(string partitionKey, string workflowId)
        {
            await orchestratorEventSender.ApproveAsync(partitionKey, workflowId);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReject(string partitionKey, string workflowId)
        {
            await orchestratorEventSender.RejectAsync(partitionKey, workflowId);
            return RedirectToPage();
        }
    }
}
