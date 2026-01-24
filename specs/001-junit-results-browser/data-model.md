# Data Model: JUnit Test Results Browser

**Feature**: 001-junit-results-browser  
**Date**: 2026-01-16  
**Technology**: C# / .NET 8.0  
**ORM**: None (in-memory for test data), LiteDB for user data

---

## Entity Overview

This document defines the data model for the JUnit Test Results Browser. Entities are categorized by:
- **Core Entities** (test data - memory only)
- **User Data Entities** (persistent - LiteDB)
- **Computed/View Entities** (derived on-demand)

---

## Core Entities (In-Memory Cache)

### 1. TestResult

**Purpose**: Represents a single test case execution from JUnit XML

**C# Model**:
```csharp
public record TestResult
{
    public required string Id { get; init; }                    // Composite: {ConfigId}_{BuildId}_{TestFullName}
    public required string TestFullName { get; init; }          // e.g., "AlarmManagerTests.TestDownloadReport"
    public required string ClassName { get; init; }             // e.g., "AlarmManagerTests"
    public required string MethodName { get; init; }            // e.g., "TestDownloadReport"
    public required TestStatus Status { get; init; }            // Pass, Fail, Skip
    public required double ExecutionTimeSeconds { get; init; }  // Test duration
    public required DateTime Timestamp { get; init; }           // Run timestamp
    public string? ErrorMessage { get; init; }                  // Failure message (null if passed)
    public string? StackTrace { get; init; }                    // Stack trace (null if passed)
    
    // Associated metadata
    public required string DomainId { get; init; }              // e.g., "Core"
    public required string FeatureId { get; init; }             // e.g., "AlarmManager"
    public required string TestSuiteId { get; init; }           // From JUnit <testsuite name="...">
    public required string ConfigurationId { get; init; }       // Composite: {Version}_{TestType}_{NamedConfig}_{Domain}
    public required string BuildId { get; init; }               // e.g., "Release-252_181639"
    public required int BuildNumber { get; init; }              // e.g., 252
    public required string Machine { get; init; }               // Hostname (if available in XML)
    
    // Extracted references
    public List<string> PolarionTickets { get; init; } = new(); // e.g., ["PEXC-28044"]
}

public enum TestStatus
{
    Pass,
    Fail,
    Skip
}
```

**Validation Rules**:
- `Id` must be unique within cache
- `ExecutionTimeSeconds` >= 0
- `BuildNumber` > 0
- `Timestamp` <= DateTime.UtcNow
- `Status` = Fail → `ErrorMessage` should not be null

**Relationships**:
- Belongs to one `Domain`
- Belongs to one `Feature`
- Belongs to one `TestSuite`
- Belongs to one `Configuration`
- Belongs to one `Build`

**Persistence**: Memory only (ConcurrentDictionary<string, TestResult>)

---

### 2. Domain

**Purpose**: Top-level organizational unit (Core, T&T, PM, Prod, Feature)

**C# Model**:
```csharp
public record Domain
{
    public required string Id { get; init; }              // e.g., "Core", "TnT_Prod"
    public required string DisplayName { get; init; }     // e.g., "Px Core", "Px T&T Production"
    public List<string> FeatureIds { get; init; } = new(); // Child features
}
```

**Validation Rules**:
- `Id` must be unique
- `Id` extracted from file path (4th segment of `{Version}_{TestType}_{NamedConfig}_{Domain}/`)

**Relationships**:
- Has many `Feature` entities
- Has many `TestResult` entities

**Persistence**: Memory only (computed from test results)

---

### 3. Feature

**Purpose**: Product feature or component within a domain

**C# Model**:
```csharp
public record Feature
{
    public required string Id { get; init; }              // e.g., "AlarmManager"
    public required string DisplayName { get; init; }     // e.g., "Px Core - Alarm Manager"
    public required string DomainId { get; init; }        // Parent domain
    public List<string> TestSuiteIds { get; init; } = new(); // Child test suites
}
```

**Validation Rules**:
- `Id` must be unique within domain
- `Id` extracted from folder name pattern: "Px {Domain} - {FeatureName}/"

**Relationships**:
- Belongs to one `Domain`
- Has many `TestSuite` entities

