using Microsoft.EntityFrameworkCore;
using TaskManager.Components;
using TaskManager.Data;
using TaskManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQL Server database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<TaskDbContext>(options =>
    options.UseSqlite(connectionString));

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

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
        db.Database.Migrate(); // Applies pending EF Core Migrations automatically
    }

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
    // Try to write a startup log. Use %TEMP% as a safe fallback — ContentRoot may not be
    // writable by the IIS app pool identity, and a failure here must NOT mask the real error.
    try
    {
        var logPath = Path.Combine(
            builder.Environment.ContentRootPath, "App_Data", "startup_log.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.WriteAllText(logPath,
            $"[{DateTime.Now:u}] CRITICAL STARTUP ERROR:\n{ex.Message}\n\n{ex.StackTrace}");
    }
    catch
    {
        // Swallow logging failure — the real exception will still propagate below.
    }
    throw; // Re-throw so IIS still registers the failure (Event Viewer / stdout log)
}
