namespace WorkflowDemos.Shared.Email;

public interface IEmailService
{
    Task SendModerationRequiredEmailAsync(string partitionKey, string rowKey);
}
