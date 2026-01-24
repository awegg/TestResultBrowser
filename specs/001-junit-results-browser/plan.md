# Implementation Plan: JUnit Test Results Browser

**Branch**: `001-junit-results-browser` | **Date**: 2026-01-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-junit-results-browser/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a web-based JUnit test results browser that polls a shared file system for test result XML files, caches them in memory for fast filtering/querying, and provides specialized Morning Triage and Release Triage workflows for multi-dimensional configuration matrices (Version × OS/DB × NamedConfig). The system extracts organizational hierarchy (Domain → Feature → Suite → Test) from file paths and provides <2-second filtering performance across 10,000+ tests and 50+ configurations.

## Technical Context

**Language/Version**: C# / .NET 8.0 (LTS)
**Framework**: Blazor Server (with option to migrate to Blazor WebAssembly/United later)  
**Primary Dependencies**:
  - ASP.NET Core 8.0 (web host)
  - System.Xml.Linq (JUnit XML parsing)
  - System.IO.FileSystemWatcher (file system polling)
  - LiteDB or SQLite (user data persistence: baselines, comments, filters)
  - Blazor Component Libraries: MudBlazor or Radzen (UI components, charts, matrix visualizations)
  
**Storage**:
  - **Test Results**: In-memory cache (ConcurrentDictionary<string, TestResult>) - no persistence
  - **User Data**: LiteDB or SQLite database (baselines, comments, saved filters) - lightweight file-based persistence
  - **File System**: Read-only access to shared network file system (UNC path or mapped drive)

**Testing**:
  - xUnit (unit tests for parsers, filters, aggregation logic)
  - bUnit (Blazor component testing)
  - Integration tests (file watcher, XML parsing with sample_data)
  
**Target Platform**: 
  - **Primary**: Docker container (Linux/Windows containers) on internal network
  - **Alternative**: Windows Server (IIS or Kestrel self-hosted)
  - **Containerization**: Docker Compose for single-server deployment
  
**Project Type**: Web application (Blazor Server with backend services)

**Performance Goals**:
  - <2 seconds filtering 10,000+ test results (in-memory LINQ queries)
  - <3 seconds initial page load (Morning Triage dashboard)
  - <5 seconds configuration matrix rendering (50+ configs)
  - <1 second hierarchy expansion (Domain → Feature drill-down)
  - 15-minute file system polling cycle (configurable)
  
**Constraints**:
  - 10-20GB RAM required (30M test results × ~400 bytes/result ≈ 12GB + overhead)
  - Single-server deployment (no distributed caching needed initially)
  - Read-only file system access (no write permissions required)
  - **Docker deployment**: Volume mounts for test results and user data persistence
  - **Platform compatibility**: Cross-platform .NET 8.0 (Windows/Linux containers)
  
**Scale/Scope**:
  - 50+ configuration combinations (Version × TestType × NamedConfig × Domain)
  - 60+ builds per configuration (historical data from Release-2 to Release-252+)
  - 10,000+ unique tests per build
  - Estimated: 30M total test results in memory
  - 5-20 concurrent users (team leads, QA engineers, release managers)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Status**: ✅ **PASS** (Constitution template is generic; no project-specific rules violated)

**Note**: The .specify/memory/constitution.md file contains only placeholder/template content with no actual enforced principles. This project can proceed with the Blazor/C# .NET stack without constitutional violations.

**When constitution is defined, verify**:
- Test-first development practices (if TDD mandated)
- Library/CLI structure requirements (if architectural patterns enforced)
- Technology stack constraints (if specific frameworks required/prohibited)

## Project Structure

### Documentation (this feature)

