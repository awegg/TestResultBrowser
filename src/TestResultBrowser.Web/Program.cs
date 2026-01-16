using TestResultBrowser.Web.Components;
using MudBlazor.Services;
using TestResultBrowser.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<TestResultBrowserOptions>(
    builder.Configuration.GetSection("TestResultBrowser"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Register application services
builder.Services.AddSingleton<IVersionMapperService, VersionMapperService>();
builder.Services.AddSingleton<IFilePathParserService, FilePathParserService>();
builder.Services.AddSingleton<IJUnitParserService, JUnitParserService>();
builder.Services.AddSingleton<ITestDataService, TestDataService>();
builder.Services.AddSingleton<ConfigurationValidator>();

// Register background services
builder.Services.AddHostedService<FileWatcherService>();
builder.Services.AddSingleton<IFileWatcherService>(sp => 
    sp.GetServices<IHostedService>()
      .OfType<FileWatcherService>()
      .First());

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