**Persistence**: Memory only (computed from test results)

---

### 4. TestSuite

**Purpose**: Logical grouping of related tests (from JUnit XML <testsuite>)

**C# Model**:
```csharp
public record TestSuite
{
    public required string Id { get; init; }              // From XML attribute: <testsuite name="...">
    public required string Name { get; init; }            // e.g., "Regression Tests for Alarm Reports"
    public required string FeatureId { get; init; }       // Parent feature
    public required string DomainId { get; init; }        // Parent domain
    public List<string> TestIds { get; init; } = new();   // Child test results
}
```

**Validation Rules**:
- `Id` must be unique within feature
- Extracted from JUnit XML `<testsuite name="">` attribute

**Relationships**:
- Belongs to one `Feature`
- Has many `TestResult` entities

**Persistence**: Memory only (computed from test results)

---

### 5. Configuration

**Purpose**: Multi-dimensional test configuration (Version × TestType × NamedConfig × Domain)

**C# Model**:
```csharp
public record Configuration
{
    public required string Id { get; init; }              // e.g., "dev_E2E_Default1_Core"
    public required string Version { get; init; }         // e.g., "dev", "PXrel114"
    public required string VersionDisplay { get; init; }  // e.g., "Development", "1.14.0"
    public required string TestType { get; init; }        // e.g., "E2E"
    public required string NamedConfig { get; init; }     // e.g., "Default1", "Win2022"
    public required string Domain { get; init; }          // e.g., "Core"
    public string? OsDb { get; init; }                    // e.g., "Windows Server 2022 / MSSQL Express" (if derivable)
}
```

**Validation Rules**:
- `Id` = `{Version}_{TestType}_{NamedConfig}_{Domain}` (composite key)
- Version mapping: `PXrel114` → `VersionDisplay = "1.14.0"`
- Only valid tested combinations exist (not all permutations)

**Relationships**:
- Has many `Build` entities
- Has many `TestResult` entities

**Persistence**: Memory only (extracted from file paths)

---

### 6. Build

**Purpose**: Single test run execution session

**C# Model**:
```csharp
public record Build
{
    public required string Id { get; init; }              // e.g., "Release-252_181639"
    public required int BuildNumber { get; init; }        // e.g., 252
    public required string Timestamp { get; init; }       // e.g., "181639" (HHMMSS)
    public required DateTime RunDateTime { get; init; }   // Parsed from timestamp or file metadata
    public required string ConfigurationId { get; init; } // Parent configuration
    public int TotalTests { get; set; }                   // Computed: count of all tests in build
    public int PassedTests { get; set; }                  // Computed: count of passed tests
    public int FailedTests { get; set; }                  // Computed: count of failed tests
    public int SkippedTests { get; set; }                 // Computed: count of skipped tests
    public double PassRate { get; set; }                  // Computed: PassedTests / (TotalTests - SkippedTests)
}
```

**Validation Rules**:
- `Id` = `"Release-{BuildNumber}_{Timestamp}"`
- `BuildNumber` > 0
- `TotalTests` = PassedTests + FailedTests + SkippedTests

**Relationships**:
- Belongs to one `Configuration`
- Has many `TestResult` entities

**Persistence**: Memory only (computed from test results)

---

## User Data Entities (LiteDB Persistence)

### 7. UserBaseline

**Purpose**: User-marked baseline build for comparison

**C# Model**:
```csharp
public class UserBaseline
{
    public int Id { get; set; }                           // LiteDB auto-increment primary key
    public required string ConfigurationId { get; set; }  // e.g., "dev_E2E_Default1_Core"
    public required string BuildId { get; set; }          // e.g., "Release-252_181639"
    public required string Label { get; set; }            // e.g., "Last Stable Release"
    public required string MarkedBy { get; set; }         // Username
    public required DateTime MarkedDate { get; set; }     // When baseline was set
    public string? Notes { get; set; }                    // Optional notes
}
```

**Validation Rules**:
- `ConfigurationId` must exist in memory cache
- `BuildId` must exist for that configuration
- `Label` cannot be empty

**Persistence**: LiteDB collection "userbaselines"

