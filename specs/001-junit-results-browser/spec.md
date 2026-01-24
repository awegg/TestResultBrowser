# Feature Specification: JUnit Test Results Browser

**Feature Branch**: `001-junit-results-browser`  
**Created**: January 16, 2026  
**Status**: Draft  
**Input**: User description: "Testresultbrowser is a system that should browse Junit test results from different machines and different configurations and runs, seperate them again by team/feature and provide a comparison over time about the test results."

## Clarifications

### Session 2026-01-16

- Q: Data persistence and storage - The spec mentions accessing historical test results but doesn't specify where processed data is stored after import from the shared file system. → A: Test results cached in memory only (parsed from XML files); user-generated data (baselines, comments, saved filters) stored in lightweight persistent storage (e.g., JSON files or SQLite)
- Q: Scope of historical data in memory - With memory-only storage, how much historical test result data should the system keep loaded? → A: All available data from shared file system (unlimited history)
- Q: Authentication and access control - The spec mentions concurrent users but doesn't specify authentication or authorization requirements. → A: No authentication required (open access on internal network)
- Q: Test identity across runs - What happens when a test name changes between runs - should it be treated as a new test or the same test? → A: Treat as new test (no history correlation; broken trend line)
- Q: Import schedule interval - The spec mentions automatic periodic import but doesn't specify the polling interval for checking new files. → A: Every 15 minutes
- Q: Organizational hierarchy - How are tests organized beyond Domain and Feature? → A: Domain → Feature → Test Suite → Test (no Team level; Domains are top-level organizational units)
- Q: Configuration dimensions - Beyond Debug/Release, what other configuration dimensions exist? → A: Version (dev, PXrel114 → 1.14.0, PXrel1441 → 1.14.1) + Named Configs (Default1, Default2, Win2022) + OS/DB sets (limited valid combinations only)
- Q: Are all configuration combinations tested? → A: No, only a few valid combinations are tested (not all Version × NamedConfig × OS/DB permutations)
- Q: How are version codes mapped to readable versions? → A: PXrel{version} pattern: PXrel114 = 1.14.0, PXrel1441 = 1.14.1, etc.; 'dev' = development branch
- Q: Polarion integration details - What level of integration is needed? → A: Basic clickable links to Polarion work items; base URL configured in system settings
- Q: Are flaky test thresholds configurable? → A: Yes, thresholds (percentage and consecutive pass count) should be configurable
- Q: How should configuration information be displayed in Morning Triage? → A: Graphically (matrix/visual indicators) to help users quickly compile information
- Q: Can users manually trigger import refresh? → A: Yes, manual "Refresh Now" option in addition to 15-minute polling
- Q: Primary triage workflows - What are the critical daily/release workflows? → A: Morning Triage (daily new failures review) and Release Triage (during release cycles)
- Q: Team/Domain/Feature metadata source - How is this metadata determined? → A: File system organization and test naming conventions
- Q: Feature impact analysis - Is a feature-centric view needed? → A: Yes, to assess which features are impacted by failures

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Morning Triage of New Failures (Priority: P0)

A team lead arrives in the morning after nightly Pexcite suite execution and needs to quickly identify what broke overnight, which configurations are affected, and which features/domains need attention.

**Why this priority**: This is the #1 critical daily workflow - teams must quickly assess quality status each morning to prioritize fixes and unblock development. Without efficient triage, teams waste hours manually reviewing results.

**Independent Test**: Can be fully tested by loading results from two consecutive nightly runs (yesterday and today) and verifying that the system highlights newly failing tests, groups them by domain/feature, and shows which OS/DB/Version configurations are affected.

**Acceptance Scenarios**:

1. **Given** nightly test results from today and yesterday exist, **When** a team lead opens the Morning Triage view, **Then** the system displays all newly failing tests (passed yesterday, failed today) grouped by domain and feature
2. **Given** newly failing tests are displayed, **When** viewing a failure, **Then** the system shows which configurations (Version/OS/DB/NamedConfig combinations) have the failure graphically (matrix or visual indicators)
3. **Given** multiple domains' results are loaded, **When** filtering by specific domain, **Then** only that domain's features and newly failing tests are shown
4. **Given** failures span multiple features, **When** viewing the triage summary, **Then** impacted features are listed with failure counts per feature
5. **Given** a feature has failures, **When** selecting that feature, **Then** all tests affecting that feature are shown across all configurations
6. **Given** nightly results, **When** viewing the triage dashboard, **Then** the system shows total new failures, total fixed tests (failed yesterday, passed today), and total still-failing tests

---

### User Story 2 - Release Triage During Release Cycles (Priority: P0)

A release manager during a release cycle needs to assess test stability across all configurations to determine if the release candidate is ready or if blockers exist.

**Why this priority**: During release cycles, rapid assessment of cross-configuration stability is critical for go/no-go decisions. Delays in identifying configuration-specific issues can block releases.

**Independent Test**: Can be fully tested by loading test results from a release candidate run across all OS/DB/Version configurations and verifying that the system provides a release readiness dashboard showing pass rates per configuration matrix and identifying configuration-specific failures.

**Acceptance Scenarios**:

1. **Given** release candidate test results across all configurations, **When** viewing Release Triage dashboard, **Then** a configuration matrix shows pass rates for each OS/DB/Version combination
2. **Given** configuration matrix is displayed, **When** a configuration has failures, **Then** it is visually highlighted and shows failure count
3. **Given** configuration-specific failures exist, **When** selecting a failing configuration, **Then** all tests failing only in that configuration are listed
4. **Given** a test fails across multiple configurations, **When** viewing that test, **Then** the system shows which configuration dimensions correlate with failure (e.g., all Linux failures, all Oracle DB failures)
5. **Given** release triage view, **When** comparing to previous release candidate, **Then** system highlights tests that regressed or improved between candidates
6. **Given** multiple domains/features, **When** viewing release readiness, **Then** per-domain and per-feature pass rates are shown to identify problematic areas

---

### User Story 3 - View Test Results by Domain (Priority: P1)

A QA engineer or developer wants to quickly access test results for their specific domain to understand the current quality status of their features without wading through results from other domains.

**Why this priority**: Domains are the primary organizational units, and developers need isolated views of their domain's test results to focus on relevant issues without information overload.

**Independent Test**: Can be fully tested by uploading JUnit test results with domain metadata and verifying that filtering by domain displays only that domain's results and delivers immediate access to domain-specific quality metrics.

**Acceptance Scenarios**:

