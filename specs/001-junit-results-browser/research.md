# Research & Technical Decisions: JUnit Test Results Browser

**Feature**: 001-junit-results-browser  
**Date**: 2026-01-16  
**Purpose**: Document technical research and decisions for Blazor/C# .NET implementation

---

## 1. Blazor Hosting Model: Server vs WebAssembly vs United

**Decision**: **Blazor Server**

**Rationale**:
- **Memory Access**: Test results cached in server memory (10-20GB) - direct access without serialization
- **Performance**: <2s filtering requirement requires server-side LINQ queries on in-memory data
- **Network**: Internal network deployment - low latency to server, SignalR overhead acceptable
- **Simplicity**: No need to serialize/transfer 30M test results to client browser
- **File System**: Direct access to shared file system (UNC paths) from server

**Alternatives Considered**:
1. **Blazor WebAssembly**:
   - ❌ Cannot hold 10-20GB data in browser memory
   - ❌ Cannot access file system directly
   - ❌ Would require backend API + data pagination (violates <2s performance goal)
   - ✅ Better for public internet deployment (not needed here)

2. **Blazor United (.NET 8+)**:
   - ⚠️ Hybrid model with SSR + interactivity
   - ❌ Adds complexity without clear benefit for internal tool
   - ⚠️ Still requires server-side data access
   - 🔮 Consider for future if offline capability needed

3. **ASP.NET MVC + JavaScript SPA (React/Vue)**:
   - ❌ Requires separate frontend build pipeline
   - ❌ Loses C# code sharing between backend/frontend
   - ❌ More complex for .NET-focused team

**Risk**: SignalR connection stability over long sessions (users leave browser open all day)
**Mitigation**: Implement connection resilience, reconnect logic, and periodic keep-alive

---

## 2. Blazor Component Library: MudBlazor vs Radzen vs Telerik

**Decision**: **MudBlazor** (open source)

**Rationale**:
- **Cost**: Free and open-source (no licensing for Pexcite team)
- **Rich Components**: Charts (TrendChart), Data Grids (TestHierarchy), Dialogs, Tooltips
- **Material Design**: Modern, clean UI out-of-box
- **Active Development**: Regular updates, large community
- **Performance**: Lightweight, works well with Blazor Server
- **Documentation**: Excellent docs and examples

**Alternatives Considered**:
1. **Radzen Blazor Components** (free tier):
   - ✅ Also free and open-source
   - ✅ Good chart library
   - ⚠️ Slightly less polished UI than MudBlazor
   - ⚠️ Smaller community

2. **Telerik UI for Blazor** (commercial):
   - ✅ Enterprise-grade components
   - ✅ Advanced data grid features
   - ❌ **~$1000+/year/developer licensing cost**
   - ❌ Overkill for internal tool

3. **Syncfusion Blazor** (commercial):
   - ✅ Rich component suite
   - ❌ **~$800+/year/developer licensing cost**
   - ❌ Heavier bundle size

4. **Plain Bootstrap + Custom Components**:
   - ✅ Full control
   - ❌ Requires building charts, grids, matrix from scratch (~4-6 weeks extra dev time)

**Final Choice**: **MudBlazor** for cost, quality, and velocity

---

## 3. In-Memory Data Structure Design

**Decision**: **Layered Caching Strategy**

**Primary Cache**: `ConcurrentDictionary<string, TestResult>`
- Key: `{ConfigId}_{BuildId}_{TestFullName}` (e.g., `"dev_E2E_Default1_Core_Release-252_AlarmManagerTests.TestDownloadReport"`)
- Value: `TestResult` object (status, duration, error, timestamp)
- Thread-safe for concurrent user queries

**Aggregated Views** (computed on-demand, cached for 5 minutes):
- `Dictionary<string, DomainSummary>` - Pass rates per domain
- `Dictionary<string, ConfigSummary>` - Pass rates per configuration
- `Dictionary<string, FeatureSummary>` - Pass rates per feature

**Hierarchical Index** (for fast drill-down):
- `Dictionary<string, Domain>` → `List<Feature>` → `List<TestSuite>` → `List<TestResult>`

**Rationale**:
- Flat primary cache = O(1) lookups by full test identifier
- Aggregated views = pre-computed for dashboard performance
- Hierarchical index = fast Domain → Feature → Suite → Test navigation
- Memory-efficient: ~400 bytes/test × 30M tests ≈ 12GB (fits in 16-32GB RAM server)