---

### 8. SavedFilterConfiguration

**Purpose**: User-saved filter presets

**C# Model**:
```csharp
public class SavedFilterConfiguration
{
    public int Id { get; set; }                           // LiteDB auto-increment primary key
    public required string Name { get; set; }             // e.g., "Core Domain - Failed Tests Only"
    public required string SavedBy { get; set; }          // Username
    public required DateTime SavedDate { get; set; }      // When filter was saved
    public List<string> Domains { get; set; } = new();    // Selected domains (empty = all)
    public List<string> Features { get; set; } = new();   // Selected features (empty = all)
    public List<string> Versions { get; set; } = new();   // Selected versions (empty = all)
    public List<string> NamedConfigs { get; set; } = new(); // Selected configs (empty = all)
    public List<string> Machines { get; set; } = new();   // Selected machines (empty = all)
    public DateTime? DateFrom { get; set; }               // Date range start (null = no limit)
    public DateTime? DateTo { get; set; }                 // Date range end (null = no limit)
    public bool? OnlyFailures { get; set; }               // Filter to failures only (null = all)
    public bool? HideFlakyTests { get; set; }             // Hide flaky tests (null = show all)
}
```

**Validation Rules**:
- `Name` cannot be empty
- At least one filter criterion should be set (or allow "Show All" preset)

**Persistence**: LiteDB collection "savedfilters"

---

### 9. UserComment

**Purpose**: User annotations on tests or builds

**C# Model**:
```csharp
public class UserComment
{
    public int Id { get; set; }                           // LiteDB auto-increment primary key
    public required string TargetType { get; set; }       // "Test" or "Build"
    public required string TargetId { get; set; }         // TestResult.Id or Build.Id
    public required string Comment { get; set; }          // Comment text
    public required string Author { get; set; }           // Username
    public required DateTime CreatedDate { get; set; }    // When comment was added
    public DateTime? EditedDate { get; set; }             // Last edit timestamp (null if never edited)
}
```

**Validation Rules**:
- `Comment` cannot be empty
- `TargetId` should exist in memory cache (soft validation - comment persists even if target removed)

**Persistence**: LiteDB collection "usercomments"

---

### 10. DashboardConfiguration

**Purpose**: Custom dashboard layout for users

**C# Model**:
```csharp
public class DashboardConfiguration
{
    public int Id { get; set; }                           // LiteDB auto-increment primary key
    public required string Name { get; set; }             // e.g., "My Morning Dashboard"
    public required string Owner { get; set; }            // Username
    public required DateTime CreatedDate { get; set; }    // When dashboard was created
    public List<DashboardWidget> Widgets { get; set; } = new(); // Ordered list of widgets
}

public class DashboardWidget
{
    public required string Type { get; set; }             // "MorningTriage", "TrendChart", "FlakyTests", etc.
    public int Position { get; set; }                     // Display order
    public Dictionary<string, string> Config { get; set; } = new(); // Widget-specific config (e.g., domain filter)
}
```

**Validation Rules**:
- `Name` cannot be empty
- `Widgets` can be empty (blank dashboard)

**Persistence**: LiteDB collection "dashboards"

---

## Computed/View Entities (Derived On-Demand)

### 11. FlakyTest

**Purpose**: Test with inconsistent pass/fail results

**C# Model**:
```csharp
public record FlakyTest
{
    public required string TestFullName { get; init; }    // Test identifier
    public required double FlakinessScore { get; init; }  // Percentage of instability (0-100)
    public required int TotalRuns { get; init; }          // Number of runs analyzed
    public required int PassCount { get; init; }          // Number of passes in window
    public required int FailCount { get; init; }          // Number of fails in window
    public required List<TestResultSummary> History { get; init; } // Pass/fail timeline
    public required DateTime LastFlaky { get; init; }     // Most recent flaky behavior
    public bool IsCleared { get; init; }                  // True if last M runs all passed
}

public record TestResultSummary
{
    public required string BuildId { get; init; }
    public required TestStatus Status { get; init; }
    public required DateTime Timestamp { get; init; }
}
```