1. **Given** multiple JUnit XML files from different domains are uploaded, **When** a user selects "Domain: Px Core" filter, **Then** only test results for that domain are displayed
2. **Given** test results are displayed for a domain, **When** the user views the results, **Then** they see hierarchical organization: Domain → Features → Test Suites → Tests with pass/fail counts at each level
3. **Given** no domain filter is selected, **When** a user views the dashboard, **Then** results are organized by domains as primary grouping
4. **Given** multiple domains, **When** viewing the domain list, **Then** each domain shows aggregate pass/fail statistics and can be expanded to see features

---

### User Story 4 - Compare Test Results Over Time (Priority: P1)

A team lead or project manager wants to track how test reliability and coverage are trending across multiple test runs to identify if quality is improving or degrading.

**Why this priority**: Historical comparison is essential for understanding whether development efforts are improving quality or introducing regressions, making this a core requirement.

**Independent Test**: Can be fully tested by uploading multiple JUnit result sets from different dates/runs and verifying that trend graphs and comparison metrics show changes in pass rates, failure patterns, and test counts over time.

**Acceptance Scenarios**:

1. **Given** test results from 5 consecutive runs exist, **When** a user selects "Compare Over Time", **Then** a timeline view shows pass/fail trends with dates and percentages
2. **Given** historical data is available, **When** viewing a specific test case, **Then** the system displays that test's pass/fail history across runs
3. **Given** multiple test runs, **When** comparing two specific runs, **Then** the system highlights new failures, new passes, and consistently failing tests
4. **Given** test runs from the last 30 days, **When** viewing the trend dashboard, **Then** graphs show test reliability percentage over time

---

### User Story 5 - Browse Results by Configuration Matrix (Priority: P1)

A DevOps engineer needs to determine if test failures are configuration-specific by filtering results by Version, OS/DB combination, and named configurations to identify environmental patterns.

**Why this priority**: Pexcite runs on multiple OS/DB combinations (Windows Server 2025/MSSQL 2025, Windows Server 2022/MSSQL Express 2022, etc.) across versions (1.8.6, 1.14.0, 1.14.1, dev) and custom configurations. Configuration-specific failures are critical to identify and this is essential for release readiness.

**Independent Test**: Can be fully tested by uploading JUnit results with Version, OS/DB, and named config metadata, then verifying that filtering by configuration dimensions displays only matching results and identifies configuration-specific patterns.

**Acceptance Scenarios**:

1. **Given** test results across multiple versions (1.8.6, 1.14.0, dev), **When** user filters by "Version: 1.14.0", **Then** only tests run on that version are displayed
2. **Given** tests run on different OS/DB sets (Windows Server 2025/MSSQL 2025, Windows Server 2022/MSSQL Express 2022), **When** user filters by "Windows Server 2025/MSSQL 2025", **Then** only tests run on that OS/DB combination are displayed
3. **Given** a test that fails only on specific configurations, **When** viewing that test's details, **Then** the system highlights which Version/OS/DB combinations had failures and which passed
4. **Given** results from configuration matrix, **When** viewing the summary dashboard, **Then** pass/fail rates are shown in a matrix view (Version × OS/DB × Named Config)
5. **Given** multiple configuration dimensions, **When** user applies multi-dimensional filter (Version: 1.14.0 + Windows Server 2025/MSSQL 2025), **Then** only tests matching all dimensions are displayed

---

### User Story 6 - Feature Impact Analysis (Priority: P1)

A release manager needs to quickly assess which features are impacted by test failures across all configurations to determine feature-level readiness and risk.

**Why this priority**: Understanding feature-level impact is critical for release decisions and prioritizing fixes. A single feature may have tests spread across multiple configurations, requiring aggregated impact view.

**Independent Test**: Can be fully tested by loading test results where a feature has failures across multiple configurations and verifying that the feature impact view shows all affected tests, configurations, and overall feature health status.

**Acceptance Scenarios**:

1. **Given** test failures across multiple features, **When** viewing Feature Impact dashboard, **Then** all features are listed with pass/fail counts and percentage for each feature
2. **Given** a feature with failures, **When** selecting that feature, **Then** all tests related to that feature are shown grouped by test suite
3. **Given** a feature tested across multiple configurations, **When** viewing feature impact, **Then** a configuration matrix shows which Version/NamedConfig/OS-DB combinations have failures for this feature
4. **Given** feature impact view, **When** viewing a failing feature, **Then** system highlights configuration dimensions contributing to the failures
5. **Given** multiple features in a domain, **When** viewing domain-level results, **Then** each feature's health status is visible as a sub-item

---

### User Story 7 - Browse Results by Feature Area (Priority: P2)

A product manager or feature owner wants to view test results organized by feature area to assess the quality of specific product capabilities within domains.

**Why this priority**: Feature-based organization helps stakeholders understand product readiness at a feature level within the domain hierarchy, though it's less critical than morning triage and configuration analysis.

**Independent Test**: Can be fully tested by uploading JUnit results with domain and feature tags and verifying that grouping by feature area shows all tests related to that feature with relevant quality metrics.

**Acceptance Scenarios**:

1. **Given** test results tagged with domains and features (Domain: Px Core → Features: Authentication, Alarm Manager, Reporting), **When** user selects "Alarm Manager" feature, **Then** all tests related to that feature are displayed
2. **Given** a feature with tests across multiple domains or teams, **When** viewing that feature, **Then** all contributions to that feature's tests are visible across organizational boundaries
3. **Given** feature-based view is selected, **When** browsing results, **Then** each feature shows its overall pass rate and critical failure count across all configurations

---

### User Story 8 - Search and Filter Test Cases (Priority: P3)

A developer investigating a specific failure wants to quickly find test cases by name, error message, or failure pattern to understand the scope of an issue.

**Why this priority**: Search capability improves efficiency but is not essential for basic browsing and analysis functionality.

**Independent Test**: Can be fully tested by creating a database of test results and verifying that search queries return accurate matches based on test names, error messages, and status.

**Acceptance Scenarios**:

1. **Given** 1000 test results are loaded, **When** user searches for "authentication", **Then** all tests with "authentication" in their name or package are displayed
2. **Given** multiple failed tests, **When** user searches for a specific error message fragment, **Then** all tests with that error are shown
3. **Given** search results are displayed, **When** user applies additional filters (domain, date range), **Then** results are further refined

---

### User Story 9 - Configure Custom Dashboard (Priority: P2)

A team lead wants to create a personalized dashboard showing trends for their specific machines, configurations, and critical tests to monitor at a glance.

**Why this priority**: Custom dashboards increase efficiency for regular monitoring, especially with ~50 machines to track, making it important but secondary to basic viewing.

**Independent Test**: Can be fully tested by allowing users to select dashboard components (specific machines, configurations, test suites) and verifying the dashboard displays only selected data with relevant trends.