**Alternatives Considered**:
1. **Graph Database (Neo4j, OrientDB)**:
   - ❌ Adds external database dependency
   - ❌ Slower than in-memory for read-heavy workload
   - ❌ Violates "memory-only for test data" principle

2. **Redis Cache**:
   - ⚠️ Good for distributed scenarios
   - ❌ Single-server deployment doesn't need distributed cache
   - ❌ Adds network hop latency
   - 🔮 Consider for future multi-server scale-out

3. **SQL Database with In-Memory Tables (SQL Server, PostgreSQL)**:
   - ❌ Requires database license/installation
   - ❌ Slower than ConcurrentDictionary for filtering
   - ❌ Violates spec requirement: "memory-only for test data"

**Performance Validation**:
- Benchmark: 10,000 tests × 50 configs = 500K results
- Filter by Domain + Status = O(n) scan with LINQ `Where()`
- Expected: 500K × 100ns (LINQ predicate) = 50ms + rendering overhead = <2s ✅

---

## 4. File System Watcher Implementation

**Decision**: **IHostedService with Timer-Based Polling**

**Implementation**:
```csharp
public class FileWatcherService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScanFileSystemForNewResults();
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
```

**Rationale**:
- Simple, reliable timer-based polling every 15 minutes
- No dependency on FileSystemWatcher events (which can be unreliable over network shares)
- Scans top-level directories for new `Release-{BuildNumber}_{Timestamp}/` folders
- Compares against known builds in memory, imports only new XML files
- Runs in background thread, doesn't block web requests

**Alternatives Considered**:
1. **FileSystemWatcher API (event-driven)**:
   - ❌ **Unreliable over UNC network paths** (Windows limitation)
   - ❌ Can miss events during network hiccups
   - ❌ Generates too many events (file created, modified, renamed)
   - ⚠️ Works better for local file system

2. **Manual "Refresh Now" Only (no background polling)**:
   - ❌ Users must remember to refresh manually
   - ❌ Defeats "automatic import within 15 minutes" requirement (SC-012)
   - ✅ Simple implementation
   - **Verdict**: Keep manual refresh as *supplement*, not replacement

3. **Azure Blob Storage Change Feed (if cloud migration)**:
   - ⚠️ Future option if moving from file share to cloud storage
   - ❌ Requires Azure migration (not current architecture)

**Performance**: Scanning 50 config directories × 60 builds = 3000 folders = ~2-5 seconds (acceptable every 15 min)

---

## 5. User Data Persistence: LiteDB vs SQLite

**Decision**: **LiteDB** (NoSQL file-based database)

**Rationale**:
- **Simplicity**: Single `.db` file, no installation, xcopy deployment
- **C# Native**: LINQ queries, POCO serialization (no SQL string writing)
- **Schema-less**: Easy to add new user data types (comments, tags, custom filters) without migrations
- **Performance**: Fast enough for <1000 user records (baselines, saved filters)
- **Embedded**: No separate database process

**Example Usage**:
```csharp
using var db = new LiteDatabase("userdata.db");
var baselines = db.GetCollection<UserBaseline>("baselines");
baselines.Insert(new UserBaseline { ConfigId = "dev_E2E_Default1_Core", BuildNumber = 252, MarkedBy = "jdoe" });
```

**Alternatives Considered**:
1. **SQLite**:
   - ✅ More mature, widely used
   - ✅ SQL query language (pro and con)
   - ❌ Requires Entity Framework Core or ADO.NET (more boilerplate)
   - ❌ Schema migrations needed for new features
   - ⚠️ Still a valid choice, but LiteDB is more .NET-idiomatic

2. **JSON Files (System.Text.Json)**:
   - ✅ Simplest option
   - ❌ No indexing (slow for queries like "all baselines for user X")
   - ❌ No concurrent write safety (file locking issues)
   - ❌ Grows unbounded (no automatic cleanup)
   - ✅ Good for single-user prototype, bad for multi-user

3. **PostgreSQL / SQL Server**:
   - ❌ **Overkill** for <1000 user records
   - ❌ Requires separate database installation/management
   - ❌ Violates "lightweight persistence" spec requirement

**Migration Path**: If LiteDB proves insufficient, can export to SQLite with minimal code changes (both use POCO models)

---

## 6. Flaky Test Detection Algorithm

**Decision**: **Rolling Window Instability Calculation**

