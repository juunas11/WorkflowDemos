using Azure.Data.Tables;

namespace WorkflowDemos.DataStorage;

public class TableStorageService(
    TableServiceClient tableServiceClient) : IDataStorageService
{
    private readonly TableClient tableClient = tableServiceClient.GetTableClient("WorkflowEntities");
    private bool isInitialized;

    public async Task SaveEntityAsync(WorkflowEntity entity)
    {
        await EnsureTableExistsAsync();
        // TODO
    }

    private async ValueTask EnsureTableExistsAsync()
    {
        if (isInitialized)
        {
            return;
        }

        // Create the table if it does not exist
        await tableClient.CreateIfNotExistsAsync();
        isInitialized = true;
    }
}