**Acceptance Scenarios**:

1. **Given** a user is on the dashboard configuration page, **When** they select specific machines and tests, **Then** the dashboard displays trends only for those selections
2. **Given** 50 available machines, **When** user applies machine filters, **Then** the system provides efficient selection mechanisms (multi-select, search, groups)
3. **Given** a configured dashboard, **When** user saves the configuration, **Then** it persists across sessions
4. **Given** multiple saved dashboard configurations, **When** user switches between them, **Then** the display updates accordingly

---

### User Story 10 - Save and Reuse Filter Configurations (Priority: P3)

A QA manager regularly checks the same subset of teams and features and wants to save filter settings to avoid reconfiguring them each visit.

**Why this priority**: Saved filters improve convenience but are not essential for core browsing functionality.

**Independent Test**: Can be fully tested by creating and saving a filter configuration, closing the session, then verifying the saved filter can be recalled and correctly filters results.

**Acceptance Scenarios**:

1. **Given** a user has applied multiple filters (domain, feature, date range), **When** they save the filter configuration with a name, **Then** it appears in their saved filters list
2. **Given** saved filter configurations exist, **When** user selects one, **Then** all filters are applied and graphical trends update accordingly
3. **Given** a saved filter, **When** user modifies and re-saves it, **Then** the configuration is updated

---

### User Story 11 - Automatic Test Result Import (Priority: P1)

A DevOps engineer wants test results from all machines automatically imported from the shared file system without manual intervention.

**Why this priority**: Automatic import is essential for the system to function in a continuous integration environment, making this a core requirement.

**Independent Test**: Can be fully tested by placing new JUnit XML files on the shared file system and verifying they are automatically detected, imported, and displayed within the configured import interval.

**Acceptance Scenarios**:

1. **Given** the import scheduler is running, **When** new JUnit XML files are added to the shared file system, **Then** they are automatically imported within the configured interval
2. **Given** imported files, **When** viewing test results, **Then** the latest imports are immediately available for browsing
3. **Given** the shared file system contains approximately 50 machines uploading results, **When** the import runs, **Then** all new files are processed without errors
4. **Given** malformed or duplicate files, **When** the import runs, **Then** the system logs errors but continues processing valid files

---

### User Story 12 - Flaky Test Detection & Management (Priority: P1)

A QA engineer or team lead needs to identify tests that fail inconsistently across runs to separate real failures from noise during morning triage.

**Why this priority**: Flaky tests waste significant triage time and hide real issues. Automatically detecting and filtering them saves 20-30% of daily triage effort and improves focus on genuine regressions.

**Independent Test**: Can be fully tested by loading test results where specific tests have mixed pass/fail outcomes across consecutive runs and verifying that the system calculates flakiness scores and flags inconsistent tests.

**Acceptance Scenarios**:

1. **Given** a test that passed 3 times and failed 2 times in the last 5 runs, **When** viewing that test, **Then** it is flagged with a flaky indicator and shows flakiness score (e.g., "40% unstable")
2. **Given** flaky tests are identified, **When** viewing Morning Triage, **Then** a "Flaky Tests" section lists candidates with their instability patterns
3. **Given** the Flaky Tests view, **When** user reviews the list, **Then** each test shows its pass/fail history across recent runs with visual timeline
4. **Given** morning triage with flaky tests present, **When** user enables "Hide Known Flaky Tests" filter, **Then** tests with flakiness score >30% are hidden from new failures list
5. **Given** a test's flakiness pattern changes (becomes stable), **When** the test passes consistently for 10 runs, **Then** the flaky flag is automatically removed

---

### User Story 13 - Polarion Integration (Priority: P1)

A developer investigating a test failure needs immediate context about the Polarion ticket referenced in the test name to understand if it's a known issue.

**Why this priority**: Test names contain Polarion ticket IDs (e.g., PEXC-28044). Auto-linking eliminates manual lookup and context switching, saving minutes per failure during triage when investigating dozens of failures.

**Independent Test**: Can be fully tested by loading test results with Polarion ticket references in test names and verifying that ticket IDs are detected, made clickable, and link to the correct Polarion work items.

**Acceptance Scenarios**:

1. **Given** a test name contains "PEXC-28044 Download Alarm Configuration Report", **When** viewing the test details, **Then** "PEXC-28044" is rendered as a clickable link to Polarion
2. **Given** multiple Polarion ticket references in a test name, **When** viewing the test, **Then** all ticket IDs are individually linked
3. **Given** a linked Polarion ticket, **When** hovering over the link, **Then** a tooltip shows ticket status (Open/In Progress/Resolved) and title (if Polarion API available)
4. **Given** morning triage view with multiple failures, **When** viewing failures with Polarion tickets, **Then** ticket status is displayed inline (e.g., "PEXC-28044 [Resolved]")
5. **Given** Polarion integration is configured, **When** clicking a ticket link, **Then** Polarion opens in a new browser tab to the work item

---

### User Story 14 - Failure Grouping by Root Cause (Priority: P1)

A team lead during morning triage needs to see failures grouped by similar error patterns to identify root causes rather than individual test noise.

**Why this priority**: A single bug often causes 10+ test failures. Grouping by error similarity helps focus on root causes, not symptoms. Fixing one issue resolves many failures, making triage far more efficient.

**Independent Test**: Can be fully tested by loading test results where multiple tests fail with similar error messages and verifying that the system clusters them into groups and displays group summaries.

**Acceptance Scenarios**:

1. **Given** 10 tests fail with error "Connection timeout to AlarmService", **When** viewing Morning Triage, **Then** these are grouped under "Connection timeout to AlarmService (10 tests)"
2. **Given** failure groups are displayed, **When** viewing group summary, **Then** it shows error pattern, affected test count, and affected features/domains
3. **Given** a failure group, **When** user clicks the group, **Then** all tests in that group are listed with individual details
4. **Given** multiple error patterns exist, **When** viewing Morning Triage, **Then** groups are sorted by number of affected tests (largest groups first)
5. **Given** failure grouping, **When** user views group details, **Then** the common stack trace segment is highlighted and unique portions per test are shown

---

### User Story 15 - Configuration Diff View (Priority: P2)

A DevOps engineer needs to compare the same build across different configurations to identify configuration-specific failures.

**Why this priority**: Essential for debugging environment issues. Quickly seeing "Test passes on dev_E2E_Default1 but fails on PXrel114_Win2022_Default1" pinpoints config-specific problems.

**Independent Test**: Can be fully tested by loading the same Release build from two different top-level configurations and verifying side-by-side comparison highlights differences.

**Acceptance Scenarios**:

