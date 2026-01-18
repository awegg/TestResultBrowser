# Tasks: JUnit Test Results Browser

**Input**: Design documents from `/specs/001-junit-results-browser/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests are NOT explicitly requested in the feature specification, so NO test tasks are included. Implementation tasks only.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. P0 (Morning/Release Triage) stories are highest priority for MVP.

**🎯 SCOPE**: Ultra-Lean MVP approach - 141 tasks focused on core triage workflows + essential features including configuration history view. Deferred features marked with ⏸️ and can be added post-MVP based on user feedback.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md project structure:
- **Web App**: `src/TestResultBrowser.Web/`
- **Pages**: `src/TestResultBrowser.Web/Pages/`
- **Components**: `src/TestResultBrowser.Web/Components/`
- **Services**: `src/TestResultBrowser.Web/Services/`
- **Models**: `src/TestResultBrowser.Web/Models/`
- **Parsers**: `src/TestResultBrowser.Web/Parsers/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

**Effort**: 2-3 hours | **Status**: ✅ COMPLETE

- [x] T001 Create solution structure: src/TestResultBrowser.Web/ (Blazor Server project, .NET 8.0)
- [x] T002 Initialize Blazor Server project with Program.cs and required NuGet packages (MudBlazor, LiteDB, System.Xml.Linq)
- [x] T003 [P] Configure appsettings.json with FileSharePath, PollingIntervalMinutes, FlakyTestThresholds, PolarionBaseUrl
- [x] T004 [P] Create directory structure: Pages/, Components/, Services/, Models/, Parsers/ under src/TestResultBrowser.Web/
- [x] T005 [P] Setup MudBlazor in Program.cs (AddMudServices) and _Imports.razor
- [x] T006 [P] Create sample_data validation script to test file path parsing logic

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**Effort**: 2-3 days (16-24 hours) | **Status**: ✅ COMPLETE

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 [P] Create TestStatus enum in src/TestResultBrowser.Web/Models/TestStatus.cs
- [x] T008 [P] Create TestResult record in src/TestResultBrowser.Web/Models/TestResult.cs (data-model.md entity #1)
- [x] T009 [P] Create Domain record in src/TestResultBrowser.Web/Models/Domain.cs (data-model.md entity #2)
- [x] T010 [P] Create Feature record in src/TestResultBrowser.Web/Models/Feature.cs (data-model.md entity #3)
- [x] T011 [P] Create TestSuite record in src/TestResultBrowser.Web/Models/TestSuite.cs (data-model.md entity #4)
- [x] T012 [P] Create Configuration record in src/TestResultBrowser.Web/Models/Configuration.cs (data-model.md entity #5)
- [x] T013 [P] Create Build record in src/TestResultBrowser.Web/Models/Build.cs (data-model.md entity #6)
- [x] T014 Create IVersionMapperService interface in src/TestResultBrowser.Web/Services/IVersionMapperService.cs
- [x] T015 Implement VersionMapperService in src/TestResultBrowser.Web/Services/VersionMapperService.cs (PXrel114→1.14.0, dev→Development mapping logic)
- [x] T016 Create IFilePathParserService interface in src/TestResultBrowser.Web/Services/IFilePathParserService.cs
- [x] T017 Implement FilePathParserService in src/TestResultBrowser.Web/Services/FilePathParserService.cs (parse {Version}_{TestType}_{NamedConfig}_{Domain} paths)
- [x] T018 Create IJUnitParserService interface in src/TestResultBrowser.Web/Services/IJUnitParserService.cs
- [x] T019 Implement JUnitParserService in src/TestResultBrowser.Web/Services/JUnitParserService.cs (parse XML to TestResult objects)
- [x] T020 Create ITestDataService interface in src/TestResultBrowser.Web/Services/ITestDataService.cs (in-memory cache interface from contracts/)
- [x] T021 Implement TestDataService in src/TestResultBrowser.Web/Services/TestDataService.cs (ConcurrentDictionary cache, secondary indices)
- [x] T022 Create IFileWatcherService interface in src/TestResultBrowser.Web/Services/IFileWatcherService.cs (background polling)
- [x] T023 Implement FileWatcherService as BackgroundService in src/TestResultBrowser.Web/Services/FileWatcherService.cs (15-min timer polling)
- [x] T024 Register all services in Program.cs (AddSingleton for TestDataService, AddHostedService for FileWatcherService, etc.)
- [x] T025 Create MainLayout.razor in src/TestResultBrowser.Web/Shared/ with MudBlazor sidebar navigation structure
- [x] T026 Create shared FilterPanel component in src/TestResultBrowser.Web/Components/FilterPanel.razor (Domain/Feature/Version/Config multi-select)
- [x] T027 Implement initial file system scan on application startup (load all historical data into memory cache)
- [x] T027a Implement chunked loading in TestDataService (load 100k records at a time, yield to prevent UI freeze)
- [x] T027b Add progress indicator for initial scan (percentage loaded, ETA, current build being processed)
- [x] T027c Implement memory monitoring in FileWatcherService (log memory usage, warn if approaching limits)
- [x] T027d Add configuration validation on startup in Program.cs (verify FileSharePath exists, URLs reachable, thresholds valid)

**Checkpoint**: ✅ Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Morning Triage of New Failures (Priority: P0) 🎯 MVP

**Goal**: Enable daily triage of newly failing tests grouped by domain/feature with configuration indicators

**Effort**: 1.5-2 days (12-16 hours) | **Status**: ✅ COMPLETE

**Independent Test**: Load results from two consecutive nightly runs (yesterday/today) and verify Morning Triage view highlights newly failing tests, groups by domain/feature, shows affected configurations graphically

### Implementation for User Story 1

- [x] T028 [P] [US1] Create TriageNewFailure record in src/TestResultBrowser.Web/Models/TriageNewFailure.cs (computed entity from data-model.md)
- [x] T029 [P] [US1] Create TriageFixedTest record in src/TestResultBrowser.Web/Models/TriageFixedTest.cs
- [x] T030 [P] [US1] Create MorningTriageResult record in src/TestResultBrowser.Web/Models/MorningTriageResult.cs
- [x] T031 [US1] Create ITriageService interface in src/TestResultBrowser.Web/Services/ITriageService.cs (GetMorningTriageAsync method)
- [x] T032 [US1] Implement TriageService.GetMorningTriageAsync in src/TestResultBrowser.Web/Services/TriageService.cs (compare today vs yesterday builds, detect new failures)
- [x] T033 [US1] Create MorningTriage.razor page in src/TestResultBrowser.Web/Pages/MorningTriage.razor
- [x] T034 [US1] Implement stats cards in MorningTriage.razor (Total New Failures, Fixed Tests, Still Failing, Pass Rate)
- [x] T035 [US1] Create TestHierarchy component in src/TestResultBrowser.Web/Components/TestHierarchy.razor (collapsible Domain→Feature→Suite→Test tree)
- [x] T036 [US1] Integrate TestHierarchy component into MorningTriage.razor showing new failures grouped by domain/feature
- [x] T037 [US1] Create ConfigVisualIndicator component in src/TestResultBrowser.Web/Components/ConfigVisualIndicator.razor (graphical mini-matrix showing affected configs)
- [x] T038 [US1] Add ConfigVisualIndicator to each failure in TestHierarchy to show which Version/NamedConfig combinations are affected
- [x] T039 [US1] Implement filtering by specific domain in MorningTriage.razor using FilterPanel component
- [x] T040 [US1] Add navigation link to MorningTriage page in MainLayout.razor sidebar

**Checkpoint**: ✅ User Story 1 (Morning Triage) is fully functional - users can identify new failures, see affected domains/features, and view configuration-specific failures

---

## Phase 4: User Story 2 - Release Triage During Release Cycles (Priority: P0) 🎯 MVP

**Goal**: Enable release readiness assessment across all configurations with configuration matrix view

**Effort**: 1.5-2 days (12-16 hours) | **Status**: ✅ COMPLETE

**Independent Test**: Load release candidate results across all OS/DB/Version configurations and verify Release Triage dashboard shows configuration matrix with pass rates and highlights failing configurations

### Implementation for User Story 2

- [x] T041 [P] [US2] Create ReleaseTriageResult record in src/TestResultBrowser.Web/Models/ReleaseTriageResult.cs
- [x] T042 [P] [US2] Create ComparisonMetrics record in src/TestResultBrowser.Web/Models/ComparisonMetrics.cs
- [x] T043 [P] [US2] Create ConfigurationMatrix record in src/TestResultBrowser.Web/Models/ConfigurationMatrix.cs (data-model.md entity #14)
- [x] T044 [P] [US2] Create ConfigCell record in src/TestResultBrowser.Web/Models/ConfigCell.cs
- [x] T045 [US2] Implement TriageService.GetReleaseTriageAsync in src/TestResultBrowser.Web/Services/TriageService.cs (generate config matrix, compute domain/feature pass rates)
- [x] T046 [US2] Create ReleaseTriage.razor page in src/TestResultBrowser.Web/Pages/ReleaseTriage.razor
- [x] T047 [US2] Implement stats cards in ReleaseTriage.razor (Overall Pass Rate, Passing Configs, Failing Configs, Release Status)
- [x] T048 [US2] Create ConfigMatrix component in src/TestResultBrowser.Web/Components/ConfigMatrix.razor (Version×NamedConfig grid with color-coded cells)
- [x] T049 [US2] Integrate ConfigMatrix into ReleaseTriage.razor showing only valid tested combinations
- [x] T050 [US2] Add click handlers to ConfigMatrix cells to drill into specific configuration failures
- [x] T051 [US2] Implement domain-level pass rate summary in ReleaseTriage.razor with trend indicators (↑↓→)
- [x] T052 [US2] Add comparison to previous release candidate in ReleaseTriage.razor (highlight regressed/improved tests)
- [x] T053 [US2] Add navigation link to ReleaseTriage page in MainLayout.razor sidebar

**Checkpoint**: ✅ User Stories 1 AND 2 are both complete - Morning Triage for daily workflow, Release Triage for release readiness

---

## Phase 5: User Story 11 - Automatic Test Result Import (Priority: P1)

**Goal**: Enable automatic polling of shared file system for new test results every 15 minutes

**Effort**: 1-1.5 days (8-12 hours) | **Status**: Complete ✅

**Independent Test**: Place new JUnit XML files on shared file system and verify they are detected and imported within 15 minutes

### Implementation for User Story 11

- [x] T054 [US11] Implement file system scanning logic in FileWatcherService.ExecuteAsync (detect new Release-{BuildNumber} folders)
- [x] T055 [US11] Implement incremental import in FileWatcherService (track known builds, import only new XML files)
- [x] T056 [US11] Add error handling in FileWatcherService for malformed XML files (log errors, continue processing valid files)
- [x] T056c [US11] Add disk space monitoring in FileWatcherService (warn if memory usage exceeds 80% of configured limit)
- [x] T057 [US11] Implement duplicate detection in FileWatcherService (avoid re-importing same build)
- [x] T058 [US11] Add manual "Refresh Now" button to System Status page (calls IFileWatcherService.ScanNowAsync)
- [x] T059 [US11] Create System Status page in src/TestResultBrowser.Web/Components/Pages/SystemStatus.razor showing scanner status, cache stats, data coverage, recent builds, monitored configurations, and memory management
- [x] T060 [US11] Add navigation link to System Status page in NavMenu.razor sidebar
- [x] T061 [US11] Implement real-time notification in UI when new files are imported (SignalR with TestDataHub broadcasting to SystemStatus and MorningTriage pages)
- [x] T056a [US11] Add network failure retry logic in FileWatcherService (exponential backoff, max 3 retries with 2s/4s/8s delays)
- [x] T056b [US11] Add permission error handling in FileWatcherService (UnauthorizedAccessException + IOException handlers, skip inaccessible files with guidance logging)

**Checkpoint**: System automatically imports new test results, handles network failures with retry logic, gracefully recovers from permission errors, and provides comprehensive monitoring dashboard

**✅ Phase 5 COMPLETE**: All 11 tasks including error recovery implemented

---

## Phase 6: User Story 5 - Configuration History View (Priority: P0) 🎯 MVP

**Goal**: Enable browsing hierarchical test results with multi-build history across filtered configurations

**Effort**: 2-3 days (16-24 hours) | **Status**: ✅ COMPLETE

**Independent Test**: Select a configuration/machine, verify hierarchical test tree displays with last N builds showing pass/fail counts per build, and visual indicators (green/red cells)

**Features**:
- Configuration/Release dropdown filter
- Machine/NamedConfig filter
- Hierarchical tree: Domain → Feature → Test Suite → Test (expandable/collapsible)
- Multi-column history showing last 5-10 builds
- Color-coded cells: Green (all passed), Red (failures), with pass/fail counts (e.g., "36/36", "35/36")
- Click to expand/collapse tree nodes
- Summary stats at top: Latest Run, Total Tests, Passed, Failed, Skipped

### Implementation for User Story 5

- [x] T062 [P] [US5] Create ConfigurationHistoryResult record in src/TestResultBrowser.Web/Models/ConfigurationHistoryResult.cs
- [x] T063 [P] [US5] Create HistoryColumn record in src/TestResultBrowser.Web/Models/HistoryColumn.cs (buildId, date, pass/fail counts)
- [x] T064 [P] [US5] Create HierarchyNode record in src/TestResultBrowser.Web/Models/HierarchyNode.cs (name, level, children, history data)
- [x] T065 [US5] Create IConfigurationHistoryService interface in src/TestResultBrowser.Web/Services/IConfigurationHistoryService.cs
- [x] T066 [US5] Implement ConfigurationHistoryService.GetConfigurationHistoryAsync in src/TestResultBrowser.Web/Services/ConfigurationHistoryService.cs
- [x] T067 [US5] Implement BuildHierarchyTree method (organize tests into Domain → Feature → Suite → Test tree)
- [x] T068 [US5] Implement GetLastNBuilds method (retrieve last N builds for selected configuration)
- [x] T069 [US5] Implement CalculateHistoryForNode method (aggregate pass/fail counts per build per node)
- [x] T070 [US5] Create ConfigurationHistory.razor page in src/TestResultBrowser.Web/Pages/ConfigurationHistory.razor
- [x] T071 [US5] Add configuration/release dropdown filter in ConfigurationHistory.razor
- [x] T072 [US5] Add machine/named config filter dropdown in ConfigurationHistory.razor
- [x] T073 [US5] Implement summary stats cards (Latest Run, Total Tests, Passed %, Failed, Skipped)
- [x] T074 [US5] Create HierarchyTreeView component in src/TestResultBrowser.Web/Components/HierarchyTreeView.razor
- [x] T075 [US5] Implement expandable/collapsible tree nodes with indentation in HierarchyTreeView
- [x] T076 [US5] Create HistoryGrid component in src/TestResultBrowser.Web/Components/HistoryGrid.razor
- [x] T077 [US5] Render multi-column history grid with build headers (date, buildId)
- [x] T078 [US5] Implement color-coded cells in HistoryGrid (green for all pass, red for failures)
- [x] T079 [US5] Display pass/fail counts in each cell (e.g., "36/36", "35/36")
- [x] T080 [US5] Add "Expand All" / "Collapse All" buttons to toolbar
- [x] T081 [US5] Add navigation link to ConfigurationHistory page in NavMenu.razor sidebar

**Checkpoint**: Configuration History View provides complete test overview with historical context for any configuration

**✅ Phase 6 COMPLETE**: All 20 tasks including Configuration History page, hierarchy tree with multi-build history, filtering, stats cards, and navigation integration implemented

---

## Phase 7: User Story 12 - Flaky Test Detection & Management (Priority: P1)

**Goal**: Identify and filter tests that fail inconsistently to reduce triage noise

**Effort**: 1.5-2 days (12-16 hours) | **Status**: Not Started

**Independent Test**: Load test results where specific tests have mixed pass/fail outcomes across consecutive runs and verify system flags them as flaky

### Implementation for User Story 12

- [ ] T082 [P] [US12] Create FlakyTest record in src/TestResultBrowser.Web/Models/FlakyTest.cs (data-model.md entity #11)
- [ ] T083 [P] [US12] Create TestResultSummary record in src/TestResultBrowser.Web/Models/TestResultSummary.cs
- [ ] T084 [US12] Create IFlakyDetectionService interface in src/TestResultBrowser.Web/Services/IFlakyDetectionService.cs
- [ ] T085 [US12] Implement FlakyDetectionService in src/TestResultBrowser.Web/Services/FlakyDetectionService.cs (rolling window calculation, 20 runs, 30% threshold)
- [ ] T086 [US12] Implement FlakyDetectionService.DetectFlakyTestsAsync (analyze all tests in cache, compute flakiness scores)
- [ ] T087 [US12] Implement FlakyDetectionService.CheckTestFlakinessAsync (check single test for flakiness)
- [ ] T088 [US12] Implement auto-clear logic in FlakyDetectionService (clear flag after 10 consecutive passes)
- [ ] T089 [US12] Create FlakyTestBadge component in src/TestResultBrowser.Web/Components/FlakyTestBadge.razor (🔀 icon with percentage)
- [ ] T090 [US12] Integrate FlakyTestBadge into TestHierarchy component for flaky tests
- [ ] T091 [US12] Create FlakyTests.razor page in src/TestResultBrowser.Web/Pages/FlakyTests.razor listing all identified flaky tests
- [ ] T092 [US12] Add "Hide Flaky Tests" toggle button to toolbar in MainLayout.razor (filters tests with flakiness score >30%)
- [ ] T093 [US12] Implement flaky test filtering in MorningTriage.razor (respect "Hide Flaky Tests" toggle)
- [ ] T094 [US12] Add pass/fail history timeline visualization in FlakyTests.razor showing recent 20 runs
- [ ] T095 [US12] Add navigation link to FlakyTests page in MainLayout.razor sidebar

**Checkpoint**: Flaky test detection reduces triage noise by 20-30% by filtering out intermittent failures

---

## Phase 8: User Story 13 - Polarion Integration (Priority: P1)

**Goal**: Auto-link Polarion ticket IDs in test names for immediate context during triage

**Effort**: 0.5-1 day (4-8 hours) | **Status**: Not Started

**Independent Test**: Load test results with Polarion ticket references (PEXC-28044) and verify ticket IDs are clickable links to Polarion work items

### Implementation for User Story 13

- [ ] T096 [P] [US13] Create PolarionTicketReference record in src/TestResultBrowser.Web/Models/PolarionTicketReference.cs (data-model.md entity #19)
- [ ] T097 [US13] Create IPolarionLinkService interface in src/TestResultBrowser.Web/Services/IPolarionLinkService.cs
- [ ] T098 [US13] Implement PolarionLinkService in src/TestResultBrowser.Web/Services/PolarionLinkService.cs (regex extraction of PEXC-\d+ pattern)
- [ ] T099 [US13] Implement PolarionLinkService.ExtractTicketIds (extract all ticket IDs from test name)
- [ ] T100 [US13] Implement PolarionLinkService.GenerateTicketUrl (construct URL using configured Polarion base URL)
- [ ] T101 [US13] Create PolarionLink component in src/TestResultBrowser.Web/Components/PolarionLink.razor (clickable badge)
- [ ] T102 [US13] Integrate PolarionLink component into TestHierarchy component for all tests with ticket references
- [ ] T103 [US13] Add Polarion base URL configuration to appsettings.json (PolarionBaseUrl setting)
- [ ] T104 [US13] Display multiple Polarion links when test name contains multiple ticket IDs

**Checkpoint**: Polarion integration eliminates manual lookup, saves minutes per failure during triage

---

## Phase 9: User Story 14 - Failure Grouping by Root Cause (Priority: P1)

**Goal**: Group failures by similar error patterns to identify root causes instead of individual test noise

**Effort**: 1-1.5 days (8-12 hours) | **Status**: Not Started

**Independent Test**: Load test results where multiple tests fail with similar error messages and verify system clusters them into groups

### Implementation for User Story 14

- [ ] T105 [P] [US14] Create FailureGroup record in src/TestResultBrowser.Web/Models/FailureGroup.cs (data-model.md entity #12)
- [ ] T106 [US14] Create IFailureGroupingService interface in src/TestResultBrowser.Web/Services/IFailureGroupingService.cs
- [ ] T107 [US14] Implement FailureGroupingService in src/TestResultBrowser.Web/Services/FailureGroupingService.cs (Levenshtein distance algorithm, 80% similarity threshold)
- [ ] T108 [US14] Implement FailureGroupingService.GroupFailuresAsync (cluster failed tests by error message similarity)
- [ ] T109 [US14] Implement exact match grouping first (same error message → same group)
- [ ] T110 [US14] Implement fuzzy matching for near-duplicates (Levenshtein distance >80%)
- [ ] T111 [US14] Create FailureGroups.razor page in src/TestResultBrowser.Web/Pages/FailureGroups.razor
- [ ] T112 [US14] Implement failure group summary cards in FailureGroups.razor (error pattern, test count, affected domains/features)
- [ ] T113 [US14] Add drill-down capability in FailureGroups.razor to show all tests in a group
- [ ] T114 [US14] Integrate failure grouping into MorningTriage.razor (group new failures by error pattern)
- [ ] T115 [US14] Add navigation link to FailureGroups page in MainLayout.razor sidebar

**Checkpoint**: Failure grouping helps focus on root causes - fixing one issue resolves many failures

---

## Phase 10: User Story 3 - View Test Results by Domain (Priority: P1)

**Goal**: Enable domain-specific filtering to isolate relevant test results

**Effort**: 1 day (8 hours) | **Status**: Not Started

**Independent Test**: Upload JUnit results with domain metadata and verify filtering by domain displays only that domain's results

### Implementation for User Story 3

- [ ] T116 [P] [US3] Create DomainSummary record in src/TestResultBrowser.Web/Models/DomainSummary.cs (data-model.md entity #13)
- [ ] T117 [P] [US3] Create FeatureSummary record in src/TestResultBrowser.Web/Models/FeatureSummary.cs
- [ ] T118 [US3] Implement ITestDataService.GetDomainSummary in TestDataService (aggregate pass/fail counts for domain)
- [ ] T119 [US3] Implement ITestDataService.GetFeatureSummary in TestDataService (aggregate pass/fail counts for feature)
- [ ] T120 [US3] Create DomainExplorer.razor page in src/TestResultBrowser.Web/Pages/DomainExplorer.razor
- [ ] T121 [US3] Implement domain filter dropdown in DomainExplorer.razor using FilterPanel component
- [ ] T122 [US3] Display Domain→Feature→Suite→Test hierarchy in DomainExplorer.razor using TestHierarchy component
- [ ] T123 [US3] Show pass/fail counts at each hierarchy level in DomainExplorer.razor
- [ ] T124 [US3] Implement expand/collapse all functionality in DomainExplorer.razor
- [ ] T125 [US3] Add navigation link to DomainExplorer page in MainLayout.razor sidebar

**Checkpoint**: Domain filtering provides isolated views for domain-specific quality assessment

---

## Phase 11: User Story 10 - Save and Reuse Filter Configurations (Priority: P3) ✅ MVP

**Goal**: Enable users to save filter presets for repeated use

**Effort**: 1 day (8 hours) | **Status**: Not Started

**Independent Test**: Create and save filter configuration, then verify it can be recalled and applied correctly

### Implementation for User Story 10

- [ ] T126 [P] [US10] Create SavedFilterConfiguration record in src/TestResultBrowser.Web/Models/SavedFilterConfiguration.cs (data-model.md entity #8)
- [ ] T127 [P] [US10] Create DashboardConfiguration record in src/TestResultBrowser.Web/Models/DashboardConfiguration.cs (data-model.md entity #10)
- [ ] T128 [US10] Create IUserDataService interface in src/TestResultBrowser.Web/Services/IUserDataService.cs (contracts/ interface #9)
- [ ] T129 [US10] Implement UserDataService in src/TestResultBrowser.Web/Services/UserDataService.cs (LiteDB CRUD operations for SavedFilterConfiguration)
- [ ] T130 [US10] Initialize LiteDB database connection in Program.cs (connection string from appsettings.json)
- [ ] T131 [US10] Create SaveFilterDialog component in src/TestResultBrowser.Web/Components/SaveFilterDialog.razor (name, description, current filter state)
- [ ] T132 [US10] Create LoadFilterDropdown component in src/TestResultBrowser.Web/Components/LoadFilterDropdown.razor (list saved filters, apply on selection)
- [ ] T133 [US10] Integrate SaveFilterDialog into FilterPanel component (Save button opens dialog)
- [ ] T134 [US10] Integrate LoadFilterDropdown into FilterPanel component (Load dropdown applies saved filter)

**Checkpoint**: Saved filters improve workflow efficiency by eliminating repetitive filter configuration

---

## Phase 12: Polish & Cross-Cutting Concerns ✅ MVP

**Purpose**: Essential improvements for production readiness

**Effort**: 1 day (8 hours) | **Status**: Not Started

- [ ] T135 [P] Add loading spinners to all pages during data fetch operations
- [ ] T136 [P] Implement error boundaries and user-friendly error messages for all components
- [ ] T137 [P] Optimize performance: implement caching for aggregated summaries (5-minute cache)
- [ ] T138 [P] Implement logging for all service operations (Serilog integration)
- [ ] T139 [P] Create deployment guide in docs/deployment.md (IIS configuration, appsettings)
- [ ] T140 Code cleanup: remove unused imports, apply consistent naming conventions
- [ ] T141 Run quickstart.md validation (verify setup steps work for new developers)

---

# ⏸️ DEFERRED FEATURES (Post-MVP)

**Note**: The following phases are deferred until after MVP deployment and user feedback. They represent ~100 additional tasks that can be prioritized based on actual usage patterns.

---

## Phase 13: User Story 6 - Feature Impact Analysis (Priority: P1) ⏸️ DEFERRED

**Goal**: Assess which features are impacted by failures across all configurations

**Effort**: 1 day (8 hours) | **Status**: Deferred

**Independent Test**: Load test results where a feature has failures across multiple configurations and verify Feature Impact view shows all affected tests and configuration matrix

### Implementation for User Story 6

- [ ] T142 [US5] Implement multi-dimensional filter in FilterPanel component (Version + NamedConfig + OS/DB)
- [ ] T143 [US5] Add configuration filter dropdowns to toolbar in MainLayout.razor
- [ ] T144 [US5] Implement ITestDataService.FilterTestResults with multi-dimensional criteria support
- [ ] T145 [US5] Display active filter chips in toolbar showing applied configuration filters
- [ ] T146 [US5] Implement configuration-specific failure highlighting in ConfigMatrix component
- [ ] T147 [US5] Add tooltip to ConfigMatrix cells showing detailed test results for that configuration

**Checkpoint**: Configuration matrix filtering enables identification of environment-specific issues

---

## Phase 14: User Story 5 - Browse Results by Configuration Matrix (Priority: P1) ⏸️ DEFERRED

**Goal**: Filter by configuration dimensions to identify environment-specific patterns

**Effort**: 0.5-1 day (4-8 hours) | **Status**: Deferred

**Independent Test**: Upload multiple JUnit result sets from different dates and verify trend graphs show changes in pass rates

### Implementation for User Story 4

- [ ] T148 [P] [US4] Create QualityTrend record in src/TestResultBrowser.Web/Models/QualityTrend.cs (data-model.md entity #15)
- [ ] T149 [P] [US4] Create TrendDataPoint record in src/TestResultBrowser.Web/Models/TrendDataPoint.cs
- [ ] T150 [P] [US4] Create TrendDirection enum in src/TestResultBrowser.Web/Models/TrendDirection.cs
- [ ] T151 [US4] Create IQualityTrendService interface in src/TestResultBrowser.Web/Services/IQualityTrendService.cs
- [ ] T152 [US4] Implement QualityTrendService in src/TestResultBrowser.Web/Services/QualityTrendService.cs
- [ ] T153 [US4] Implement QualityTrendService.GetDomainTrendAsync (compute pass rate trends over last 30 builds)
- [ ] T154 [US4] Implement trend direction calculation in QualityTrendService (Improving/Stable/Degrading)
- [ ] T155 [US4] Create Trends.razor page in src/TestResultBrowser.Web/Pages/Trends.razor
- [ ] T156 [US4] Integrate MudBlazor chart component in Trends.razor to visualize pass rate trends
- [ ] T157 [US4] Add domain selector in Trends.razor to filter trends by domain
- [ ] T158 [US4] Display trend direction indicators (↑↓→) in Trends.razor
- [ ] T159 [US4] Implement comparison between two specific runs in Trends.razor (highlight new failures, new passes, consistently failing)
- [ ] T160 [US4] Add navigation link to Trends page in MainLayout.razor sidebar

**Checkpoint**: Quality trend tracking enables proactive quality management

---

## Phase 15: User Story 8 - Search Test Results (Priority: P2) ⏸️ DEFERRED

**Goal**: Enable quick search by test name, error message, or failure pattern

**Effort**: 0.5 day (4 hours) | **Status**: Deferred

**Independent Test**: Create database of test results and verify search queries return accurate matches

### Implementation for User Story 8

- [ ] T161 [US8] Add search input box to toolbar in MainLayout.razor
- [ ] T162 [US8] Implement ITestDataService.SearchTests method (search by test name, error message)
- [ ] T163 [US8] Create SearchResults.razor page in src/TestResultBrowser.Web/Pages/SearchResults.razor
- [ ] T137 [US8] Display search results grouped by domain/feature in SearchResults.razor
- [ ] T138 [US8] Implement combined filtering (search + domain + date range) in SearchResults.razor

**Checkpoint**: Search capability improves efficiency for specific test investigations

---

## Phase 16: User Story 4 - Compare Test Results Over Time (Priority: P1) ⏸️ DEFERRED

**Goal**: Track quality trends across multiple test runs

**Effort**: 1.5 days (12 hours) | **Status**: Deferred

**Independent Test**: Upload multiple JUnit result sets from different dates and verify trend graphs show changes in pass rates

### Implementation for User Story 4

- [ ] T137 [P] [US4] Create QualityTrend record in src/TestResultBrowser.Web/Models/QualityTrend.cs (data-model.md entity #15)
- [ ] T138 [P] [US4] Create TrendDataPoint record in src/TestResultBrowser.Web/Models/TrendDataPoint.cs
- [ ] T139 [P] [US4] Create TrendDirection enum in src/TestResultBrowser.Web/Models/TrendDirection.cs
- [ ] T140 [US4] Create IQualityTrendService interface in src/TestResultBrowser.Web/Services/IQualityTrendService.cs
- [ ] T141 [US4] Implement QualityTrendService in src/TestResultBrowser.Web/Services/QualityTrendService.cs
- [ ] T142 [US4] Implement QualityTrendService.GetDomainTrendAsync (compute pass rate trends over last 30 builds)
- [ ] T143 [US4] Implement trend direction calculation in QualityTrendService (Improving/Stable/Degrading)
- [ ] T144 [US4] Create Trends.razor page in src/TestResultBrowser.Web/Pages/Trends.razor
- [ ] T145 [US4] Integrate MudBlazor chart component in Trends.razor to visualize pass rate trends
- [ ] T146 [US4] Add domain selector in Trends.razor to filter trends by domain
- [ ] T147 [US4] Display trend direction indicators (↑↓→) in Trends.razor
- [ ] T148 [US4] Implement comparison between two specific runs in Trends.razor (highlight new failures, new passes, consistently failing)
- [ ] T149 [US4] Add navigation link to Trends page in MainLayout.razor sidebar

**Checkpoint**: Quality trend tracking enables proactive quality management

---

## Phase 17: User Story 16 - Smart Baseline Comparison (Priority: P2) ⏸️ DEFERRED

**Goal**: Compare current build against known stable baseline instead of just previous run

**Effort**: 1 day (8 hours) | **Status**: Deferred

**Independent Test**: Mark specific build as baseline and verify comparison views use baseline correctly

### Implementation for User Story 16

- [ ] T148 [P] [US16] Create UserBaseline class in src/TestResultBrowser.Web/Models/UserBaseline.cs (data-model.md entity #7)
- [ ] T149 [US16] Implement IUserDataService.CreateBaselineAsync in UserDataService
- [ ] T150 [US16] Implement IUserDataService.GetBaselinesByConfigurationAsync in UserDataService
- [ ] T151 [US16] Add "Mark as Baseline" button to ReleaseTriage.razor
- [ ] T152 [US16] Create SetBaselineDialog component in src/TestResultBrowser.Web/Components/SetBaselineDialog.razor (MudDialog for label input)
- [ ] T153 [US16] Add baseline selector dropdown to MorningTriage.razor toolbar (toggle between "Previous Run" and "Baseline")
- [ ] T154 [US16] Modify TriageService.GetMorningTriageAsync to support baseline comparison mode
- [ ] T155 [US16] Display "Compare to Baseline" metrics in MorningTriage.razor (new failures since baseline, fixed since baseline)
- [ ] T156 [US16] Add baseline indicator (⭐) in ReleaseTriage.razor for builds marked as baselines

**Checkpoint**: Baseline comparison enables "distance from stable" assessment during release cycles

---

## Phase 18: User Story 15 - Configuration Diff View (Priority: P2) ⏸️ DEFERRED

**Goal**: Compare same build across different configurations side-by-side

**Effort**: 0.5-1 day (4-8 hours) | **Status**: Deferred

**Independent Test**: Load same Release build from two configurations and verify side-by-side comparison highlights differences

### Implementation for User Story 15

- [ ] T157 [US15] Create ConfigDiff.razor page in src/TestResultBrowser.Web/Pages/ConfigDiff.razor
- [ ] T158 [US15] Add configuration selector dropdowns in ConfigDiff.razor (select two configurations to compare)
- [ ] T159 [US15] Implement side-by-side test result comparison in ConfigDiff.razor
- [ ] T160 [US15] Highlight tests that pass in one config but fail in the other
- [ ] T161 [US15] Display summary showing "X tests ONLY fail on Config A" in ConfigDiff.razor
- [ ] T162 [US15] Add navigation link to ConfigDiff page in MainLayout.razor sidebar

**Checkpoint**: Configuration diff view pinpoints environment-specific failures

---

## Phase 19: User Story 17 - Build Quality Trend Analytics (Priority: P2) ⏸️ DEFERRED

**Goal**: Visualize quality trends over time per domain with alerts

**Effort**: 0.5 day (4 hours) | **Status**: Deferred

**Independent Test**: Load 30 builds of historical data and verify trend graphs show pass rate changes with alerts

### Implementation for User Story 17

- [ ] T163 [US17] Enhance Trends.razor to include quality threshold alerts (<95% pass rate)
- [ ] T164 [US17] Implement domain quality alert highlighting in Trends.razor (red alert for domains below threshold)
- [ ] T165 [US17] Add hover tooltips in Trends.razor showing build number, date, pass rate, failure count
- [ ] T166 [US17] Display trend metric text in Trends.razor (e.g., "Core domain: Degrading ↓ (97% → 92% over last 10 builds)")

**Checkpoint**: Quality trend analytics with alerts enable proactive quality management

---

## Phase 20: User Story 19 - Failure History Heatmap (Priority: P2) ⏸️ DEFERRED

**Goal**: Visual overview of chronically unstable features across recent builds

**Effort**: 1 day (8 hours) | **Status**: Deferred

**Independent Test**: Load results for multiple features across 10 builds and verify heatmap grid displays color-coded stability patterns

### Implementation for User Story 19

- [ ] T167 [P] [US19] Create HeatmapCell record in src/TestResultBrowser.Web/Models/HeatmapCell.cs (data-model.md entity #17)
- [ ] T168 [P] [US19] Create HeatmapStatus enum in src/TestResultBrowser.Web/Models/HeatmapStatus.cs
- [ ] T169 [US19] Create IHeatmapService interface in src/TestResultBrowser.Web/Services/IHeatmapService.cs
- [ ] T170 [US19] Implement HeatmapService in src/TestResultBrowser.Web/Services/HeatmapService.cs
- [ ] T171 [US19] Implement HeatmapService.GenerateHeatmapAsync (generate Feature×Build grid)
- [ ] T172 [US19] Create Heatmap.razor page in src/TestResultBrowser.Web/Pages/Heatmap.razor
- [ ] T173 [US19] Implement heatmap grid visualization in Heatmap.razor (color-coded cells: Green/Yellow/Red/Gray)
- [ ] T174 [US19] Add click handler to heatmap cells in Heatmap.razor to drill into specific feature/build failures
- [ ] T175 [US19] Implement domain filter in Heatmap.razor to show only features within selected domains
- [ ] T176 [US19] Add navigation link to Heatmap page in MainLayout.razor sidebar

**Checkpoint**: Failure heatmap enables visual pattern recognition for chronically unstable features

---

## Phase 21: User Story 18 - Test Execution Time Regression Detection (Priority: P3) ⏸️ DEFERRED

**Goal**: Identify tests slowing down over time to catch performance regressions

**Effort**: 1 day (8 hours) | **Status**: Deferred

**Independent Test**: Load test results where tests show increasing execution times and verify alerts for significant slowdowns

### Implementation for User Story 18

- [ ] T177 [P] [US18] Create ExecutionTimeMetric record in src/TestResultBrowser.Web/Models/ExecutionTimeMetric.cs (data-model.md entity #16)
- [ ] T178 [P] [US18] Create ExecutionTimeSample record in src/TestResultBrowser.Web/Models/ExecutionTimeSample.cs
- [ ] T179 [US18] Implement IQualityTrendService.GetTestExecutionTrendAsync in QualityTrendService
- [ ] T180 [US18] Create SlowTests.razor page in src/TestResultBrowser.Web/Pages/SlowTests.razor
- [ ] T181 [US18] Implement slow test detection in SlowTests.razor (tests with 50%+ execution time increase)
- [ ] T182 [US18] Add sparkline chart component in SlowTests.razor showing execution time trend for each test
- [ ] T183 [US18] Display execution time regression alerts (⚠️ Execution time increased X%) in SlowTests.razor
- [ ] T184 [US18] Add navigation link to SlowTests page in MainLayout.razor sidebar

**Checkpoint**: Execution time regression detection prevents test suite slowdown

---

## Phase 21: User Story 9 - Configure Custom Dashboard (Priority: P2) ⏸️ DEFERRED

**Goal**: Enable personalized dashboards for regular monitoring

**Effort**: 1.5 days (12 hours) | **Status**: Deferred

**Independent Test**: Allow users to select dashboard components and verify dashboard displays only selected data

### Implementation for User Story 9

- [ ] T185 [P] [US9] Create DashboardConfiguration class in src/TestResultBrowser.Web/Models/DashboardConfiguration.cs (data-model.md entity #10)
- [ ] T186 [P] [US9] Create DashboardWidget class in src/TestResultBrowser.Web/Models/DashboardWidget.cs
- [ ] T187 [US9] Implement IUserDataService.CreateDashboardAsync in UserDataService
- [ ] T188 [US9] Implement IUserDataService.GetDashboardsByUserAsync in UserDataService
- [ ] T189 [US9] Create CustomDashboard.razor page in src/TestResultBrowser.Web/Pages/CustomDashboard.razor
- [ ] T190 [US9] Implement dashboard configuration UI in CustomDashboard.razor (select widgets, filters)
- [ ] T191 [US9] Create reusable dashboard widgets (MorningTriageWidget, TrendChartWidget, FlakyTestsWidget, etc.)
- [ ] T192 [US9] Implement drag-and-drop widget arrangement in CustomDashboard.razor
- [ ] T193 [US9] Add "Save Dashboard" button in CustomDashboard.razor
- [ ] T194 [US9] Add dashboard selector dropdown to load saved dashboards
- [ ] T195 [US9] Add navigation link to CustomDashboard page in MainLayout.razor sidebar

**Checkpoint**: Custom dashboards improve efficiency for regular monitoring across 50+ machines

---

## Phase 22: User Story 20 - Permalinks & Collaboration (Priority: P3) ⏸️ DEFERRED

**Goal**: Enable sharing of specific views via URL and user annotations

**Effort**: 1.5 days (12 hours) | **Status**: Deferred

**Independent Test**: Apply filters, generate permalink, open in new session, verify all filters preserved

### Implementation for User Story 20

- [ ] T196 [P] [US20] Create UserComment class in src/TestResultBrowser.Web/Models/UserComment.cs (data-model.md entity #9)
- [ ] T197 [US20] Implement IUserDataService.CreateCommentAsync in UserDataService
- [ ] T198 [US20] Implement IUserDataService.GetCommentsByTargetAsync in UserDataService
- [ ] T199 [US20] Implement permalink generation in all views (encode filters in URL query string)
- [ ] T200 [US20] Implement permalink restoration in all views (decode filters from URL on page load)
- [ ] T201 [US20] Add "Share Link" button to toolbar in MainLayout.razor (generates permalink URL)
- [ ] T202 [US20] Add "Copy Results" button to toolbar in MainLayout.razor (copy selected tests to clipboard in markdown format)
- [ ] T203 [US20] Create CommentDialog component in src/TestResultBrowser.Web/Components/CommentDialog.razor for adding notes
- [ ] T204 [US20] Add comment button (💬) to each test in TestHierarchy component
- [ ] T205 [US20] Display comment indicators (💬 icon) on tests with annotations
- [ ] T206 [US20] Show comment preview on hover in TestHierarchy component

**Checkpoint**: Permalinks and collaboration features improve distributed triage workflows

---

## Phase 23: User Story 7 - Browse Results by Feature Area (Priority: P2) ⏸️ DEFERRED

**Goal**: View test results organized by feature area

**Effort**: 0.5 day (4 hours) | **Status**: Deferred

**Independent Test**: Upload JUnit results with feature tags and verify feature-based grouping

### Implementation for User Story 7

- [ ] T207 [US7] Create FeatureView.razor page in src/TestResultBrowser.Web/Pages/FeatureView.razor
- [ ] T208 [US7] Implement feature selector dropdown in FeatureView.razor
- [ ] T209 [US7] Display all tests for selected feature grouped by test suite in FeatureView.razor
- [ ] T210 [US7] Show overall pass rate and critical failure count for selected feature
- [ ] T211 [US7] Add navigation link to FeatureView page in MainLayout.razor sidebar

**Checkpoint**: Feature-based organization helps stakeholders assess product capability quality

---

## Phase 24: Additional Polish (Optional) ⏸️ DEFERRED

**Purpose**: Nice-to-have improvements for post-MVP releases

**Effort**: 1 day (8 hours) | **Status**: Deferred

- [ ] T212 [P] Add keyboard shortcuts for common actions (F5 for refresh, Ctrl+K for search)
- [ ] T213 [P] Add responsive design adjustments for smaller screens (tablet support)
- [ ] T214 [P] Add telemetry for user actions and performance metrics
- [ ] T215 [P] Create user documentation in docs/user-guide.md based on quickstart.md
- [ ] T216 Refactor common patterns into shared utilities
- [ ] T217 Security hardening: input validation, XSS prevention, secure SignalR configuration

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - US1 (Morning Triage) - Can start after Phase 2
  - US2 (Release Triage) - Can start after Phase 2
  - US11 (Automatic Import) - Can start after Phase 2
  - US12 (Flaky Detection) - Can start after Phase 2
  - US13 (Polarion Integration) - Can start after Phase 2
  - US14 (Failure Grouping) - Can start after Phase 2
  - US3 (Domain View) - Can start after Phase 2
  - US6 (Feature Impact) - Can start after Phase 2
  - US5 (Config Matrix) - Can start after Phase 2
  - US4 (Trends) - Can start after Phase 2
  - US8 (Search) - Can start after Phase 2
  - US10 (Saved Filters) - Can start after Phase 2 (requires LiteDB setup)
  - US16 (Baselines) - Depends on US10 (LiteDB/UserDataService)
  - US15 (Config Diff) - Can start after Phase 2
  - US17 (Trend Analytics) - Depends on US4 (QualityTrendService)
  - US19 (Heatmap) - Can start after Phase 2
  - US18 (Execution Time) - Depends on US4 (QualityTrendService)
  - US9 (Custom Dashboard) - Depends on US10 (LiteDB/UserDataService)
  - US20 (Permalinks) - Depends on US10 (LiteDB/UserDataService)
  - US7 (Feature View) - Can start after Phase 2
- **Polish (Phase 23)**: Depends on all desired user stories being complete

### User Story Dependencies

**Independent User Stories** (can be implemented in parallel after Phase 2):
- US1 (Morning Triage) - No dependencies on other stories
- US2 (Release Triage) - No dependencies on other stories
- US11 (Automatic Import) - No dependencies on other stories
- US12 (Flaky Detection) - No dependencies on other stories
- US13 (Polarion Integration) - No dependencies on other stories
- US14 (Failure Grouping) - No dependencies on other stories
- US3 (Domain View) - No dependencies on other stories
- US6 (Feature Impact) - No dependencies on other stories
- US5 (Config Matrix) - No dependencies on other stories
- US8 (Search) - No dependencies on other stories
- US15 (Config Diff) - No dependencies on other stories
- US7 (Feature View) - No dependencies on other stories

**Dependent User Stories**:
- US4 (Trends) → US17 (Trend Analytics) - US17 requires QualityTrendService from US4
- US4 (Trends) → US18 (Execution Time) - US18 requires QualityTrendService from US4
- US10 (Saved Filters) → US16 (Baselines) - US16 requires UserDataService from US10
- US10 (Saved Filters) → US9 (Custom Dashboard) - US9 requires UserDataService from US10
- US10 (Saved Filters) → US20 (Permalinks) - US20 requires UserDataService from US10
- US2 (Release Triage) → US19 (Heatmap) - US19 uses ConfigurationMatrix concept from US2

### Recommended Implementation Order (Ultra-Lean MVP)

**🎯 MVP (Ultra-Lean) - 6-8 weeks with 2-3 developers (128 tasks, ~13-17 days effort)**:
1. Phase 1: Setup - **2-3 hours** (6 tasks) ✅ COMPLETE
2. Phase 2: Foundational - **2-3 days** (25 tasks) - **INCLUDES MEMORY MANAGEMENT & VALIDATION**
3. Phase 3: US1 (Morning Triage) - **1.5-2 days** (13 tasks) - HIGHEST PRIORITY
4. Phase 4: US2 (Release Triage) - **1.5-2 days** (13 tasks) - HIGHEST PRIORITY
5. Phase 5: US11 (Automatic Import) - **1-1.5 days** (11 tasks) - CRITICAL for automation + **ERROR HANDLING**
6. Phase 6: US12 (Flaky Detection) - **1.5-2 days** (14 tasks) - High ROI for triage efficiency
7. Phase 7: US13 (Polarion Integration) - **0.5-1 day** (9 tasks) - Quick win, high value
8. Phase 8: US14 (Failure Grouping) - **1-1.5 days** (11 tasks) - High ROI for root cause analysis
9. Phase 9: US3 (Domain View) - **1 day** (10 tasks) - Core navigation
10. Phase 10: US10 (Saved Filters) - **1 day** (9 tasks) - **LiteDB INFRASTRUCTURE SETUP**
11. Phase 11: Polish (Essential) - **1 day** (7 tasks) - Production readiness

**Total MVP**: 128 tasks across 11 phases | **Effort**: ~13-17 days (104-136 hours)

**⚠️ CRITICAL NOTE**: US10 sets up LiteDB/UserDataService infrastructure needed for US9, US16, US20 (all deferred). Complete this phase to enable easy addition of these features post-MVP.

**Post-MVP (Based on User Feedback) - Prioritize after deployment (~14-18 days additional effort)**:
- Phase 12: US6 (Feature Impact) - **1 day** (9 tasks) - If release managers need feature-level assessment
- Phase 13: US5 (Config Matrix enhancements) - **0.5-1 day** (6 tasks) - If matrix filtering is heavily requested
- Phase 14: US8 (Search) - **0.5 day** (5 tasks) - If users struggle to find specific tests
- Phase 15: US4 (Trends) - **1.5 days** (13 tasks) - If historical trending is critical
- Phase 16: US16 (Baselines) - **1 day** (9 tasks) - If baseline comparison becomes essential
- Phase 17: US15 (Config Diff) - **0.5-1 day** (6 tasks) - If config-specific debugging is common
- Phase 18: US17 (Trend Analytics) - **0.5 day** (4 tasks) - Depends on US4
- Phase 19: US19 (Heatmap) - **1 day** (10 tasks) - If visual patterns help identify chronic issues
- Phase 20: US18 (Execution Time) - **1 day** (8 tasks) - If performance regression is a concern
- Phase 21: US9 (Custom Dashboard) - **1.5 days** (11 tasks) - If personalization is highly valued
- Phase 22: US20 (Permalinks) - **1.5 days** (11 tasks) - If collaboration/sharing is needed
- Phase 23: US7 (Feature View) - **0.5 day** (5 tasks) - If duplicate of US3/US6
- Phase 24: Additional Polish - **1 day** (6 tasks) - Nice-to-haves

**Total Deferred**: 103 tasks | **Effort**: ~12-18 days (96-144 hours)

### Parallel Opportunities

**Within Setup Phase (Phase 1)**:
- T003, T004, T005, T006 can run in parallel (different files, no dependencies)

**Within Foundational Phase (Phase 2)**:
- T007-T013 (Models) can run in parallel
- T014-T015 (VersionMapper) can run in parallel with T016-T017 (FilePathParser)
- T018-T019 (JUnitParser) can run in parallel with T020-T021 (TestDataService)
- T022-T023 (FileWatcher) can run after parsers are done

**Across User Stories (after Phase 2 complete)**:
- US1, US2, US11, US12, US13, US14, US3, US6, US5, US8, US15, US7 can all be worked on in parallel by different developers
- Each story is independently testable and deliverable

**Within Each User Story**:
- Model creation tasks marked [P] can run in parallel
- Interface definitions can run in parallel with model creation
- Service implementations must come after interfaces
- UI components can be developed in parallel after services are ready

---

## Parallel Example: Morning Triage (User Story 1)

If you have 4 developers available for US1:

**Developer 1**: T028, T029, T030 (models) → T033 (page) → T034 (stats cards)
**Developer 2**: T031, T032 (service) → T039 (filtering logic)
**Developer 3**: T035 (TestHierarchy component) → T036 (integration)
**Developer 4**: T037 (ConfigVisualIndicator) → T038 (integration) → T040 (navigation)

All start simultaneously, integrate at the end.

---

## Summary Statistics

### 🎯 Ultra-Lean MVP Scope

**Total MVP Tasks**: **128 tasks** (43% reduction from original 224)
**Setup**: 6 tasks (Phase 1)
**Foundational**: 25 tasks (Phase 2) - **+4 for memory/validation**
**User Story Implementation**: 90 tasks (Phases 3-10) - **+3 for error handling**
**Essential Polish**: 7 tasks (Phase 11)

**MVP User Story Breakdown**:
- ✅ US1 (Morning Triage - P0): 13 tasks
- ✅ US2 (Release Triage - P0): 13 tasks
- ✅ US11 (Automatic Import - P1): 11 tasks (**+3 error handling**)
- ✅ US12 (Flaky Detection - P1): 14 tasks
- ✅ US13 (Polarion Integration - P1): 9 tasks
- ✅ US14 (Failure Grouping - P1): 11 tasks
- ✅ US3 (Domain View - P1): 10 tasks
- ✅ US10 (Saved Filters - P3): 9 tasks (**LiteDB setup for future features**)

**Deferred Tasks**: **103 tasks** (Phases 12-24)
- ⏸️ US6 (Feature Impact): 9 tasks
- ⏸️ US5 (Config Matrix): 6 tasks
- ⏸️ US4 (Trends): 13 tasks
- ⏸️ US8 (Search): 5 tasks
- ⏸️ US16 (Baselines): 9 tasks
- ⏸️ US15 (Config Diff): 6 tasks
- ⏸️ US17 (Trend Analytics): 4 tasks
- ⏸️ US19 (Heatmap): 10 tasks
- ⏸️ US18 (Execution Time): 8 tasks
- ⏸️ US9 (Custom Dashboard): 11 tasks
- ⏸️ US20 (Permalinks): 11 tasks
- ⏸️ US7 (Feature View): 5 tasks
- ⏸️ Additional Polish: 6 tasks

**Parallel Opportunities**: 
- Setup: 4 of 6 tasks can run in parallel (67%)
- Foundational: 13 of 21 tasks can run in parallel (62%)
- User Stories: All 8 MVP user stories are independently parallelizable after Phase 2

**Estimated MVP Duration**:
- **With 2 developers**: ~8-10 weeks (2-2.5 months)
- **With 3 developers**: ~6-8 weeks (1.5-2 months)
- **With 4+ developers**: ~5-6 weeks (1.25-1.5 months)

**Value Proposition**: Delivers 80% of critical functionality with 57% of implementation effort (**+7 tasks for production robustness: memory management, error handling, validation**)