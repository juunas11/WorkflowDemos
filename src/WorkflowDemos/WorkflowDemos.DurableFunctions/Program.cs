using DurableFunctionsMonitor.DotNetIsolated;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddMailgunEmailService(builder.Configuration["MailgunFromEmail"]!, builder.Configuration["MailgunDomain"]!, builder.Configuration["MailgunApiKey"]!, builder.Configuration["ModeratorEmail"]!, builder.Configuration["ModerationPortalUrl"]!)
    .AddAzureContentSafetyModeration(builder.Configuration["AzureContentSafetyEndpoint"]!, builder.Configuration["AzureContentSafetyApiKey"]!)
    .AddTableStorageService(builder.Configuration["StorageConnectionString"]!);

builder.UseDurableFunctionsMonitor((settings, extensions) =>
{
    settings.DisableAuthentication = true;
    settings.Mode = DfmMode.Normal;
});

builder.Build().Run();