```text
specs/001-junit-results-browser/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── api.json         # Internal API contracts (if backend/frontend separation)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── TestResultBrowser.Web/           # Blazor Server web app
│   ├── Program.cs                   # App entry point, service registration
│   ├── Pages/                       # Blazor pages/routes
│   │   ├── Index.razor              # Dashboard/landing page
│   │   ├── MorningTriage.razor      # US 1 - Morning Triage view
│   │   ├── ReleaseTriage.razor      # US 2 - Release Triage view
│   │   ├── DomainExplorer.razor     # US 3 - Domain browsing
│   │   ├── Trends.razor             # US 4 - Historical trends
│   │   └── FeatureImpact.razor      # US 11 - Feature impact analysis
│   ├── Components/                  # Reusable Blazor components
│   │   ├── TestHierarchy.razor      # 4-level tree (Domain→Feature→Suite→Test)
│   │   ├── ConfigMatrix.razor       # Configuration matrix grid
│   │   ├── FilterPanel.razor        # Multi-dimensional filter UI
│   │   ├── FlakyTestBadge.razor     # Flaky test indicator
│   │   └── TrendChart.razor         # Pass/fail trend visualization
│   ├── Services/                    # Business logic services (injected)
│   │   ├── ITestDataService.cs      # In-memory test result cache interface
│   │   ├── TestDataService.cs       # Manages in-memory ConcurrentDictionary
│   │   ├── IFileWatcherService.cs   # File system polling interface
│   │   ├── FileWatcherService.cs    # Polls file share every 15 min
│   │   ├── IJUnitParserService.cs   # XML parsing interface
│   │   ├── JUnitParserService.cs    # Parses JUnit XML to domain models
│   │   ├── IUserDataService.cs      # User-generated data (baselines, comments, filters)
│   │   ├── UserDataService.cs       # LiteDB/SQLite persistence
│   │   ├── ITriageService.cs        # Morning/Release triage logic
│   │   ├── TriageService.cs         # New failures, config diff, grouping
│   │   ├── IFlakyDetectionService.cs # Flaky test detection
│   │   └── FlakyDetectionService.cs # Calculates flakiness scores
│   ├── Models/                      # Domain models (C# records/classes)
│   │   ├── TestResult.cs            # Individual test result
│   │   ├── TestSuite.cs             # Test suite grouping
│   │   ├── Feature.cs               # Feature entity
│   │   ├── Domain.cs                # Domain entity
│   │   ├── Configuration.cs         # Version × NamedConfig × OS/DB
│   │   ├── Build.cs                 # Release build metadata
│   │   ├── FlakyTest.cs             # Flaky test record
│   │   ├── FailureGroup.cs          # Grouped failures
│   │   └── UserBaseline.cs          # User-marked baseline build
│   ├── Parsers/                     # XML parsing and metadata extraction
│   │   ├── JUnitXmlParser.cs        # Low-level XML → TestResult mapping
│   │   ├── FilePathParser.cs        # Extracts Domain/Feature/Config from paths
│   │   └── VersionMapper.cs         # PXrel114 → 1.14.0 mapping
│   ├── appsettings.json             # Configuration (file share path, thresholds)
│   └── wwwroot/                     # Static assets (CSS, JS, images)
│
├── TestResultBrowser.Core/          # Shared core library (optional, for future CLI)
│   ├── Models/                      # Same as above (shared if extracted)
│   └── Interfaces/                  # Service contracts
│
tests/
├── TestResultBrowser.Tests.Unit/    # Unit tests (xUnit)
│   ├── Services/
│   │   ├── JUnitParserServiceTests.cs    # XML parsing edge cases
│   │   ├── FilePathParserTests.cs        # Path extraction logic
│   │   ├── TriageServiceTests.cs         # New failure detection
│   │   └── FlakyDetectionServiceTests.cs # Flakiness scoring
│   └── Parsers/
│       └── VersionMapperTests.cs         # PXrel code mapping
│
├── TestResultBrowser.Tests.Integration/  # Integration tests
│   ├── FileWatcherTests.cs           # File system polling with test data
│   ├── EndToEndTriageTests.cs        # Load sample_data → verify triage output
│   └── PerformanceTests.cs           # Verify <2s filtering on 10K results
│
└── TestResultBrowser.Tests.Component/    # Blazor component tests (bUnit)
    ├── MorningTriageTests.cs         # US 1 acceptance scenarios
    ├── ConfigMatrixTests.cs          # Matrix rendering
    └── FilterPanelTests.cs           # Multi-dimensional filtering
```

**Structure Decision**: Blazor Server web application with separate test projects. The architecture uses:
- **Blazor Server** (not WebAssembly) for lower latency and direct server-side memory access to cached test results
- **Service layer pattern** with dependency injection for testability
- **In-memory caching** via ConcurrentDictionary in TestDataService (thread-safe for concurrent users)
- **Background service** (IHostedService) for FileWatcherService to poll file system every 15 minutes
- **LiteDB or SQLite** for lightweight user data persistence (no heavyweight database required)
- **Component library** (MudBlazor/Radzen) for rich UI components (charts, grids, matrix visualizations)

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**Status**: N/A - No constitutional violations detected (constitution template is not yet populated with project-specific rules)

**Future Considerations** (if complexity concerns arise):
- In-memory caching at 10-20GB RAM may seem complex, but simpler alternatives (database queries) cannot meet <2s performance requirement
- Background FileWatcher service adds complexity, but eliminates need for manual upload/API integration
- Blazor Server vs WebAssembly: Server chosen for memory access locality and lower latency to cached data
