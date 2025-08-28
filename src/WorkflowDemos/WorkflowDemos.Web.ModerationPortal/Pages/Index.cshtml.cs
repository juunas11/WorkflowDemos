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
        public List<WorkflowEntity> Items { get; set; } = [];

        public async Task OnGet()
        {
            Items = await dataStorageService.GetAllEntitiesAsync();
        }

        public async Task<IActionResult> OnPostApprove(string partitionKey, string rowKey)
        {
            await orchestratorEventSender.ApproveAsync(partitionKey, rowKey);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReject(string partitionKey, string rowKey)
        {
            await orchestratorEventSender.RejectAsync(partitionKey, rowKey);
            return RedirectToPage();
        }
    }
}
