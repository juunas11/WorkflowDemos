using Elsa.CSharp.Options;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.AspNetCore.Mvc;
using WorkflowDemos.Elsa.Server.Types;
using WorkflowDemos.Shared.DataStorage;
using WorkflowDemos.Shared.Email;
using WorkflowDemos.Shared.Moderation;

var builder = WebApplication.CreateBuilder(args);

// Filter out EF Core logs below Warning level
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);

builder.WebHost.UseStaticWebAssets();

builder.Services
    .AddElsa(elsa => elsa
        .UseIdentity(identity =>
        {
            identity.TokenOptions = options => options.SigningKey = "large-signing-key-for-signing-JWT-tokens";
            identity.UseAdminUserProvider();
        })
        .UseDefaultAuthentication(auth => auth.UseAdminApiKey())
        .UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef => ef.UseSqlite());
            management.AddVariableTypes(
            [
                typeof(WorkflowInput),
                typeof(Comment),
            ], "Moderation");
        })
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseCSharp((CSharpOptions opts) =>
        {
            opts.Assemblies.Add(typeof(Comment).Assembly);
            opts.Namespaces.Add(typeof(Comment).Namespace!);
        })
        .UseHttp(http => http.ConfigureHttpOptions = options => builder.Configuration.GetSection("Http").Bind(options))
        .UseWorkflowsApi()
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>()
    );

// TODO: Tighten CORS
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));
builder.Services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

builder.Services.AddMailgunEmailService(
    builder.Configuration["Mailgun:FromEmail"]!,
    builder.Configuration["Mailgun:Domain"]!,
    builder.Configuration["Mailgun:ApiKey"]!,
    builder.Configuration["ModeratorEmail"]!,
    builder.Configuration["ModerationPortalUrl"]!);
builder.Services.AddAzureContentSafetyModeration(builder.Configuration["AzureContentSafety:Endpoint"]!, builder.Configuration["AzureContentSafety:ApiKey"]!);
builder.Services.AddTableStorageService(builder.Configuration["Storage:ConnectionString"]!);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseRouting();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapFallbackToPage("/_Host");
app.Run();