1. **Given** Release-252 exists in both dev_E2E_Default1_Core and PXrel114_Win2022_Default1_Core, **When** user selects "Compare Configurations", **Then** a side-by-side view shows both configurations for the same build
2. **Given** configuration comparison view, **When** viewing results, **Then** tests that pass in one config but fail in the other are highlighted
3. **Given** configuration-specific failures, **When** viewing the diff, **Then** a summary shows "8 tests ONLY fail on PXrel114_Win2022_Default1_Core"
4. **Given** multiple features tested, **When** comparing configs, **Then** user can drill down to specific features to see config differences at feature level
5. **Given** configuration comparison, **When** a test has different results, **Then** both error messages/stack traces are shown side-by-side for analysis

---

### User Story 16 - Smart Baseline Comparison (Priority: P2)

A release manager needs to compare current build quality against a known stable baseline instead of just the previous run to assess release readiness.

**Why this priority**: During release cycles, comparing to "last known good" is more valuable than "previous run" which might also be broken. Enables "how far from stable" assessment.

**Independent Test**: Can be fully tested by marking a specific build as baseline and verifying that comparison views can use baseline instead of previous run, showing delta accurately.

**Acceptance Scenarios**:

1. **Given** viewing a build's details, **When** user clicks "Mark as Baseline", **Then** that build is saved as a baseline reference with a label (e.g., "Last Stable Release")
2. **Given** a baseline exists (e.g., Release-250), **When** viewing Morning Triage for Release-252, **Then** user can toggle between "Compare to Previous Run" and "Compare to Baseline"
3. **Given** baseline comparison mode, **When** viewing new failures, **Then** system shows "3 new failures since Release-250 baseline" instead of since previous run
4. **Given** Release Triage view, **When** baseline is set, **Then** system auto-suggests baseline comparison with clear indicator of baseline build
5. **Given** multiple baselines exist per domain/version, **When** user selects comparison baseline, **Then** dropdown shows available baselines with dates and labels

---

### User Story 17 - Build Quality Trend Analytics (Priority: P2)

A project manager needs to see quality trends over time per domain to identify if quality is improving or degrading before release.

**Why this priority**: Proactive quality management. Catching degrading trends early prevents release delays. Visual trends make quality status clear to stakeholders.

**Independent Test**: Can be fully tested by loading 30 builds of historical data and verifying that trend graphs show pass rate changes over time with domain-level granularity.

**Acceptance Scenarios**:

1. **Given** 30 builds of historical data, **When** viewing Quality Trends dashboard, **Then** a graph shows pass rate percentage over builds for each domain
2. **Given** quality trend graph, **When** viewing domain trends, **Then** each domain line shows trend direction indicator (Improving ↑, Stable →, Degrading ↓)
3. **Given** domain quality trends, **When** a domain drops below 95% pass rate threshold, **Then** it is highlighted in red with alert indicator
4. **Given** trend dashboard, **When** user hovers over a data point, **Then** tooltip shows build number, date, pass rate, and number of failures
5. **Given** quality trends, **When** viewing a degrading domain, **Then** system shows metric: "Core domain: Degrading ↓ (97% → 92% over last 10 builds)"

---

### User Story 18 - Test Execution Time Regression Detection (Priority: P3)

A performance-focused team lead needs to identify tests that are slowing down over time to catch performance regressions early.

**Why this priority**: Slow tests indicate performance issues and keep test suite execution time creeping up. Early detection prevents hour-long test runs.

**Independent Test**: Can be fully tested by loading test results where specific tests show increasing execution times across builds and verifying alerts for significant slowdowns.

**Acceptance Scenarios**:

1. **Given** a test's execution time across 10 builds, **When** viewing test details, **Then** a small sparkline graph shows execution time trend
2. **Given** a test that now takes 45s but averaged 10s in previous builds, **When** viewing that test, **Then** it is flagged with "⚠️ Execution time increased 350%"
3. **Given** execution time tracking, **When** viewing a "Slow Tests" report, **Then** tests with 50%+ slowdown compared to 10-build average are listed
4. **Given** slow test detection, **When** viewing Morning Triage, **Then** newly slow tests are highlighted with performance regression indicator
5. **Given** test execution time trends, **When** viewing feature-level summary, **Then** aggregate execution time changes are shown (e.g., "Alarm Manager suite: +15s slower than baseline")

---

### User Story 19 - Failure History Heatmap (Priority: P2)

A QA manager needs a visual overview of which features/domains are chronically unstable across recent builds to prioritize stabilization efforts.

**Why this priority**: Visual pattern recognition helps identify problematic areas at a glance. "Always red" features need architecture review, not just bug fixes.

**Independent Test**: Can be fully tested by loading results for multiple features across 10 builds and verifying heatmap grid displays color-coded stability patterns.

**Acceptance Scenarios**:

1. **Given** test results for 20 features across last 10 builds, **When** viewing Failure History Heatmap, **Then** a grid shows Features (rows) × Builds (columns)
2. **Given** heatmap grid, **When** viewing cells, **Then** cell color indicates health: Green (all tests passed), Yellow (1-5 failures), Red (6+ failures), Gray (no data)
3. **Given** heatmap visualization, **When** user clicks a cell, **Then** detailed view shows specific failures for that feature in that build
4. **Given** heatmap over time, **When** viewing the grid, **Then** chronically unstable features (multiple red cells) are visually obvious
5. **Given** heatmap filtering, **When** user selects specific domains, **Then** heatmap updates to show only features within selected domains

---

### User Story 20 - Permalinks & Collaboration (Priority: P3)

A team lead needs to share specific triage views with colleagues via URL to discuss findings without requiring manual filter recreation.

**Why this priority**: Improves collaboration during distributed triage. Share exact view state with team members or stakeholders for discussion and follow-up.

**Independent Test**: Can be fully tested by applying specific filters, generating permalink, opening in new session, and verifying all filters and view state are preserved.

**Acceptance Scenarios**:

1. **Given** a Morning Triage view with specific filters applied (Domain: Core, Version: dev, failures only), **When** user clicks "Share Link", **Then** a permalink URL is generated preserving all filters and view state
2. **Given** a permalink URL, **When** another user opens it, **Then** the exact same view loads with all filters applied as originally configured
3. **Given** viewing test results, **When** user clicks "Copy Results", **Then** selected test results are copied to clipboard formatted for Slack/email (markdown or plain text)
4. **Given** a specific test failure, **When** user adds a comment/note, **Then** the note is persistently stored and visible to all users viewing that test in that build
5. **Given** annotated failures, **When** viewing Morning Triage, **Then** tests with notes show indicator (e.g., 💬) and note preview on hover

