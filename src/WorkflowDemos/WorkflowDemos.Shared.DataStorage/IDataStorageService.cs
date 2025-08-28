namespace WorkflowDemos.Shared.DataStorage;

public interface IDataStorageService
{
    Task CreateEntityAsync(WorkflowEntity entity);
    Task<WorkflowEntity?> GetEntityAsync(string partitionKey, string rowKey);
    Task UpdateEntityAsync(WorkflowEntity entity);
    Task<List<WorkflowEntity>> GetAllEntitiesAsync();
}