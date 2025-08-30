using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Web.ModerationPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddTableStorageService(builder.Configuration["Storage:ConnectionString"]!);
builder.Services.AddTemporalClient(builder.Configuration["Temporal:HostUri"]!);
builder.Services.AddHttpClient();
builder.Services.AddTransient<IOrchestratorIntegrationService, DurableFunctionsIntegrationService>();
builder.Services.AddTransient<IOrchestratorIntegrationService, TemporalIntegrationService>();
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

app.Run();