---

### Edge Cases

- What happens when JUnit XML files are malformed or incomplete?
- How does the system handle test results with missing feature/domain metadata from file paths?
- What happens when test runs have duplicate or conflicting timestamps?
- How does the system display tests that have no historical data (first run)?
- How does the system handle very large JUnit XML files (thousands of tests)?
- What happens when machine or configuration metadata is absent from test results?
- What happens when the shared file system is unavailable during scheduled import?
- What happens when multiple files match the same domain/feature pattern?
- How does the system handle tests that transition between pass/fail/skip states across runs?
- What happens when a user saves a filter configuration that references teams or features that no longer exist?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST ingest JUnit XML test result files from multiple sources
- **FR-002**: System MUST extract test case name, status (pass/fail/skip), execution time, error messages, and stack traces from JUnit XML
- **FR-038**: System MUST identify tests uniquely by their full test name (class name + method name); renamed tests are treated as new tests with no historical correlation
- **FR-040**: System MUST associate test results with domain identifiers via file system organization (directory structure)
- **FR-004**: System MUST associate test results with feature areas via file system organization (feature folder names)
- **FR-041**: System MUST organize tests hierarchically as: Domain → Feature → Test Suite → Test
- **FR-085**: System MUST map version codes to human-readable versions (PXrel114 → 1.14.0, PXrel1441 → 1.14.1, dev → Development)
- **FR-005**: System MUST associate test results with machine identifiers (hostname)
- **FR-042**: System MUST associate test results with Version identifier (e.g., 1.8.6, 1.14.0, 1.14.1, dev)
- **FR-043**: System MUST associate test results with OS/DB combination (e.g., Windows Server 2025/MSSQL 2025, Windows Server 2022/MSSQL Express 2022)
- **FR-044**: System MUST associate test results with named configuration identifiers (customer-specific configs)
- **FR-006**: System MUST associate test results with run metadata (timestamp, build number, run identifier)
- **FR-045**: System MUST allow filtering test results by domain
- **FR-008**: System MUST allow filtering test results by feature area
- **FR-046**: System MUST allow filtering test results by Version
- **FR-047**: System MUST allow filtering test results by OS/DB combination
- **FR-048**: System MUST allow filtering test results by named configuration
- **FR-009**: System MUST allow filtering test results by machine
- **FR-049**: System MUST support multi-dimensional filtering (e.g., Version + OS/DB + Named Config simultaneously)
- **FR-010**: System MUST allow filtering test results by date range or specific run
- **FR-011**: System MUST display test pass/fail/skip counts and percentages
- **FR-012**: System MUST display individual test case details including error messages and stack traces for failures
- **FR-013**: System MUST provide comparison view showing test result changes between two selected runs
- **FR-014**: System MUST provide timeline/trend view showing test pass rates over multiple runs
- **FR-015**: System MUST display test execution time metrics (average, min, max per test)
- **FR-016**: System MUST identify tests that consistently fail across multiple runs
- **FR-017**: System MUST identify newly failing tests (passed in previous run, failed in current run)
- **FR-018**: System MUST identify newly passing tests (failed in previous run, passed in current run)
- **FR-050**: System MUST provide Morning Triage view showing newly failing tests grouped by domain/feature with configuration details displayed graphically
- **FR-051**: System MUST provide Release Triage view with configuration matrix showing pass rates for valid Version × NamedConfig × OS/DB combinations only (not all permutations)
- **FR-052**: System MUST provide Feature Impact view showing all tests affecting a specific feature across all configurations
- **FR-053**: System MUST display configuration-specific failure patterns graphically (e.g., tests failing only on specific configurations)
- **FR-054**: System MUST aggregate and display pass/fail statistics at each hierarchy level (Domain, Feature, Suite, Test)
- **FR-055**: System MUST calculate flakiness score for tests based on pass/fail pattern over recent runs (e.g., passed 3/5 = 40% unstable)
- **FR-056**: System MUST flag tests as flaky when instability exceeds configurable threshold (default: >30% failure rate over last 10 runs with mixed results)
- **FR-057**: System MUST provide Flaky Tests view listing all identified flaky tests with instability metrics
- **FR-058**: System MUST allow filtering to hide flaky tests from Morning Triage and other views
- **FR-059**: System MUST automatically remove flaky flag when test becomes stable based on configurable consecutive pass count (default: 10 consecutive passes)
- **FR-086**: System MUST allow configuration of flaky test detection thresholds (percentage threshold and consecutive pass count)
- **FR-060**: System MUST detect Polarion ticket references in test names (pattern: PEXC-\d+)
- **FR-061**: System MUST render detected Polarion ticket IDs as clickable hyperlinks using configured Polarion base URL
- **FR-087**: System MUST allow configuration of Polarion base URL for work item links
- **FR-063**: System MUST cluster test failures by similar error messages and stack traces into failure groups
- **FR-064**: System MUST display failure group summaries showing error pattern, affected test count, and affected features
- **FR-065**: System MUST allow users to drill into failure groups to see individual test details
- **FR-066**: System MUST provide Configuration Diff View comparing same build across different top-level configurations
- **FR-067**: System MUST highlight tests with different results between configurations in diff view
- **FR-068**: System MUST identify and display configuration-specific failures (tests failing in only one configuration)
- **FR-069**: System MUST allow users to mark specific builds as baselines with labels (e.g., "Last Stable Release")
- **FR-070**: System MUST provide baseline comparison mode as alternative to previous-run comparison
- **FR-071**: System MUST calculate and display delta metrics from baseline (new failures since baseline, fixed since baseline)
- **FR-072**: System MUST generate quality trend graphs showing pass rate over time per domain
- **FR-073**: System MUST calculate and display trend direction indicators (Improving ↑, Stable →, Degrading ↓) for each domain
- **FR-074**: System MUST alert when domain quality drops below configurable threshold (e.g., <95% pass rate)
- **FR-075**: System MUST track test execution time across builds and calculate average execution time
- **FR-076**: System MUST detect and flag tests with significant execution time increases (e.g., >50% slower than average)
- **FR-077**: System MUST provide Slow Tests report showing performance regression candidates
- **FR-078**: System MUST generate Failure History Heatmap showing features × builds grid with color-coded health status
- **FR-079**: System MUST color-code heatmap cells based on failure count (Green: 0, Yellow: 1-5, Red: 6+, Gray: no data)
- **FR-080**: System MUST generate shareable permalink URLs preserving all filters and view state
- **FR-081**: System MUST restore view state from permalink URL when accessed
- **FR-082**: System MUST allow users to copy test results to clipboard in shareable format (markdown/plain text)
- **FR-083**: System MUST allow users to add persistent comments/notes to specific test failures
- **FR-084**: System MUST display note indicators and previews on tests with annotations
- **FR-019**: System MUST support searching test cases by name
- **FR-020**: System MUST handle multiple concurrent users browsing results
- **FR-037**: System MUST NOT require user authentication or login (open access on internal network)
- **FR-021**: System MUST access historical test result data from external file system (data retention managed by DevOps team, not within system scope)
- **FR-034**: System MUST cache all processed test results in memory only without persistent database storage
- **FR-035**: System MUST re-parse JUnit XML files from shared file system on application restart to rebuild in-memory cache
- **FR-036**: System MUST load all available historical test result data from shared file system into memory (no time-based retention limit)
- **FR-022**: System MUST provide a high-level overview dashboard showing the latest test run with comparison to previous run (highlighting changes)
- **FR-023**: System MUST allow users to save filter configurations (domain, feature, configuration, date range combinations)
- **FR-024**: System MUST recall and apply saved filter configurations
- **FR-025**: System MUST update graphical trends dynamically when filters are applied
- **FR-026**: System MUST provide a configurable dashboard where users can select and arrange views for specific configurations, machines, and tests
- **FR-027**: System MUST handle filtering and selection across approximately 50 different machines and branches efficiently
- **FR-028**: System MUST display skipped tests as a distinct category separate from failed tests
- **FR-029**: System MUST include skipped test counts in summary statistics
- **FR-030**: System MUST automatically import JUnit XML files from a shared network file system on a periodic schedule
- **FR-039**: System MUST poll shared file system for new JUnit XML files every 15 minutes
- **FR-031**: System MUST monitor the shared file system for new JUnit XML files
- **FR-088**: System MUST provide manual "Refresh Now" capability to immediately check for and import new test results