**Algorithm**:
```csharp
// For each test, look at last N runs
var recentRuns = testResults.OrderByDescending(r => r.Timestamp).Take(N);
var passCount = recentRuns.Count(r => r.Status == TestStatus.Pass);
var failCount = recentRuns.Count(r => r.Status == TestStatus.Fail);
var total = passCount + failCount;

// Flakiness score = percentage of minority outcome
var flakinessScore = total > 0 
    ? (Math.Min(passCount, failCount) / (double)total) * 100 
    : 0;

// Flag as flaky if:
// 1. Instability > threshold (default 30%)
// 2. Has both passes AND fails in window
var isFlaky = flakinessScore > threshold && passCount > 0 && failCount > 0;

// Clear flaky flag if:
// - Last M consecutive runs all passed (default M=10)
var lastM = recentRuns.Take(M).ToList();
var clearedFlaky = lastM.Count == M && lastM.All(r => r.Status == TestStatus.Pass);
```

**Parameters (configurable in appsettings.json)**:
- `N` = 20 (window size: last 20 runs)
- `threshold` = 30 (flag if >30% instability)
- `M` = 10 (clear flag after 10 consecutive passes)

**Rationale**:
- Simple, deterministic, explainable to users
- Captures intermittent failures (e.g., 6 passes, 4 fails in last 10 = 40% instability)
- Auto-clears when test becomes stable (avoids stale flags)
- No ML complexity needed for Phase 1

**Alternatives Considered**:
1. **Machine Learning (Statistical Process Control)**:
   - ✅ Could detect more subtle patterns
   - ❌ Requires training data, model deployment
   - ❌ "Black box" to users (hard to explain why flagged)
   - 🔮 Consider for Phase 2 if simple algorithm insufficient

