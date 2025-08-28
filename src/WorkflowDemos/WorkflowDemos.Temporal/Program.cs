using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Client;
using Temporalio.Worker;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;
using WorkflowDemos.Temporal;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);
services.AddMailgunEmailService(configuration["Mailgun:FromEmail"]!, configuration["Mailgun:Domain"]!, configuration["Mailgun:ApiKey"]!);
services.AddAzureContentSafetyModeration(configuration["AzureContentSafety:Endpoint"]!, configuration["AzureContentSafety:ApiKey"]!);
services.AddTableStorageService(configuration["Storage:ConnectionString"]!);

services.AddTransient<ContentModerationActivities>();

var serviceProvider = services.BuildServiceProvider();

using var serviceScope = serviceProvider.CreateScope();

var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

using var tokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    tokenSource.Cancel();
    eventArgs.Cancel = true;
};

var activities = serviceScope.ServiceProvider.GetRequiredService<ContentModerationActivities>();
using var worker = new TemporalWorker(
    client,
    new TemporalWorkerOptions(taskQueue: "CONTENT_MODERATION_TASK_QUEUE")
        .AddAllActivities(activities)
        .AddWorkflow<ContentModerationWorkflow>()
        .AddWorkflow<ManualModerationWorkflow>()
);

Console.WriteLine("Running worker...");
try
{
    await worker.ExecuteAsync(tokenSource.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Worker cancelled");
}