### Key Entities

- **Test Result**: Represents a single test case execution containing test name, status (pass/fail/skip - with skip as distinct category), execution time, timestamp, error message, stack trace, and associated metadata (domain, feature, version, OS/DB, named config)
- **Test Run**: Represents a complete test execution session (typically nightly Pexcite suite execution) containing collection of test results, run timestamp, run identifier (Release-{BuildNumber}_{Timestamp}), and aggregated metadata
- **Domain**: Represents a major product area (Core, T&T, PM, Prod, Feature) with domain identifier, name, and associated features; top-level organizational unit
- **Feature Area**: Represents a product feature or component within a domain with feature identifier, name, and associated test suites
- **Test Suite**: Represents a logical grouping of related test cases within a feature (extracted from JUnit XML testsuite element)
- **Machine**: Represents physical or virtual test execution machine with hostname identifier
- **Version**: Represents Pexcite product version with code mapping (PXrel114 = 1.14.0, PXrel1441 = 1.14.1, dev = Development)
- **OS/DB Combination**: Represents operating system and database pairing encoded in NamedConfig (e.g., Win2022 implies Windows Server 2022)
- **Named Configuration**: Represents specific test configuration identifier (Default1, Default2, Win2022, etc.)
- **Configuration Matrix**: Represents limited set of valid Version × NamedConfig × OS/DB combinations actually tested (not all permutations)
- **Filter Configuration**: Represents a saved set of filter criteria including domain, feature, version, named config, machine, and date range selections; persistently stored
- **Dashboard Configuration**: Represents a custom dashboard layout including selected views (Morning Triage, Release Triage, Feature Impact, etc.) and filter presets; persistently stored
- **Flaky Test**: Represents a test with inconsistent pass/fail results over recent runs, including flakiness score (percentage of instability), pass/fail history timeline, and auto-removal criteria (thresholds are configurable)
- **Failure Group**: Represents a cluster of test failures with similar error messages/stack traces, including error pattern, affected test count, list of grouped tests, and affected features/domains
- **Baseline Build**: Represents a specific test run marked as reference point for comparison (e.g., "Last Stable Release"), including build identifier, label, date marked, and associated metrics; persistently stored
- **Polarion Ticket Reference**: Represents extracted Polarion work item ID from test name (e.g., PEXC-28044) with hyperlink generated using configured Polarion base URL
- **Quality Trend**: Represents historical pass rate data for a domain over multiple builds, including trend direction (Improving/Stable/Degrading), current pass rate, and threshold alerts
- **Execution Time Metric**: Represents test execution duration tracking across builds, including average time, current time, percentage change, and regression flag
- **Heatmap Cell**: Represents feature × build intersection in Failure History Heatmap, including health status (Pass/Partial/Fail/NoData), failure count, and color coding

### UI/UX Design Decisions

The following design decisions from the UI mock define the user experience and visual organization:

**Hierarchical Test Organization**:
- Tests are organized in a 4-level hierarchy: Domain → Feature Area → Test Suite → Individual Test Case
- Domain is the top-level organizational unit (Core, T&T, PM, Prod, Feature)
- Each level is collapsible/expandable to manage information density
- Visual indentation indicates hierarchy depth
- Pass/fail counts are aggregated and displayed at each level (Domain, Feature, Suite, Test)
- Users can expand all or collapse all levels for quick overview or detailed investigation

**Multi-Run Side-by-Side Comparison**:
- Test results are displayed in a table with multiple run columns (e.g., Latest Run, Previous Run, Run-2)
- Each run column shows pass/fail counts for each level of hierarchy
- Visual change indicators (↑ improved, ↓ degraded) highlight differences between consecutive runs
- Users can quickly scan across runs to identify stability trends and recent regressions
- Summary statistics at the top aggregate results across the entire view

**Multi-Dimensional Configuration Filtering**:
- Configuration filtering supports three independent dimensions:
  - **Version**: Dropdown or multi-select (dev, 1.14.0 [PXrel114], 1.14.1 [PXrel1441]) with human-readable labels
  - **Named Configuration**: Dropdown or multi-select (Default1, Default2, Win2022, etc.)
  - **OS/DB Combination**: Derived from NamedConfig or shown when applicable (e.g., Win2022 = Windows Server 2022)
- Configuration Matrix View displays pass rates for ONLY valid tested combinations (not all permutations)
- Configuration-specific failures are visually highlighted in matrix cells with color coding
- Multi-dimensional filters can be combined (e.g., Version: 1.14.0 + NamedConfig: Win2022)

**Triage-Focused Views**:
- **Morning Triage View**:
  - Dedicated landing page for daily new failure review
  - Automatically highlights newly failing tests (passed yesterday, failed today)
  - Groups failures by Domain → Feature for quick domain assignment
  - Shows configuration details GRAPHICALLY for each failure (mini-matrix or visual indicators showing which Version/NamedConfig combinations are affected)
  - Displays metrics: Total new failures, Total fixed (failed yesterday, passed today), Total still-failing
  - Graphical configuration display helps users quickly compile information without reading lists