2. **Fixed Threshold (e.g., "failed 2 of last 5")**:
   - ❌ Too rigid (doesn't scale to longer histories)
   - ❌ Misses patterns like "fails every 10th run"

3. **Markov Chain / State Transitions**:
   - ❌ Over-engineered for Phase 1
   - ⚠️ Could detect "fails after pass" patterns but added complexity

**Testing**: Use sample_data to find tests with mixed pass/fail outcomes, verify algorithm flags them correctly

---

## 7. Failure Grouping Algorithm

**Decision**: **Levenshtein Distance on Error Messages with Thresholding**

**Algorithm**:
1. Extract error messages from all failed tests
2. For each unique error message:
   - Compute similarity to all other error messages (Levenshtein distance)
   - Group messages with similarity > 80% (configurable)
3. Cluster tests by error group
4. Display groups sorted by test count (largest groups first)

**Example**:
```csharp
var errorGroups = failedTests
    .GroupBy(t => NormalizeErrorMessage(t.ErrorMessage)) // First pass: exact match
    .Select(g => new FailureGroup 
    { 
        Pattern = g.Key, 
        Tests = g.ToList(), 
        Count = g.Count() 
    })
    .OrderByDescending(g => g.Count);

// Then fuzzy matching for near-duplicates:
// "Connection timeout to AlarmService" 
// "Connection timeout to AlarmService on line 45"
// → Same group (80% similarity)
```

**Rationale**:
- Levenshtein distance is standard for string similarity
- 80% threshold balances precision (don't over-group) vs recall (catch similar errors)
- First pass exact matching = fast
- Second pass fuzzy matching = catches minor variations

**Alternatives Considered**:
1. **Regex Pattern Extraction**:
   - ✅ Good for structured error messages (e.g., SQL error codes)
   - ❌ Requires manual regex crafting per error type
   - ❌ Brittle (breaks when error formats change)

2. **Natural Language Processing (TF-IDF, Word Embeddings)**:
   - ✅ More sophisticated similarity
   - ❌ Requires ML libraries (e.g., ML.NET, external NLP service)
   - ❌ Overkill for short error strings
   - 🔮 Consider if Levenshtein insufficient

3. **Stack Trace Similarity (call graph matching)**:
   - ✅ More precise (groups by code path, not just message)
   - ❌ Requires parsing stack traces (complex)
   - ❌ Not all JUnit XML includes stack traces
   - 🔮 Consider for Phase 2

**Library**: Use existing `Fastenshtein` NuGet package (fast Levenshtein implementation)

---

## 8. Version Code Mapping Strategy

**Decision**: **Regex Pattern Matching with Hardcoded Rules + Fallback**

**Implementation**:
```csharp
public static string MapVersionCode(string versionCode)
{
    // Rule 1: "dev" → "Development"
    if (versionCode.Equals("dev", StringComparison.OrdinalIgnoreCase))
        return "Development";
    
    // Rule 2: "PXrel{digits}" → "1.{major}.{minor}"
    var match = Regex.Match(versionCode, @"PXrel(\d+)", RegexOptions.IgnoreCase);
    if (match.Success)
    {
        var code = int.Parse(match.Groups[1].Value);
        // PXrel114 → 1.14 (digits 1-2 = major, digit 3 = minor if present)
        var major = code / 10;
        var minor = code % 10;
        return minor == 0 ? $"1.{major}.0" : $"1.{major}.{minor}";
    }
    
    // Fallback: return as-is
    return versionCode;
}
```

**Rationale**:
- Handles known patterns from spec: `PXrel114` = 1.14.0, `PXrel1441` = 1.14.1
- Falls back gracefully for unknown formats (displays raw code)
- Simple, no external config file needed for Phase 1

**Alternatives Considered**:
1. **Configuration File (JSON mapping)**:
   - ✅ More flexible (add new versions without code changes)
   - ❌ Requires manual updates when new versions added
   - ❌ File management overhead
   - 🔮 Consider if version patterns become more complex

2. **Database Table (version registry)**:
   - ❌ Overkill for simple mapping
   - ❌ Requires admin UI to manage mappings

3. **Semantic Versioning Parser (SemVer library)**:
   - ❌ Pexcite versions don't follow SemVer format (`PXrel114` is proprietary)

---

## 9. Performance Optimization Strategies

**Decision**: **Layered Caching + Lazy Loading + Virtualization**

**Strategies**:

1. **Aggregated View Caching**:
   - Cache pass rate summaries for 5 minutes (avoid re-computing on every page load)
   - Invalidate cache when new test results imported

2. **Virtualized Rendering (MudBlazor Virtualize)**:
   - Only render visible rows in large test lists (1000+ tests)
   - Reduces DOM nodes, improves browser performance

3. **Async Loading**:
   - Load test details on-demand (lazy load stack traces, history charts)
   - Initial page load shows only summary data

4. **Parallel XML Parsing**:
   - Use `Parallel.ForEach()` to parse XML files concurrently during import
   - Utilize multi-core CPU during 15-minute import cycle

**Rationale**:
- Meets <2s filtering requirement via in-memory LINQ + caching
- Meets <1s hierarchy expansion via indexed lookups
- Reduces browser memory (virtualization)

**Benchmarking Plan**:
- Load full sample_data (60+ builds × 50 configs)
- Measure filtering time for "all failed tests in Domain=Core" (should be <2s)
- Measure Morning Triage page load (should be <3s)

---

## 10. CI/CD Integration Consideration

**Decision**: **Phase 1: File-Based Import Only** (CI/CD optional, not required)

**Rationale**:
- Spec assumes tests already write to file share (existing infrastructure)
- Adding CI/CD integration is a Phase 2 enhancement
- File watcher works with or without CI/CD (agnostic to how XML files arrive)

**Future CI/CD Integration Path** (if desired later):
1. Keep file-based import working (backward compatible)
2. Add optional webhook endpoint: POST `/api/import/trigger` with build metadata
3. CI/CD pipeline calls webhook after test run completes (triggers immediate import)
4. Reduces latency from 15 minutes (polling) to <1 minute (push-based)

**Benefit of File-First Approach**:
- Works with legacy test infrastructure (no CI/CD changes needed)
- Zero coupling to Jenkins/Azure Pipelines/GitHub Actions
- Team can start using browser immediately

---

## Summary of Key Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| **Hosting** | Blazor Server | Direct memory access, <2s performance |
| **UI Library** | MudBlazor | Free, modern, rich components |
| **Data Cache** | ConcurrentDictionary | Thread-safe, O(1) lookups, <2s filtering |
| **File Watcher** | Timer-based polling (15 min) | Reliable over network shares |
| **User Data** | LiteDB | Simple, NoSQL, C# native |
| **Flaky Detection** | Rolling window instability | Simple, configurable, explainable |
| **Failure Grouping** | Levenshtein distance | Standard similarity, fast |
| **Version Mapping** | Regex pattern + fallback | Handles known PXrel format |
| **Performance** | Caching + virtualization | Meets <2s filtering requirement |
| **CI/CD** | Optional, Phase 2 | File-based import works standalone |

**Risks & Mitigations**:
- **Risk**: SignalR disconnects during long sessions → **Mitigation**: Auto-reconnect, keep-alive
- **Risk**: 10-20GB RAM insufficient as data grows → **Mitigation**: Add data retention policy (e.g., keep last 90 days)
- **Risk**: File share network outages → **Mitigation**: Retry logic, stale data warnings

**Next Steps**: Proceed to Phase 1 - Generate data-model.md, contracts/, quickstart.md
