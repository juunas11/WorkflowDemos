namespace WorkflowDemos.Shared.DataStorage;

public interface IDataStorageService
{
    Task CreateEntityAsync(CommentEntity entity);
    Task<CommentEntity?> GetEntityAsync(string partitionKey, string rowKey);
    Task UpdateEntityAsync(CommentEntity entity);
    Task<List<CommentEntity>> GetAllEntitiesAsync();
}