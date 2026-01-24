# Service Contracts: JUnit Test Results Browser

**Feature**: 001-junit-results-browser  
**Date**: 2026-01-16  
**Purpose**: Define service interfaces for dependency injection and testability

**Note**: This is a Blazor Server application, so contracts are C# service interfaces (not REST API endpoints). Blazor uses SignalR for client-server communication automatically.

---

## Core Service Interfaces

### 1. ITestDataService

**Purpose**: Manages in-memory test result cache and provides query capabilities

```csharp
public interface ITestDataService
{
    // Cache Management
    void AddTestResult(TestResult result);
    void AddTestResults(IEnumerable<TestResult> results);
    void ClearCache();
    int GetCachedTestCount();
    
    // Query by Primary Key
    TestResult? GetTestResultById(string id);
    IEnumerable<TestResult> GetTestResultsByIds(IEnumerable<string> ids);
    
    // Query by Domain/Feature/Suite
    IEnumerable<TestResult> GetTestResultsByDomain(string domainId);
    IEnumerable<TestResult> GetTestResultsByFeature(string featureId);
    IEnumerable<TestResult> GetTestResultsBySuite(string testSuiteId);
    
    // Query by Configuration/Build
    IEnumerable<TestResult> GetTestResultsByConfiguration(string configurationId);
    IEnumerable<TestResult> GetTestResultsByBuild(string buildId);
    
    // Query by Test Name (historical lookup across builds)
    IEnumerable<TestResult> GetTestResultsByTestName(string testFullName);
    
    // Filtering
    IEnumerable<TestResult> FilterTestResults(TestFilterCriteria criteria);
    
    // Aggregations
    DomainSummary GetDomainSummary(string domainId);
    FeatureSummary GetFeatureSummary(string featureId);
    ConfigurationMatrix GetConfigurationMatrix();
    
    // Metadata
    IEnumerable<Domain> GetAllDomains();
    IEnumerable<Feature> GetFeaturesByDomain(string domainId);
    IEnumerable<Configuration> GetAllConfigurations();
    IEnumerable<Build> GetBuildsByConfiguration(string configurationId);
}

public record TestFilterCriteria
{
    public List<string>? Domains { get; init; }
    public List<string>? Features { get; init; }
    public List<string>? Versions { get; init; }
    public List<string>? NamedConfigs { get; init; }
    public List<string>? Machines { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public bool? OnlyFailures { get; init; }
    public bool? HideFlakyTests { get; init; }
}
```

---

### 2. IFileWatcherService

**Purpose**: Background service that polls file system for new test results

```csharp
public interface IFileWatcherService
{
    // Service Lifecycle (IHostedService)
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    
    // Manual Trigger
    Task ScanFileSystemNowAsync();
    
    // Status
    DateTime? LastScanTime { get; }
    DateTime? LastImportTime { get; }
    int NewFilesFoundInLastScan { get; }
    bool IsScanning { get; }
    
    // Events
    event EventHandler<FileSystemScanEventArgs>? ScanStarted;
    event EventHandler<FileSystemScanEventArgs>? ScanCompleted;
    event EventHandler<NewFilesImportedEventArgs>? NewFilesImported;
    event EventHandler<ErrorEventArgs>? ScanError;
}

public class FileSystemScanEventArgs : EventArgs
{
    public DateTime ScanTime { get; init; }
    public int DirectoriesScanned { get; init; }
}

public class NewFilesImportedEventArgs : EventArgs
{
    public int FilesImported { get; init; }
    public int TestResultsAdded { get; init; }
    public List<string> NewBuildIds { get; init; } = new();
}
```

---

### 3. IJUnitParserService

**Purpose**: Parses JUnit XML files into domain models

```csharp
public interface IJUnitParserService
{
    // Parse single XML file
    Task<List<TestResult>> ParseXmlFileAsync(string filePath, ParseContext context);
    
    // Parse multiple XML files (parallel)
    Task<List<TestResult>> ParseXmlFilesAsync(IEnumerable<string> filePaths, ParseContext context);
    
    // Validate XML structure
    bool IsValidJUnitXml(string filePath);
}

public record ParseContext
{
    public required string ConfigurationId { get; init; }    // e.g., "dev_E2E_Default1_Core"
    public required string BuildId { get; init; }            // e.g., "Release-252_181639"
    public required int BuildNumber { get; init; }           // e.g., 252
    public required DateTime BuildTimestamp { get; init; }   // When test ran
    public required string DomainId { get; init; }           // e.g., "Core"
    public required string FeatureId { get; init; }          // e.g., "AlarmManager"
}
```

