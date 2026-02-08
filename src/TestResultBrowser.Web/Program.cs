using TestResultBrowser.Web.Components;
using MudBlazor.Services;
using TestResultBrowser.Web.Services;
using TestResultBrowser.Web.Hubs;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

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
builder.Services.AddSingleton<IFeatureGroupingService, FeatureGroupingService>();
builder.Services.AddSingleton<ITestReportUrlService, TestReportUrlService>();
builder.Services.AddSingleton<IReportAssetService, ReportAssetService>();
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
var reportOptions = app.Services.GetRequiredService<IOptions<TestResultBrowserOptions>>().Value;
if (!string.IsNullOrWhiteSpace(reportOptions.FileSharePath) && Directory.Exists(reportOptions.FileSharePath))
{
    var reportFileProvider = new PhysicalFileProvider(Path.GetFullPath(reportOptions.FileSharePath));
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = reportFileProvider,
        RequestPath = "/test-reports"
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = reportFileProvider,
        RequestPath = "/test-reports"
    });
}
else
{
    app.Logger.LogWarning("Test report file share path is missing or does not exist: {Path}", reportOptions.FileSharePath);
}
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
