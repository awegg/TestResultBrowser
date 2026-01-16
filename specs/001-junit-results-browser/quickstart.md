# Quickstart Guide: JUnit Test Results Browser

**Feature**: 001-junit-results-browser  
**Date**: 2026-01-16  
**Stack**: Blazor Server / C# / .NET 8.0

---

## Prerequisites

### Required Software
- **. NET 8.0 SDK** (LTS): https://dot net.microsoft.com/download/dotnet/8.0
- **Visual Studio 2022** (17.8+) or **VS Code** with C# Dev Kit extension
- **Git** (for source control)
- **Windows OS** (required for file system paths and sample_data access)

### Recommended Tools
- **Visual Studio 2022 Community/Professional** (best Blazor debugging experience)
- **ReSharper** or **Rider** (optional, for advanced C# tooling)
- **LINQPad** (optional, for testing LINQ queries against cached data)

---

## Project Setup

### 1. Clone Repository & Install Dependencies

```powershell
# Navigate to repository root
cd C:\workspace\TestResultBrowser2.0

# Restore NuGet packages (when solution exists)
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

### 2. Configure Application Settings

Create `appsettings.Development.json` in `src/TestResultBrowser.Web/`:

```json
{
  "FileSystem": {
    "FileSharePath": "C:\\workspace\\TestResultBrowser2.0\\sample_data",
    "PollingIntervalMinutes": 1
  },
  "FlakyDetection": {
    "WindowSize": 20,
    "Threshold": 30.0,
    "ClearAfterConsecutivePasses": 10
  },
  "FailureGrouping": {
    "SimilarityThreshold": 0.8
  },
  "Polarion": {
    "BaseUrl": "https://polarion.company.com"
  },
  "UserData": {
    "DbPath": "userdata.db"
  },
  "Performance": {
    "AggregateCacheMinutes": 5,
    "VirtualizeRowThreshold": 1000
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "TestResultBrowser": "Debug"
    }
  }
}
```

**For Development**: Point `FileSharePath` to local `sample_data` folder
**For Production**: Point to network share (e.g., `\\\\fileserver\\testresults`)

---

## Running the Application

### Option 1: Visual Studio 2022

1. Open `TestResultBrowser.sln` in Visual Studio
2. Set `TestResultBrowser.Web` as startup project
3. Press **F5** to run with debugging (or **Ctrl+F5** without debugging)
4. Browser will open at `https://localhost:5001` (or http://localhost:5000)

### Option 2: .NET CLI

```powershell
cd src\TestResultBrowser.Web
dotnet run
```

Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

Navigate to: `https://localhost:5001`

### Option 3: VS Code

1. Open repository root in VS Code
2. Install "C# Dev Kit" extension
3. Open `src/TestResultBrowser.Web/Program.cs`
4. Click "Run and Debug" → ".NET Core Launch (web)"
5. Browser opens automatically

---

## Initial Data Load

### First Run

On first startup, the `FileWatcherService` will:
1. Scan `sample_data/` directory
2. Discover configuration directories (e.g., `dev_E2E_Default1_Core/`)
3. Parse all `Release-{BuildNumber}_{Timestamp}/` folders
4. Import all JUnit XML files (`tests-*.xml`)
5. Build in-memory cache

**Expected import time**: ~30-60 seconds for sample_data (60+ builds × 50+ configs)

Watch console output:
```
[FileWatcherService] Starting file system scan...
[FileWatcherService] Found 50 configuration directories
[FileWatcherService] Importing 3000 XML files...
[JUnitParserService] Parsing tests-abc123.xml (185 tests)
[TestDataService] Cache size: 500,000 test results (12 GB RAM)
[FileWatcherService] Import complete in 45.2 seconds
```

---

## Navigating the Application

### Landing Page: Morning Triage

Default view shows:
- **New Failures**: Tests that passed yesterday, failed today
- **Fixed Tests**: Tests that failed yesterday, passed today
- **Configuration Matrix**: Visual grid showing which configs are affected
- **Domain/Feature Grouping**: Failures organized by Domain → Feature

**Try**: Click on a domain (e.g., "Px Core") to see feature breakdown

### Release Triage View

Navigate to **Release Triage** in sidebar:
- Configuration matrix with pass rates
- Failing configurations highlighted in red
- Domain/Feature pass rates for release readiness

