using Azure;
using Azure.Data.Tables;

namespace WorkflowDemos.Shared.DataStorage;

public class TableStorageService(
    TableServiceClient tableServiceClient) : IDataStorageService
{
    private readonly TableClient tableClient = tableServiceClient.GetTableClient("WorkflowEntities");
    private bool isInitialized;

    public async Task CreateEntityAsync(WorkflowEntity entity)
    {
        await EnsureTableExistsAsync();
        await tableClient.AddEntityAsync(entity);
    }

    public async Task UpdateEntityAsync(WorkflowEntity entity)
    {
        await EnsureTableExistsAsync();
        await tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
    }

    public async Task<WorkflowEntity?> GetEntityAsync(string partitionKey, string rowKey)
    {
        await EnsureTableExistsAsync();
        try
        {
            return await tableClient.GetEntityAsync<WorkflowEntity>(partitionKey, rowKey);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null; // Entity not found
        }
    }

    public async Task<List<WorkflowEntity>> GetAllEntitiesAsync()
    {
        await EnsureTableExistsAsync();

        var entities = new List<WorkflowEntity>();
        await foreach (var entity in tableClient.QueryAsync<WorkflowEntity>())
        {
            entities.Add(entity);
        }

        return entities.OrderByDescending(e => e.Timestamp).ToList();
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
