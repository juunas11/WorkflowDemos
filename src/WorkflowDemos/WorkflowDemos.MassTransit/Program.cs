using MassTransit;
using WorkflowDemos.MassTransit.Consumers;
using WorkflowDemos.MassTransit.Messages;
using WorkflowDemos.MassTransit.Sagas;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(cfg =>
{
    cfg.AddSagaStateMachine<CommentModerationStateMachine, CommentModerationState>()
        .InMemoryRepository();

    cfg.AddConsumer<StoreInitialCommentStateConsumer>();
    cfg.AddConsumer<ReviewCommentWithAiConsumer>();
    cfg.AddConsumer<EmailModeratorConsumer>();
    cfg.AddConsumer<SaveResultConsumer>();

    cfg.UsingInMemory((context, transport) =>
    {
        transport.ConfigureEndpoints(context);
    });
});
builder.Services.AddMailgunEmailService(
    builder.Configuration["Mailgun:FromEmail"]!,
    builder.Configuration["Mailgun:Domain"]!,
    builder.Configuration["Mailgun:ApiKey"]!,
    builder.Configuration["ModeratorEmail"]!,
    builder.Configuration["ModerationPortalUrl"]!);
builder.Services.AddAzureContentSafetyModeration(builder.Configuration["AzureContentSafety:Endpoint"]!, builder.Configuration["AzureContentSafety:ApiKey"]!);
builder.Services.AddTableStorageService(builder.Configuration["Storage:ConnectionString"]!);

var app = builder.Build();

app.MapGet("/", async (IPublishEndpoint publishEndpoint) =>
{
    var commentId = Guid.NewGuid();
    await publishEndpoint.Publish<SubmitComment>(new
    {
        CommentId = commentId,
        CommentText = "I hate you.",
    });
    return Results.Ok($"Submitted comment with ID: {commentId}");
});

app.MapGet("/approve/{commentId}", async (IPublishEndpoint publishEndpoint, Guid commentId) =>
{
    await publishEndpoint.Publish<CommentApprovedByHuman>(new
    {
        CommentId = commentId
    });
    return Results.Ok($"Approved comment with ID: {commentId}");
});
app.MapGet("/reject/{commentId}", async (IPublishEndpoint publishEndpoint, Guid commentId) =>
{
    await publishEndpoint.Publish<CommentRejectedByHuman>(new
    {
        CommentId = commentId
    });
    return Results.Ok($"Rejected comment with ID: {commentId}");
});

app.Run();