**Try**: Hover over matrix cells to see failure details

### Feature Impact View

Navigate to **Feature Impact** → Select a feature (e.g., "Alarm Manager"):
- All tests affecting the feature across all configurations
- Configuration matrix filtered to feature-specific results
- Quick assessment: "Is this feature ready for release?"

**Try**: Filter by a specific version (e.g., "1.14.0") to see version-specific impact

### Flaky Tests View

Navigate to **Flaky Tests**:
- List of tests with inconsistent pass/fail results
- Flakiness scores (e.g., "40% unstable")
- Pass/fail history timeline

**Try**: Toggle "Hide Flaky Tests" in toolbar to filter them out from other views

---

## Development Workflow

### 1. Add a New Page (Blazor Component)

Create file: `src/TestResultBrowser.Web/Pages/MyNewView.razor`

```razor
@page "/mynewview"
@using TestResultBrowser.Web.Services
@inject ITestDataService TestDataService

<PageTitle>My New View</PageTitle>

<MudText Typo="Typo.h4">My New View</MudText>

@code {
    private int testCount;
    
    protected override async Task OnInitializedAsync()
    {
        testCount = TestDataService.GetCachedTestCount();
        await base.OnInitializedAsync();
    }
}
```

Add to sidebar navigation in `src/TestResultBrowser.Web/Shared/NavMenu.razor`:

```razor
<MudNavLink Href="/mynewview" Icon="@Icons.Material.Filled.Analytics">My New View</MudNavLink>
```

### 2. Add a New Service

Create interface: `src/TestResultBrowser.Web/Services/IMyService.cs`

```csharp
public interface IMyService
{
    Task<string> DoSomethingAsync();
}
```

Create implementation: `src/TestResultBrowser.Web/Services/MyService.cs`

```csharp
public class MyService : IMyService
{
    private readonly ITestDataService _testDataService;
    
    public MyService(ITestDataService testDataService)
    {
        _testDataService = testDataService;
    }
    
    public async Task<string> DoSomethingAsync()
    {
        // Implementation
        return await Task.FromResult("Done");
    }
}
```

Register in `Program.cs`:

```csharp
builder.Services.AddScoped<IMyService, MyService>();
```

### 3. Add Unit Tests

Create test file: `tests/TestResultBrowser.Tests.Unit/Services/MyServiceTests.cs`

```csharp
using Xunit;
using Moq;
using TestResultBrowser.Web.Services;

public class MyServiceTests
{
    [Fact]
    public async Task DoSomething_ReturnsExpectedResult()
    {
        // Arrange
        var mockDataService = new Mock<ITestDataService>();
        mockDataService.Setup(s => s.GetCachedTestCount()).Returns(1000);
        
        var myService = new MyService(mockDataService.Object);
        
        // Act
        var result = await myService.DoSomethingAsync();
        
        // Assert
        Assert.Equal("Done", result);
    }
}
```

Run tests:

```powershell
dotnet test
```

---

## Testing with Sample Data

### Verify File Structure Parsing

Check if configurations are detected correctly:

1. Open browser Dev Tools (F12)
2. Navigate to `/` (home page)
3. Check console for FileWatcher logs
4. Expected configurations from `sample_data`:
   - `dev_E2E_Default1_Core`
   - `PXrel114_Win2022_Default1_Core`
   - `dev_E2E_Default2_TnT_Prod`
   - `dev_E2E_Default1_ProductionMonitoring`

### Test Morning Triage Workflow

1. Navigate to **Morning Triage** view
2. Select "Today" = latest build (e.g., `Release-252`)
3. Select "Yesterday" = previous build (e.g., `Release-251`)
4. Verify new failures are highlighted
5. Check configuration matrix shows affected configs graphically

### Test Flaky Detection

To create flaky tests for testing:

1. Modify sample XML files to have mixed pass/fail results
2. Copy `sample_data/dev_E2E_Default1_Core/Release-250/` to `Release-253/`
3. Edit some `<testcase>` elements to change status from pass → fail
4. Trigger manual import: Click "Refresh Now" button
5. Navigate to **Flaky Tests** view
6. Verify tests with mixed results are flagged

---

## Debugging Tips

### Debug Blazor Components

