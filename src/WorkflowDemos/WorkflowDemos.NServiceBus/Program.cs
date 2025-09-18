using WorkflowDemos.NServiceBus.Commands;
using WorkflowDemos.NServiceBus.Events;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

var builder = WebApplication.CreateBuilder(args);

var endpointConfiguration = new EndpointConfiguration("WorkflowDemos.NServiceBus");
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.UsePersistence<LearningPersistence>();
var transport = endpointConfiguration.UseTransport<LearningTransport>();

transport.Routing().RouteToEndpoint(
    assembly: typeof(SubmitComment).Assembly,
    destination: "WorkflowDemos.NServiceBus");

builder.UseNServiceBus(endpointConfiguration);

builder.Services.AddMailgunEmailService(
    builder.Configuration["Mailgun:FromEmail"]!,
    builder.Configuration["Mailgun:Domain"]!,
    builder.Configuration["Mailgun:ApiKey"]!,
    builder.Configuration["ModeratorEmail"]!,
    builder.Configuration["ModerationPortalUrl"]!);
builder.Services.AddAzureContentSafetyModeration(builder.Configuration["AzureContentSafety:Endpoint"]!, builder.Configuration["AzureContentSafety:ApiKey"]!);
builder.Services.AddTableStorageService(builder.Configuration["Storage:ConnectionString"]!);

var app = builder.Build();

app.MapPost("/comments", async (IMessageSession messageSession, OrchestrationInputV2 input) =>
{
    foreach (var comment in input.Comments)
    {
        var commentId = Guid.NewGuid();
        await messageSession.Send(new SubmitCommentV2
        {
            CommentId = commentId,
            CommentText = comment,
            DoManualReview = input.DoManualReview
        });
    }

    return Results.NoContent();
});

app.MapPost("/comments/{commentId}/approve", async (IMessageSession messageSession, Guid commentId) =>
{
    await messageSession.Publish(new CommentApprovedByHuman
    {
        CommentId = commentId
    });
    return Results.NoContent();
});

app.MapPost("/comments/{commentId}/reject", async (IMessageSession messageSession, Guid commentId) =>
{
    await messageSession.Publish(new CommentRejectedByHuman
    {
        CommentId = commentId
    });
    return Results.NoContent();
});

app.Run();