---

### 4. IFilePathParserService

**Purpose**: Extracts metadata from file system paths

```csharp
public interface IFilePathParserService
{
    // Parse configuration from top-level directory
    // e.g., "dev_E2E_Default1_Core" → {Version="dev", TestType="E2E", NamedConfig="Default1", Domain="Core"}
    Configuration? ParseConfigurationFromPath(string directoryPath);
    
    // Parse build from second-level directory
    // e.g., "Release-252_181639" → {BuildNumber=252, Timestamp="181639"}
    Build? ParseBuildFromPath(string directoryPath, string configurationId);
    
    // Parse feature from third-level directory
    // e.g., "Px Core - Alarm Manager" → {Domain="Core", Feature="AlarmManager"}
    (string DomainId, string FeatureId)? ParseFeatureFromPath(string directoryPath);
    
    // Find all XML files in feature directory
    IEnumerable<string> FindJUnitXmlFiles(string featureDirectoryPath);
}
```

---

### 5. IVersionMapperService

**Purpose**: Maps version codes to human-readable versions

```csharp
public interface IVersionMapperService
{
    // Map version code to display string
    // e.g., "PXrel114" → "1.14.0", "dev" → "Development"
    string MapVersionCode(string versionCode);
    
    // Reverse mapping (if needed for filtering)
    string? GetVersionCodeFromDisplay(string displayVersion);
    
    // Get all known mappings
    Dictionary<string, string> GetAllMappings();
}
```

---

### 6. ITriageService

**Purpose**: Morning/Release triage workflow logic

```csharp
public interface ITriageService
{
    // Morning Triage: Find newly failing tests
    Task<MorningTriageResult> GetMorningTriageAsync(string todayBuildId, string? yesterdayBuildId = null);
    
    // Release Triage: Configuration matrix with failure highlights
    Task<ReleaseTriageResult> GetReleaseTriageAsync(string releaseBuildId, string? previousReleaseBuildId = null);
    
    // Feature Impact: All tests affecting a feature across configs
    Task<FeatureImpactResult> GetFeatureImpactAsync(string featureId, string? buildId = null);
}

public record MorningTriageResult
{
    public required List<TriageNewFailure> NewFailures { get; init; }
    public required List<TriageFixedTest> FixedTests { get; init; }
    public required int TotalNewFailures { get; init; }
    public required int TotalFixed { get; init; }
    public required int TotalStillFailing { get; init; }
    public required Dictionary<string, int> FailuresByDomain { get; init; }
    public required Dictionary<string, int> FailuresByFeature { get; init; }
}

public record TriageFixedTest
{
    public required string TestFullName { get; init; }
    public required string DomainId { get; init; }
    public required string FeatureId { get; init; }
}

public record ReleaseTriageResult
{
    public required ConfigurationMatrix Matrix { get; init; }
    public required List<string> FailingConfigurations { get; init; }
    public required Dictionary<string, double> DomainPassRates { get; init; }
    public required Dictionary<string, double> FeaturePassRates { get; init; }
    public required ComparisonMetrics? ComparisonToPrevious { get; init; }
}

public record ComparisonMetrics
{
    public required int TestsRegressed { get; init; }       // Passed in previous, failed in current
    public required int TestsImproved { get; init; }        // Failed in previous, passed in current
    public required double PassRateChange { get; init; }    // Percentage point change
}

public record FeatureImpactResult
{
    public required string FeatureId { get; init; }
    public required List<TestResult> AllTests { get; init; }
    public required ConfigurationMatrix ConfigMatrix { get; init; }
    public required double OverallPassRate { get; init; }
    public required bool IsReleaseReady { get; init; }      // True if pass rate > threshold
}
```

---

### 7. IFlakyDetectionService