**Set breakpoint in Razor component**:
1. Open `.razor` file
2. Set breakpoint in `@code { }` block
3. Press F5 to start debugging
4. Trigger component action (click button, etc.)
5. Breakpoint hits, inspect variables in VS debugger

### Debug Background FileWatcher Service

**View FileWatcher logs**:
```csharp
// In FileWatcherService.cs, add logging
_logger.LogInformation("Scanning directory: {Path}", directoryPath);
```

**Force immediate scan** (instead of waiting 15 minutes):
- Add manual trigger button in UI
- Call `await _fileWatcherService.ScanFileSystemNowAsync();`

### Debug In-Memory Cache

**Inspect cache contents** in debugger:
1. Set breakpoint in `TestDataService.AddTestResults()`
2. Watch window: Add `_testResultsCache` variable
3. Expand dictionary to see cached test results
4. Check key format: `{ConfigId}_{BuildId}_{TestFullName}`

### Performance Profiling

**Measure filtering performance**:
```csharp
var stopwatch = Stopwatch.StartNew();
var results = _testDataService.FilterTestResults(criteria);
stopwatch.Stop();
_logger.LogInformation("Filter took {Ms}ms", stopwatch.ElapsedMilliseconds);
```

**Target**: <2000ms for 10,000+ test filtering (SC-001)

---

## Common Issues & Solutions

### Issue: FileWatcherService not finding sample_data

**Cause**: Incorrect `FileSharePath` in appsettings.json

**Solution**:
```json
{
  "FileSystem": {
    "FileSharePath": "C:\\workspace\\TestResultBrowser2.0\\sample_data"  // Use absolute path
  }
}
```

### Issue: Out of memory exception on large datasets

**Cause**: Insufficient RAM for 30M test results

**Solution**:
- Add data retention policy (keep only last 90 days)
- Increase server RAM to 32 GB
- Or filter sample_data to fewer builds during development

### Issue: Slow page loads (>5 seconds)

**Cause**: Missing aggregate cache, computing summaries on every request

**Solution**:
```csharp
// In TestDataService, add caching
private readonly MemoryCache _aggregateCache = new(new MemoryCacheOptions());

public DomainSummary GetDomainSummary(string domainId)
{
    return _aggregateCache.GetOrCreate($"domain_{domainId}", entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
        return ComputeDomainSummary(domainId);
    });
}
```

### Issue: Blazor SignalR connection drops frequently

**Cause**: Long idle sessions, default timeout = 30 seconds

**Solution** in `Program.cs`:
```csharp
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
    });
```

---

## Next Steps

### Phase 1 Implementation (P0 Features)

1. **Setup project structure** (see plan.md → Project Structure)
2. **Implement core services**:
   - `TestDataService` (in-memory cache)
   - `JUnitParserService` (XML parsing)
   - `FileWatcherService` (background polling)
3. **Build Morning Triage page** (US 1)
4. **Build Release Triage page** (US 2)
5. **Add configuration matrix component** (visual grid)

### Phase 2 Implementation (P1 Features)

1. **Flaky detection service** (US 12)
2. **Polarion link extraction** (US 13)
3. **Failure grouping** (US 14)
4. **User data persistence** (LiteDB for baselines, comments)

### Phase 3 Implementation (P2-P3 Features)

1. **Quality trends** (US 4, US 17)
2. **Failure heatmap** (US 18)
3. **Collaboration features** (US 19, US 20)

---

## Resources

### Official Documentation
- **Blazor**: https://learn.microsoft.com/aspnet/core/blazor
- **.NET 8**: https://learn.microsoft.com/dotnet/core/whats-new/dotnet-8
- **MudBlazor**: https://mudblazor.com/getting-started/installation
- **LiteDB**: https://www.litedb.org/docs/getting-started

### Sample Projects
- **Blazor Server Sample**: https://github.com/dotnet/blazor-samples
- **MudBlazor Templates**: https://github.com/MudBlazor/Templates

### Testing Resources
- **xUnit**: https://xunit.net/
- **bUnit** (Blazor testing): https://bunit.dev/docs/getting-started
- **Moq** (mocking): https://github.com/moq/moq4

---

## Support

For questions or issues:
1. Check `research.md` for architecture decisions
2. Review `data-model.md` for entity definitions
3. Check `contracts/service-interfaces.md` for service APIs
4. Consult `spec.md` for functional requirements

**Happy coding!** 🚀