**Computation Logic**:
```csharp
// Rolling window: last 20 runs
var recentRuns = testResults.OrderByDescending(r => r.Timestamp).Take(20);
var passCount = recentRuns.Count(r => r.Status == TestStatus.Pass);
var failCount = recentRuns.Count(r => r.Status == TestStatus.Fail);
var total = passCount + failCount;

var flakinessScore = total > 0 
    ? (Math.Min(passCount, failCount) / (double)total) * 100 
    : 0;

var isFlaky = flakinessScore > threshold && passCount > 0 && failCount > 0;

// Clear flag if last 10 consecutive runs all passed
var lastM = recentRuns.Take(10).ToList();
var isCleared = lastM.Count == 10 && lastM.All(r => r.Status == TestStatus.Pass);
```

**Persistence**: Computed on-demand from TestResult cache (no persistence)

---

### 12. FailureGroup

**Purpose**: Cluster of tests failing with similar error messages

**C# Model**:
```csharp
public record FailureGroup
{
    public required string Pattern { get; init; }         // Representative error message
    public required int TestCount { get; init; }          // Number of tests in group
    public required List<string> TestIds { get; init; }   // List of TestResult.Id in group
    public required List<string> AffectedDomains { get; init; } // Unique domains affected
    public required List<string> AffectedFeatures { get; init; } // Unique features affected
    public required string CommonStackTrace { get; init; } // Shared portion of stack trace
}
```

**Computation Logic**:
```csharp
// Step 1: Exact match grouping
var exactGroups = failedTests
    .GroupBy(t => NormalizeErrorMessage(t.ErrorMessage))
    .Select(g => new { Pattern = g.Key, Tests = g.ToList() });

// Step 2: Fuzzy matching for near-duplicates (Levenshtein distance > 80%)
// (Implementation details in research.md)
```

**Persistence**: Computed on-demand from TestResult cache (no persistence)

---

### 13. DomainSummary

**Purpose**: Aggregated pass/fail statistics for a domain

**C# Model**:
```csharp
public record DomainSummary
{
    public required string DomainId { get; init; }
    public required string DisplayName { get; init; }
    public required int TotalTests { get; init; }
    public required int PassedTests { get; init; }
    public required int FailedTests { get; init; }
    public required int SkippedTests { get; init; }
    public required double PassRate { get; init; }
    public required List<FeatureSummary> Features { get; init; }
}
```

**Computation Logic**:
```csharp
var summary = testResults
    .Where(r => r.DomainId == domainId)
    .GroupBy(r => r.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToList();
```

**Caching**: Cache for 5 minutes, invalidate on new test import

**Persistence**: Memory only (cached computation)

---

### 14. ConfigurationMatrix

**Purpose**: Pass rates for all valid configuration combinations

**C# Model**:
```csharp
public record ConfigurationMatrix
{
    public required List<string> Versions { get; init; }        // Unique versions (rows)
    public required List<string> NamedConfigs { get; init; }    // Unique configs (columns)
    public required Dictionary<string, ConfigCell> Cells { get; init; } // Key: "{Version}_{NamedConfig}"
}

public record ConfigCell
{
    public required string ConfigurationId { get; init; }
    public required int TotalTests { get; init; }
    public required int PassedTests { get; init; }
    public required int FailedTests { get; init; }
    public required double PassRate { get; init; }
    public bool HasData { get; init; } = true;                  // False if config combo not tested
}
```

**Computation Logic**:
```csharp
// Only include valid tested combinations (not all permutations)
var validConfigs = testResults
    .Select(r => r.ConfigurationId)
    .Distinct();

var matrix = validConfigs
    .Select(configId => {
        var results = testResults.Where(r => r.ConfigurationId == configId);
        return new ConfigCell { ... };
    });
```

**Persistence**: Memory only (cached computation)

---

### 15. QualityTrend

**Purpose**: Historical pass rate trend for a domain

