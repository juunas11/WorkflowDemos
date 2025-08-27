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

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddMailgunEmailService(builder.Configuration["Mailgun:FromEmail"]!, builder.Configuration["Mailgun:Domain"]!, builder.Configuration["Mailgun:ApiKey"]!)
    .AddAzureContentSafetyModeration(builder.Configuration["AzureContentSafety:Endpoint"]!, builder.Configuration["AzureContentSafety:ApiKey"]!)
    .AddTableStorageService(builder.Configuration["Storage:ConnectionString"]!);

builder.Build().Run();
