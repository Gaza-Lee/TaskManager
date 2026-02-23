using Microsoft.EntityFrameworkCore;
using TaskManager.Components;
using TaskManager.Data;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQLite database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "tasks.db");
builder.Services.AddDbContext<TaskDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Services - Scoped to match DbContext lifetime
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// User identity - lightweight localStorage wrapper
builder.Services.AddScoped<UserIdentityService>();
builder.Services.AddScoped<TaskStateService>();
builder.Services.AddScoped<BrowserService>();

try 
{
    var app = builder.Build();

    // Ensure the database and tables exist
    DatabaseHelper.Initialize(app.Services);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    var logPath = Path.Combine(builder.Environment.ContentRootPath, "startup_log.txt");
    File.WriteAllText(logPath, $"[{DateTime.Now}] CRITICAL STARTUP ERROR:\n{ex.Message}\n\n{ex.StackTrace}");
    throw; // Re-throw to ensure IIS still registers the failure
}