**C# Model**:
```csharp
public record QualityTrend
{
    public required string DomainId { get; init; }
    public required List<TrendDataPoint> DataPoints { get; init; } // Last 30 builds
    public required TrendDirection Direction { get; init; }        // Improving/Stable/Degrading
    public required double CurrentPassRate { get; init; }          // Latest build pass rate
    public required double AveragePassRate { get; init; }          // Average over period
}

public record TrendDataPoint
{
    public required string BuildId { get; init; }
    public required DateTime Timestamp { get; init; }
    public required double PassRate { get; init; }
}

public enum TrendDirection
{
    Improving,  // Pass rate increasing
    Stable,     // Pass rate consistent (±2%)
    Degrading   // Pass rate decreasing
}
```

**Computation Logic**:
```csharp
// Compare last 5 builds to previous 5 builds
var recentAvg = lastBuilds.Take(5).Average(b => b.PassRate);
var previousAvg = lastBuilds.Skip(5).Take(5).Average(b => b.PassRate);
var direction = recentAvg > previousAvg + 2 ? TrendDirection.Improving
              : recentAvg < previousAvg - 2 ? TrendDirection.Degrading
              : TrendDirection.Stable;
```

**Persistence**: Memory only (cached computation)

---

### 16. ExecutionTimeMetric

**Purpose**: Test performance tracking over time

**C# Model**:
```csharp
public record ExecutionTimeMetric
{
    public required string TestFullName { get; init; }
    public required double CurrentTimeSeconds { get; init; }      // Latest execution time
    public required double AverageTimeSeconds { get; init; }      // Average over last 10 runs
    public required double PercentageChange { get; init; }        // % change from average
    public bool IsRegression { get; init; }                       // True if >20% slower than average
    public required List<ExecutionTimeSample> History { get; init; }
}

public record ExecutionTimeSample
{
    public required string BuildId { get; init; }
    public required double TimeSeconds { get; init; }
    public required DateTime Timestamp { get; init; }
}
```

**Computation Logic**:
```csharp
var avgTime = history.Average(h => h.TimeSeconds);
var percentChange = ((currentTime - avgTime) / avgTime) * 100;
var isRegression = percentChange > 20; // 20% threshold configurable
```

**Persistence**: Memory only (cached computation)

---

### 17. HeatmapCell

**Purpose**: Feature × Build intersection in Failure Heatmap

**C# Model**:
```csharp
public record HeatmapCell
{
    public required string FeatureId { get; init; }
    public required string BuildId { get; init; }
    public required HeatmapStatus Status { get; init; }
    public required int FailureCount { get; init; }
    public required int TotalTests { get; init; }
}

public enum HeatmapStatus
{
    Pass,       // All tests passed (green)
    Partial,    // 1-5 failures (yellow)
    Fail,       // 6+ failures (red)
    NoData      // No tests ran (gray)
}
```

**Computation Logic**:
```csharp
var status = failureCount == 0 ? HeatmapStatus.Pass
           : failureCount <= 5 ? HeatmapStatus.Partial
           : HeatmapStatus.Fail;
```

**Persistence**: Memory only (cached computation)

---

### 18. TriageNewFailure

**Purpose**: Test that passed yesterday but failed today (Morning Triage)

**C# Model**:
```csharp
public record TriageNewFailure
{
    public required string TestFullName { get; init; }
    public required string DomainId { get; init; }
    public required string FeatureId { get; init; }
    public required List<string> AffectedConfigs { get; init; }   // Configs where it failed
    public required string ErrorMessage { get; init; }
    public required DateTime FailedOn { get; init; }              // Today's run timestamp
}
```

**Computation Logic**:
```csharp
// Find tests: Status=Pass in yesterday's run AND Status=Fail in today's run
var newFailures = todayTests
    .Where(t => t.Status == TestStatus.Fail)
    .Join(yesterdayTests, 
          today => today.TestFullName, 
          yesterday => yesterday.TestFullName, 
          (today, yesterday) => new { Today = today, Yesterday = yesterday })
    .Where(x => x.Yesterday.Status == TestStatus.Pass);
```

**Persistence**: Memory only (computed on-demand)

---

### 19. PolarionTicketReference

**Purpose**: Extracted Polarion work item ID from test name

**C# Model**:
```csharp
public record PolarionTicketReference
{
    public required string TicketId { get; init; }        // e.g., "PEXC-28044"
    public required string Url { get; init; }             // e.g., "https://polarion.company.com/work-items/PEXC-28044"
    public required string TestFullName { get; init; }    // Test containing this reference
}
```