- **Release Triage View**:
  - Configuration matrix prominently displayed
  - Pass rates shown per configuration combination
  - Failing configurations highlighted with failure counts
  - Per-domain and per-feature pass rates for release readiness assessment
  - Comparison to previous release candidate
- **Feature Impact View**:
  - Feature-centric organization showing all tests affecting selected feature
  - Configuration matrix filtered to show feature-specific results across all configs
  - Domain contributions to feature health highlighted
  - Quick assessment of "Is Feature X ready for release?"

**Navigation Structure**:
- Sidebar navigation provides access to:
  - **Morning Triage**: Daily new failure review (default landing page)
  - **Release Triage**: Configuration matrix and release readiness
  - **Feature Impact**: Feature-centric analysis
  - **Flaky Tests**: List of identified flaky tests with instability metrics
  - **Slow Tests**: Performance regression detection and execution time trends
  - **Quality Trends**: Domain-level quality trends over time with alerts
  - **Failure Heatmap**: Visual feature × build stability overview
  - **Configuration Diff**: Side-by-side configuration comparison
  - **Home**: Main hierarchical test results browser
  - **Import**: Monitor and configure automatic import from shared file system
  - **Changelog**: View history of test result imports and system changes
  - **Configuration**: Manage file system organization parsing rules, baseline builds, and import settings
  - **Dashboard**: Custom dashboard configuration for personalized monitoring
- Active view is visually highlighted in sidebar

**Filtering and Search**:
- Filter controls in toolbar for Domain, Features, Version, NamedConfig, and Machines
- **"Hide Flaky Tests"** toggle in toolbar to filter out tests with flakiness score above configured threshold (default >30%)
- **Baseline selector** dropdown to choose comparison baseline instead of previous run
- Domain/Feature filter uses hierarchical modal dialog for multi-select
- Version, NamedConfig filters use multi-select dropdowns with human-readable labels
- Machine filter uses modal dialog with search/grouping for 50+ machines
- Active filters displayed as removable chips below toolbar
- Real-time search box for finding specific test names
- Save/load filter configurations for repeated use
- **Manual "Refresh Now" button** to immediately check for new test result imports (in addition to 15-minute auto-polling)

**Visual Status Indicators**:
- Color coding: Green (pass), Red (fail), Yellow/Orange (skip)
- Percentage-based pass rates in summary bar and at each hierarchy level
- Badge counts on filter buttons showing active selections
- Change indicators between runs with directional symbols (↑↓)
- Configuration matrix cells color-coded by pass rate (green = 100%, red = <90%, yellow = 90-99%)
- **Flaky test indicators**: 🔀 icon with flakiness score percentage (e.g., "🔀 40% unstable")
- **Polarion ticket links**: Clickable PEXC-XXXXX references (configured base URL)
- **Failure group badges**: Count indicator showing "15 tests" grouped under error pattern
- **Performance regression alerts**: ⚠️ icon for tests with significant execution time increases
- **Trend direction arrows**: ↑ Improving, → Stable, ↓ Degrading for domain quality trends
- **Heatmap color coding**: Green (all pass), Yellow (1-5 failures), Red (6+ failures), Gray (no data)
- **Note indicators**: 💬 icon showing tests with user comments/annotations
- **Baseline markers**: ⭐ icon indicating builds marked as baselines
- **Configuration indicators**: Graphical mini-matrix or icon set showing affected configurations at a glance

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can filter 10,000+ test results by domain and see filtered results in under 2 seconds
- **SC-002**: Users can identify newly failing tests within 10 seconds of viewing Morning Triage view
- **SC-015**: Domain leads can complete morning triage workflow (identify new failures, note affected features, note configurations) in under 5 minutes for their domain
- **SC-016**: Release managers can assess release readiness across all configurations in under 2 minutes using Release Triage view
- **SC-017**: Users can identify configuration-specific failures (e.g., "only fails on Windows Server 2025/MSSQL 2025") within 30 seconds
- **SC-018**: Feature Impact view displays all tests and configurations affecting a selected feature in under 3 seconds
- **SC-019**: Configuration matrix (Version × OS/DB × Named Config) displays pass rates for all combinations in under 5 seconds
- **SC-020**: System correctly associates tests with Domain/Feature based on file system organization with 99%+ accuracy
- **SC-003**: Historical trend graphs display 30 days of test result data with clear pass/fail rate visualization
- **SC-004**: Users can successfully locate a specific failed test case within 30 seconds using search and filters
- **SC-005**: System correctly parses and displays test results from standard JUnit XML format with 99%+ accuracy
- **SC-006**: Domain leads can review quality status for their domains covering the last 10 runs in under 1 minute
- **SC-021**: Users can drill down through 4-level hierarchy (Domain → Feature → Suite → Test) with each level expanding in under 1 second
- **SC-007**: 90% of users can complete morning triage and identify regressions without training
- **SC-008**: System handles concurrent uploads of test results from at least 20 different machines without data corruption
- **SC-009**: Users can configure and save a custom dashboard in under 2 minutes
- **SC-010**: Dashboard displays high-level overview with changes from last run within 3 seconds of loading
- **SC-011**: Saved filter configurations are recalled and applied in under 1 second
- **SC-012**: Automatic import processes new files from shared file system within 15-minute polling interval
- **SC-013**: Users can distinguish between failed and skipped tests at a glance in all views
- **SC-014**: Users can efficiently select specific machines from a list of 50+ options using filtering or grouping in under 30 seconds
- **SC-022**: Users can apply multi-dimensional filters (Version + OS/DB + Named Config) and see results in under 3 seconds
- **SC-023**: System handles nightly import of entire Pexcite suite results (estimated 10,000+ tests across 50+ configurations) within 15-minute polling interval
- **SC-024**: System identifies flaky tests with 95%+ accuracy based on historical pass/fail patterns
- **SC-025**: Users can filter out flaky tests from Morning Triage, reducing triage time by 20-30%
- **SC-026**: Polarion ticket IDs in test names are automatically detected and linked with 99%+ accuracy
- **SC-027**: Clicking Polarion link opens correct work item in under 2 seconds
- **SC-028**: Failure grouping clusters 80%+ of similar failures correctly, reducing perceived failure count by 50%+
- **SC-029**: Users can identify root cause from failure groups within 30 seconds instead of reviewing individual test failures
- **SC-030**: Configuration Diff View highlights configuration-specific failures for same build in under 5 seconds
- **SC-031**: Users can identify "only fails on Config X" tests within 1 minute using diff view
- **SC-032**: Baseline comparison mode allows release managers to assess "distance from stable" in under 2 minutes
- **SC-033**: Quality trend graphs display 30 builds of historical data with trend indicators in under 3 seconds
- **SC-034**: Degrading quality alerts (domains below threshold) are visually obvious within 5 seconds of viewing trends
- **SC-035**: Slow test detection identifies tests with 50%+ execution time regression with 90%+ accuracy
- **SC-036**: Performance regression alerts help teams keep average test suite execution time stable over 30 builds
- **SC-037**: Failure History Heatmap renders 20 features × 10 builds grid in under 3 seconds
- **SC-038**: Chronically unstable features (3+ red cells in heatmap) are identifiable at a glance
- **SC-039**: Generated permalink URLs preserve 100% of filter and view state
- **SC-040**: Shared permalink loads identical view for all users within 3 seconds

