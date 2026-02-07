using TestResultBrowser.Web.Components;
using MudBlazor.Services;
using TestResultBrowser.Web.Services;
using TestResultBrowser.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<TestResultBrowserOptions>(
    builder.Configuration.GetSection("TestResultBrowser"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add controllers for API endpoints
builder.Services.AddControllers();

// Add MudBlazor services
builder.Services.AddMudServices();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Add memory cache
builder.Services.AddMemoryCache();

// Register application services
builder.Services.AddSingleton<ISettingsService, SettingsService>();
builder.Services.AddSingleton<IVersionMapperService, VersionMapperService>();
builder.Services.AddSingleton<IFilePathParserService, FilePathParserService>();
builder.Services.AddSingleton<IJUnitParserService, JUnitParserService>();
builder.Services.AddSingleton<ITestDataService, TestDataService>();
builder.Services.AddSingleton<ITriageService, TriageService>();
builder.Services.AddSingleton<IConfigurationHistoryService, ConfigurationHistoryService>();
builder.Services.AddSingleton<IUserDataService, UserDataService>();
builder.Services.AddSingleton<IWorkItemLinkService, WorkItemLinkService>();
builder.Services.AddSingleton<IFailureGroupingService, FailureGroupingService>();
builder.Services.AddSingleton<IFlakyTestDetectionService, FlakyTestDetectionService>();
builder.Services.AddSingleton<ConfigurationValidator>();

// Register background services
builder.Services.AddHostedService<FileWatcherService>();
builder.Services.AddSingleton<IFileWatcherService>(sp =>
    sp.GetServices<IHostedService>()
      .OfType<FileWatcherService>()
      .FirstOrDefault() ?? throw new InvalidOperationException("FileWatcherService not registered"));

var app = builder.Build();

// Validate configuration on startup
using (var scope = app.Services.CreateScope())
{
    var validator = scope.ServiceProvider.GetRequiredService<ConfigurationValidator>();
    if (!validator.ValidateConfiguration())
    {
        throw new InvalidOperationException("Configuration validation failed. Check logs for details.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Map API controllers
app.MapControllers();

// Add health check endpoint for Docker
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<TestDataHub>("/hubs/testdata");

try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}
