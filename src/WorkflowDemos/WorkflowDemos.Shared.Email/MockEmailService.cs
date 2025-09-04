using Microsoft.Extensions.Logging;

namespace WorkflowDemos.Shared.Email;

public class MockEmailService(ILogger<MockEmailService> logger) : IEmailService
{
    public Task SendModerationRequiredEmailAsync(string partitionKey, string rowKey)
    {
        logger.LogInformation("Mock moderation required email sent for partition key '{PartitionKey}' and row key '{RowKey}'", partitionKey, rowKey);
        return Task.CompletedTask;
    }
}