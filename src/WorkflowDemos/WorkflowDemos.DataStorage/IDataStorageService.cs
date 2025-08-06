namespace WorkflowDemos.DataStorage;

public interface IDataStorageService
{
    Task SaveEntityAsync(WorkflowEntity entity);
}