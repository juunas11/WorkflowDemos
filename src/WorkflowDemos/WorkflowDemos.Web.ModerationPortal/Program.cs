using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Web.ModerationPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddTableStorageService(builder.Configuration["Storage:ConnectionString"]!);
builder.Services.AddMailgunEmailService(
    builder.Configuration["Mailgun:FromEmail"]!,
    builder.Configuration["Mailgun:Domain"]!,
    builder.Configuration["Mailgun:ApiKey"]!,
    builder.Configuration["ModeratorEmail"]!,
    builder.Configuration["ModerationPortalUrl"]!);
builder.Services.AddTemporalClient(builder.Configuration["Temporal:HostUri"]!);
builder.Services.AddHttpClient();
builder.Services.AddTransient<LogicAppsManualApprovalService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, DurableFunctionsIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, TemporalIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, ElsaIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, MassTransitIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, NServiceBusIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, LogicAppsIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, PowerAutomateIntegrationService>();
builder.Services.AddTransient<OrchestratorManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.MapPost("/api/logicapps/manual-approvals/subscriptions", async (
    LogicAppsManualApprovalSubscription request,
    LogicAppsManualApprovalService manualApprovalService) =>
{
    if (string.IsNullOrWhiteSpace(request.CommentId) || string.IsNullOrWhiteSpace(request.CallbackUrl))
    {
        return Results.BadRequest();
    }

    var registered = await manualApprovalService.TryRegisterAsync(request);
    return registered ? Results.Ok() : Results.NotFound();
});

app.MapDelete("/api/logicapps/manual-approvals/subscriptions/{commentId}", async (
    string commentId,
    LogicAppsManualApprovalService manualApprovalService) =>
{
    if (string.IsNullOrWhiteSpace(commentId))
    {
        return Results.BadRequest();
    }

    var removed = await manualApprovalService.TryUnregisterAsync(commentId);
    return removed ? Results.Ok() : Results.NotFound();
});

app.Run();