**Purpose**: Identifies and manages flaky tests

```csharp
public interface IFlakyDetectionService
{
    // Detect all flaky tests in cache
    Task<List<FlakyTest>> DetectFlakyTestsAsync();
    
    // Check if specific test is flaky
    Task<FlakyTest?> CheckTestFlakinessAsync(string testFullName);
    
    // Configure thresholds
    void SetThresholds(int windowSize, double flakinessThreshold, int clearAfterConsecutivePasses);
    
    // Get current thresholds
    (int WindowSize, double Threshold, int ClearAfter) GetThresholds();
}
```

---

### 8. IFailureGroupingService

**Purpose**: Groups tests by similar error patterns

```csharp
public interface IFailureGroupingService
{
    // Group all failed tests by error similarity
    Task<List<FailureGroup>> GroupFailuresAsync(IEnumerable<TestResult> failedTests);
    
    // Find failure group for specific test
    Task<FailureGroup?> FindGroupForTestAsync(string testId);
    
    // Configure similarity threshold (0.0 - 1.0)
    void SetSimilarityThreshold(double threshold);
}
```

---

### 9. IUserDataService

**Purpose**: Manages user-generated data (baselines, comments, filters) in LiteDB

```csharp
public interface IUserDataService
{
    // Baselines
    Task<UserBaseline> CreateBaselineAsync(UserBaseline baseline);
    Task<UserBaseline?> GetBaselineByIdAsync(int id);
    Task<List<UserBaseline>> GetBaselinesByConfigurationAsync(string configurationId);
    Task UpdateBaselineAsync(UserBaseline baseline);
    Task DeleteBaselineAsync(int id);
    
    // Saved Filters
    Task<SavedFilterConfiguration> CreateFilterAsync(SavedFilterConfiguration filter);
    Task<SavedFilterConfiguration?> GetFilterByIdAsync(int id);
    Task<List<SavedFilterConfiguration>> GetFiltersByUserAsync(string username);
    Task UpdateFilterAsync(SavedFilterConfiguration filter);
    Task DeleteFilterAsync(int id);
    
    // Comments
    Task<UserComment> CreateCommentAsync(UserComment comment);
    Task<UserComment?> GetCommentByIdAsync(int id);
    Task<List<UserComment>> GetCommentsByTargetAsync(string targetType, string targetId);
    Task UpdateCommentAsync(UserComment comment);
    Task DeleteCommentAsync(int id);
    
    // Dashboards
    Task<DashboardConfiguration> CreateDashboardAsync(DashboardConfiguration dashboard);
    Task<DashboardConfiguration?> GetDashboardByIdAsync(int id);
    Task<List<DashboardConfiguration>> GetDashboardsByUserAsync(string username);
    Task UpdateDashboardAsync(DashboardConfiguration dashboard);
    Task DeleteDashboardAsync(int id);
}
```

---

### 10. IQualityTrendService

**Purpose**: Computes quality trends over time

```csharp
public interface IQualityTrendService
{
    // Get trend for domain over last N builds
    Task<QualityTrend> GetDomainTrendAsync(string domainId, int buildCount = 30);
    
    // Get trend for feature over last N builds
    Task<QualityTrend> GetFeatureTrendAsync(string featureId, int buildCount = 30);
    
    // Get trend for specific test over last N runs
    Task<ExecutionTimeMetric> GetTestExecutionTrendAsync(string testFullName, int runCount = 10);
}
```

---

### 11. IPolarionLinkService

**Purpose**: Extracts and generates Polarion ticket links

```csharp
public interface IPolarionLinkService
{
    // Extract ticket IDs from test name
    List<string> ExtractTicketIds(string testName);
    
    // Generate URL for ticket ID
    string GenerateTicketUrl(string ticketId);
    
    // Extract and generate all links for test
    List<PolarionTicketReference> GetTicketReferencesForTest(TestResult testResult);
    
    // Configure Polarion base URL
    void SetPolarionBaseUrl(string baseUrl);
    string GetPolarionBaseUrl();
}
```

---

### 12. IHeatmapService

**Purpose**: Generates failure heatmap (Feature × Build)

