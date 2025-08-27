using WorkflowDemos.Elsa;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddElsaCore(options => options
    .AddHttpActivities()
    .AddWorkflow<ContentModerationWorkflow>());

var app = builder.Build();

app.UseHttpActivities();

app.Run();