## Assumptions

- JUnit XML files follow standard JUnit/Surefire XML schema conventions
- Entire Pexcite test suite runs nightly across all domains and features
- Test results are uploaded to a shared network file system by test execution infrastructure after each nightly run
- DevOps team manages the shared file system, including data retention, backups, and access permissions
- System has read access to the shared file system location

**Data Storage & Persistence:**
- Test results are cached in memory only (parsed from JUnit XML files on shared file system)
- User-generated data (baseline markers, comments/notes, saved filter configurations) stored in lightweight persistent storage (JSON files or SQLite database)
- System assumes sufficient RAM for dataset size (estimated: 50 configs × 60 builds × 10,000 tests ≈ 30M test results ≈ 10-20GB RAM)
- On system restart, test results are re-parsed from XML files; user data is loaded from persistent storage

**File System Organization (based on sample_data analysis):**
- Top-level directory structure: `{Version}_{TestType}_{NamedConfig}_{Domain}/`
  - **Version**: e.g., `dev`, `PXrel114` (maps to 1.14.0), `PXrel1441` (maps to 1.14.1)
  - **TestType**: e.g., `E2E` (End-to-End tests)
  - **NamedConfig**: e.g., `Default1`, `Default2`, `Win2022` (OS/DB info encoded in name when applicable)
  - **Domain**: e.g., `Core`, `ProductionMonitoring`, `TnT_Prod` (Track & Trace Production)
- Second level: `Release-{BuildNumber}_{Timestamp}/` (e.g., `Release-252_181639/`)
  - BuildNumber increments with each build
  - Timestamp provides unique run identifier
- Third level: Feature folders with naming pattern `Px {Domain} - {FeatureName}/`
  - Examples: `Px Core - Alarm Manager`, `Px T&T - Aggregation_Process`, `Px PM OEE Calculation`
  - Domain prefix in folder name (e.g., `Px Core`, `Px T&T`, `Px PM`, `Px Feature`, `Px Prod`)
  - Feature name follows the domain prefix
- Fourth level: Multiple JUnit XML files per feature (e.g., `tests-{hash}.xml`)
- Test Suite names extracted from XML `<testsuite name="">` attribute (e.g., "Regression Tests for Alarm Reports")
- Individual test names include Polarion ticket references (e.g., "PEXC-28044 Download Alarm Configuration Report...")

**Metadata Extraction Rules:**
- **Version**: Extracted from top-level directory name (first segment before underscore)
  - Version mapping: `PXrel{code}` → semantic version (e.g., `PXrel114` = 1.14.0, `PXrel1441` = 1.14.1)
  - `dev` = Development branch
- **TestType**: Extracted from top-level directory name (second segment)
- **NamedConfig**: Extracted from top-level directory name (third segment)
- **Domain**: Extracted from top-level directory name (fourth segment) AND validated against feature folder prefix
- **Feature**: Extracted from third-level directory name after "Px {Domain} - " prefix
- **Run Identifier**: Extracted from second-level directory name (Release-{BuildNumber}_{Timestamp})
- **OS/DB Combination**: Encoded in NamedConfig when applicable (e.g., `Win2022` implies Windows Server 2022)
- **Test Suite**: From XML `<testsuite name="">` attribute
- **Test Case**: From XML `<testcase name="">` attribute
- **Polarion Ticket Links**: Test names containing `PEXC-XXXXX` pattern automatically linkified using configured Polarion base URL

**Configuration Scope:**
- Only limited valid configuration combinations (Version × NamedConfig × OS/DB × Domain) are tested (NOT all permutations)
- Valid combinations determined by what appears in shared file system
- Configuration dropdown filters only show existing (tested) combinations
- Example: `dev_E2E_Default1_Core` and `PXrel114_Win2022_Default1_Core` exist, but not every {Version} × {NamedConfig} × {Domain} permutation

**System Constraints:**
- Test results cached in memory to optimize browsing performance (no database queries for test data)
- User-generated data (baselines, comments, filters) persisted in JSON/SQLite for cross-session retention
- Application state (filters, expanded nodes) managed in browser session state
- No authentication/authorization system at this stage (future enhancement)

**Data Volume & Scale:**
- Approximately 50+ configuration combinations based on Version × NamedConfig × Domain permutations
- Multiple Release builds per day (build numbers increment frequently; sample shows ~60 builds in dataset)
- Each feature folder contains 1-10 XML files (split test suites)
- Each domain contains 10-30 features based on sample data
- Historical data spans months of nightly builds (sample shows builds from Release-2 to Release-252)

**Known Domains (from sample_data):**
- **Core**: Core Pexcite functionality (Alarm Manager, Dashboard, Reporting, etc.)
- **T&T** (Track & Trace): Serialization and tracking features
- **PM** (Production Monitoring): OEE and Energy Monitoring
- **Prod**: Production-specific tests (PD - Product Data, SC - Shop Control)
- **Feature**: Cross-cutting features (Identity & Access Management, Audit Trail, etc.)

- Users have basic familiarity with software testing concepts (understand terms like pass/fail, skip, regression, flaky tests)
- Users understand the Domain → Feature organizational structure used in Pexcite development
- The system will be accessed via web browser interface
- Network connectivity to shared file system is generally reliable with occasional outages acceptable
- System operates on internal trusted network with no authentication required; network-level access controls provide security boundary
- All users have equal access to view all test results across all domains and features
- System uses in-memory caching for all processed test results with no persistent database; historical data is re-loaded from files on restart
- System loads all available historical data from shared file system with no retention limit
- Available server memory is sufficient to hold entire historical test result dataset in memory; memory capacity planning must account for unlimited growth as new test runs accumulate
- DevOps team manages file system data retention, which indirectly controls the system's memory footprint