```csharp
public interface IHeatmapService
{
    // Generate heatmap for all features across last N builds
    Task<List<HeatmapCell>> GenerateHeatmapAsync(int buildCount = 20);
    
    // Generate heatmap for specific domain
    Task<List<HeatmapCell>> GenerateHeatmapForDomainAsync(string domainId, int buildCount = 20);
}
```

---

## Configuration Interface

### IAppConfiguration

**Purpose**: Application settings and configuration

```csharp
public interface IAppConfiguration
{
    // File System
    string FileSharePath { get; }               // e.g., @"\\fileserver\testresults"
    int PollingIntervalMinutes { get; }         // Default: 15
    
    // Flaky Detection
    int FlakyDetectionWindowSize { get; }       // Default: 20
    double FlakyDetectionThreshold { get; }     // Default: 30.0 (%)
    int FlakyClearAfterPasses { get; }          // Default: 10
    
    // Failure Grouping
    double FailureGroupingSimilarity { get; }   // Default: 0.8 (80%)
    
    // Polarion
    string PolarionBaseUrl { get; }             // e.g., "https://polarion.company.com"
    
    // User Data
    string UserDataDbPath { get; }              // e.g., "userdata.db"
    
    // Performance
    int AggregateCacheMinutes { get; }          // Default: 5
    int VirtualizeRowThreshold { get; }         // Default: 1000
}
```

---

## Blazor Component State Management

### ComponentState (for Blazor components)

**Purpose**: Shared state between Blazor components using services

```csharp
public interface IFilterState
{
    // Active filters
    TestFilterCriteria CurrentFilters { get; set; }
    
    // Events for filter changes
    event EventHandler? FiltersChanged;
    
    // Apply filters
    void ApplyFilters(TestFilterCriteria filters);
    void ClearFilters();
}

public interface INavigationState
{
    // Current view
    string CurrentView { get; set; }
    
    // Selected items
    string? SelectedDomainId { get; set; }
    string? SelectedFeatureId { get; set; }
    string? SelectedBuildId { get; set; }
    
    // Events
    event EventHandler? NavigationChanged;
}
```

---

## Service Registration (Dependency Injection)

**In Program.cs**:

```csharp
// Core Services
builder.Services.AddSingleton<ITestDataService, TestDataService>();
builder.Services.AddSingleton<IFilePathParserService, FilePathParserService>();
builder.Services.AddSingleton<IVersionMapperService, VersionMapperService>();
builder.Services.AddSingleton<IPolarionLinkService, PolarionLinkService>();

// Parsing Services
builder.Services.AddScoped<IJUnitParserService, JUnitParserService>();

// Background Services
builder.Services.AddHostedService<FileWatcherService>();

// Triage Services
builder.Services.AddScoped<ITriageService, TriageService>();
builder.Services.AddScoped<IFlakyDetectionService, FlakyDetectionService>();
builder.Services.AddScoped<IFailureGroupingService, FailureGroupingService>();
builder.Services.AddScoped<IQualityTrendService, QualityTrendService>();
builder.Services.AddScoped<IHeatmapService, HeatmapService>();

// User Data Services
builder.Services.AddSingleton<IUserDataService, UserDataService>();

// Configuration
builder.Services.AddSingleton<IAppConfiguration, AppConfiguration>();

// Component State
builder.Services.AddScoped<IFilterState, FilterState>();
builder.Services.AddScoped<INavigationState, NavigationState>();
```

---

## Testing Interfaces

All services implement interfaces to enable:
- **Unit testing** with mocks (Moq, NSubstitute)
- **Integration testing** with test data
- **Component testing** with bUnit and stubbed services

Example test:
```csharp
[Fact]
public async Task MorningTriage_DetectsNewFailures()
{
    // Arrange
    var mockDataService = new Mock<ITestDataService>();
    mockDataService.Setup(s => s.GetTestResultsByBuild("today"))
        .Returns(new List<TestResult> { /* ... */ });
    
    var triageService = new TriageService(mockDataService.Object);
    
    // Act
    var result = await triageService.GetMorningTriageAsync("today", "yesterday");
    
    // Assert
    Assert.NotEmpty(result.NewFailures);
}
```

---

**Next Steps**: Generate quickstart.md for developer setup