**Extraction Logic**:
```csharp
// Regex: "PEXC-\d+" pattern
var matches = Regex.Matches(testName, @"PEXC-\d+");
var tickets = matches.Select(m => new PolarionTicketReference 
{ 
    TicketId = m.Value, 
    Url = $"{polarionBaseUrl}/work-items/{m.Value}" 
});
```

**Persistence**: Memory only (extracted on-demand)

---

### 20. Machine

**Purpose**: Test execution machine identifier

**C# Model**:
```csharp
public record Machine
{
    public required string Hostname { get; init; }        // e.g., "WIN-TEST-01"
    public int TotalTestsRun { get; init; }               // Count of tests executed on this machine
}
```

**Extraction**: From JUnit XML `<testsuite hostname="">` attribute (if present)

**Persistence**: Memory only (computed from test results)

---

## Indexing Strategy

### Primary Index (ConcurrentDictionary)
- **Key**: `TestResult.Id` (composite: `{ConfigId}_{BuildId}_{TestFullName}`)
- **Value**: `TestResult` object
- **Lookup**: O(1)

### Secondary Indices (Dictionary<string, List<string>>)
- **ByDomain**: `DomainId` → List of `TestResult.Id`
- **ByFeature**: `FeatureId` → List of `TestResult.Id`
- **ByConfiguration**: `ConfigurationId` → List of `TestResult.Id`
- **ByBuild**: `BuildId` → List of `TestResult.Id`
- **ByTestName**: `TestFullName` → List of `TestResult.Id` (for historical lookup)

**Build Indices On Import**:
```csharp
private void RebuildIndices()
{
    byDomain.Clear();
    byFeature.Clear();
    // ... etc
    
    foreach (var result in testResults.Values)
    {
        byDomain.GetOrAdd(result.DomainId, _ => new List<string>()).Add(result.Id);
        byFeature.GetOrAdd(result.FeatureId, _ => new List<string>()).Add(result.Id);
        // ... etc
    }
}
```

---

## Relationships Summary

```
Domain (1) ─────< Feature (M)
Feature (1) ────< TestSuite (M)
TestSuite (1) ──< TestResult (M)

Configuration (1) ──< Build (M)
Build (1) ──────────< TestResult (M)

TestResult (M) ──> Domain (1)
TestResult (M) ──> Feature (1)
TestResult (M) ──> Configuration (1)
TestResult (M) ──> Build (1)
```

---

## State Transitions

### TestResult
- **Created**: When imported from JUnit XML
- **Immutable**: Never updated (historical record)
- **Deleted**: Never (kept in memory until app restart)

### FlakyTest
- **Flagged**: When flakiness score > threshold
- **Cleared**: When last M consecutive runs pass
- **Re-flagged**: If fails again after clearing

### UserBaseline
- **Created**: User marks a build as baseline
- **Updated**: User changes label or notes
- **Deleted**: User removes baseline

---

## Performance Considerations

### Memory Estimates
- `TestResult`: ~400 bytes/object
- 30M results × 400 bytes ≈ **12 GB RAM**
- Indices: ~20% overhead ≈ **3 GB RAM**
- **Total**: ~15 GB RAM (fits in 16-32 GB server)

### Query Performance
- Filter by Domain: O(n) scan via `byDomain` index → ~50ms for 500K results
- Hierarchy drill-down: O(1) lookups via indices → <10ms
- Flaky detection: O(n log n) sort + O(20) window scan → ~100ms per test

### Caching Strategy
- Aggregated summaries cached for 5 minutes
- Invalidate on new test import
- Lazy computation (compute on first access, cache result)

---

## Migration Strategy

**Phase 1**: Implement in-memory cache with basic entities (TestResult, Domain, Feature, Build)
**Phase 2**: Add computed entities (FlakyTest, FailureGroup, QualityTrend)
**Phase 3**: Add user data persistence (LiteDB for baselines, comments, filters)
**Phase 4**: Optimize with advanced indices and caching

---

**Next Steps**: Generate API contracts (service interfaces) in contracts/ folder
