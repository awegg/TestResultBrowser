using TestResultBrowser.Web.Common;

namespace TestResultBrowser.Web.Services;

using TestResultBrowser.Web.Models;

/// <summary>
/// Implementation of configuration history service
/// Builds hierarchical test tree and multi-build history for configuration browsing
/// </summary>
public class ConfigurationHistoryService : IConfigurationHistoryService
{
    private readonly ITestDataService _testDataService;
    private readonly IWorkItemLinkService _workItemLinkService;
    private readonly ILogger<ConfigurationHistoryService> _logger;

    public ConfigurationHistoryService(ITestDataService testDataService, IWorkItemLinkService workItemLinkService, ILogger<ConfigurationHistoryService> logger)
    {
        _testDataService = testDataService;
        _workItemLinkService = workItemLinkService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<ConfigurationHistoryResult> GetConfigurationHistoryAsync(string configurationId, int numberOfBuilds = 5)
    {
        try
        {
            _logger.LogInformation("Building configuration history for {ConfigurationId}, last {NumberOfBuilds} builds", configurationId, numberOfBuilds);

            var result = new ConfigurationHistoryResult
            {
                ConfigurationId = configurationId
            };

            // Get all builds sorted descending (latest first)
            var allBuilds = _testDataService.GetAllBuildIds()
                .OrderByDescending(b => BuildNumberExtractor.ExtractBuildNumber(b))
                .ToList();

            if (!allBuilds.Any())
            {
                _logger.LogWarning("No builds found for configuration {ConfigurationId}", configurationId);
                return Task.FromResult(result);
            }

            // Clamp numberOfBuilds to at least 1
            var buildCount = Math.Max(1, numberOfBuilds);

            // Take last N builds (already sorted descending)
            var selectedBuildsList = allBuilds.Take(buildCount).ToList();
            var selectedBuilds = selectedBuildsList.ToHashSet();

            // Build history columns (selectedBuildsList is already sorted newest first for display)
            result.HistoryColumns = selectedBuildsList
                .Select((buildId, index) => new HistoryColumn
                {
                    BuildId = buildId,
                    BuildTime = GetBuildTime(buildId),
                    ColumnIndex = index
                })
                .ToList();

            // Set latest build info
            if (selectedBuildsList.Any())
            {
                result.LatestBuildId = selectedBuildsList.First();
                result.LatestBuildTime = GetBuildTime(result.LatestBuildId);
            }

            // Get all test results for this configuration across all selected builds
            // USE INDEXED QUERY - critical for performance with 250k+ tests
            var testResults = _testDataService.GetTestResultsByConfiguration(configurationId)
                .Where(t => selectedBuilds.Contains(t.BuildId))
                .ToList();

            _logger.LogInformation("Retrieved {TestCount} test results for {ConfigurationId} across {BuildCount} builds",
                testResults.Count, configurationId, selectedBuildsList.Count);

            if (!testResults.Any())
            {
                _logger.LogWarning("No test results found for configuration {ConfigurationId}", configurationId);
                return Task.FromResult(result);
            }

            // Calculate latest build stats
            var latestBuildTests = testResults.Where(t => t.BuildId == result.LatestBuildId).ToList();
            result.TotalTests = latestBuildTests.Count;
            result.PassedTests = latestBuildTests.Count(t => t.Status == TestStatus.Pass);
            result.FailedTests = latestBuildTests.Count(t => t.Status == TestStatus.Fail);
            result.SkippedTests = latestBuildTests.Count(t => t.Status == TestStatus.Skip);

            // Build hierarchical tree
            result.HierarchyNodes = BuildHierarchyTree(testResults, selectedBuildsList, result.HistoryColumns);

            _logger.LogInformation("Configuration history built successfully: {DomainCount} domains", result.HierarchyNodes.Count);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building configuration history for {ConfigurationId}", configurationId);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<List<string>> GetAvailableConfigurationsAsync()
    {
        try
        {
            var configs = _testDataService.GetAllConfigurationIds()
                .OrderBy(c => c)
                .ToList();

            _logger.LogInformation("Found {ConfigCount} available configurations", configs.Count);
            return Task.FromResult(configs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available configurations");
            return Task.FromResult(new List<string>());
        }
    }

    /// <inheritdoc/>
    public Task<List<string>> GetAvailableBuildsAsync()
    {
        try
        {
            var builds = _testDataService.GetAllBuildIds()
                .OrderByDescending(b => BuildNumberExtractor.ExtractBuildNumber(b))
                .ToList();

            _logger.LogInformation("Found {BuildCount} available builds", builds.Count);
            return Task.FromResult(builds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available builds");
            return Task.FromResult(new List<string>());
        }
    }

    /// <inheritdoc/>
    public Task<List<ConfigurationMetadata>> GetConfigurationsWithMetadataAsync()
    {
        try
        {
            var testResults = _testDataService.GetAllTestResults();

            // Group by configuration and find latest timestamp for each
            var configMetadata = testResults
                .GroupBy(t => t.ConfigurationId)
                .Select(g => new ConfigurationMetadata
                {
                    Id = g.Key,
                    LastUpdateTime = g.Max(t => t.Timestamp)
                })
                .OrderByDescending(c => c.LastUpdateTime)
                .ToList();

            _logger.LogInformation("Found {ConfigCount} configurations with metadata", configMetadata.Count);
            return Task.FromResult(configMetadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving configurations with metadata");
            return Task.FromResult(new List<ConfigurationMetadata>());
        }
    }

    /// <summary>
    /// Build the hierarchical tree: Domain → Feature → Suite → Test
    /// </summary>
    private List<HierarchyNode> BuildHierarchyTree(
        List<TestResult> testResults,
        List<string> selectedBuilds,
        List<HistoryColumn> historyColumns)
    {
        // Pre-index test results by (TestFullName, BuildId) for O(1) lookup
        // This eliminates O(N²) complexity in BuildHistoryCells
        var testResultsIndex = testResults
            .GroupBy(r => (r.TestFullName, r.BuildId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.Timestamp).ToList());

        // Group by Feature property from TestResult (extracted during import)
        var features = testResults
            .GroupBy(t => t.Feature)
            .OrderBy(g => g.Key)
            .ToList();

        var featureNodes = new List<HierarchyNode>();
        foreach (var featureGroup in features)
        {
            var featureName = string.IsNullOrWhiteSpace(featureGroup.Key) ? "Unknown" : featureGroup.Key;
            var featureNode = new HierarchyNode
            {
                Name = featureName,
                NodeType = HierarchyNodeType.Feature,
                NodeId = featureName,
                IndentLevel = 0,
                IsExpanded = featureGroup.Any(t => t.Status == TestStatus.Fail)
            };

            // Test Suites under feature
            var suites = featureGroup
                .GroupBy(t => t.TestSuiteId)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var suiteGroup in suites)
            {
                var suiteName = ExtractSuiteName(suiteGroup.Key);
                var suiteNode = new HierarchyNode
                {
                    Name = suiteName,
                    NodeType = HierarchyNodeType.Suite,
                    NodeId = suiteGroup.Key,
                    IndentLevel = 1,
                    IsExpanded = false
                };

                // Individual tests under suite - deduplicate by TestFullName
                // Take the LATEST result from the LATEST build (by timestamp)
                // Note: selectedBuilds is sorted descending, so First() is the latest
                var latestBuild = selectedBuilds.First();
                var tests = suiteGroup
                    .GroupBy(t => t.TestFullName)
                    .Select(g =>
                    {
                        // Get all results from the latest build for this test
                        var latestBuildResults = g.Where(t => t.BuildId == latestBuild).ToList();

                        // If test exists in latest build, take the LATEST result by timestamp
                        if (latestBuildResults.Any())
                        {
                            return latestBuildResults.OrderByDescending(t => t.Timestamp).First();
                        }

                        // Fallback: if test doesn't exist in latest build, take most recent from any build
                        return g.OrderByDescending(t => BuildNumberExtractor.ExtractBuildNumber(t.BuildId))
                                .ThenByDescending(t => t.Timestamp)
                                .First();
                    })
                    .OrderBy(t => t.TestFullName)
                    .ToList();

                foreach (var test in tests)
                {
                    // DEBUG: Log to verify we selected the correct test
                    _logger.LogDebug("Selected test '{TestName}' from build {BuildId} (timestamp: {Timestamp}) with status {Status}. ErrorMessage length: {ErrorLength}",
                        test.TestFullName, test.BuildId, test.Timestamp, test.Status, test.ErrorMessage?.Length ?? 0);

                    var testNode = new HierarchyNode
                    {
                        Name = test.MethodName,
                        NodeType = HierarchyNodeType.Test,
                        NodeId = test.Id,
                        TestFullName = test.TestFullName,  // Store for deduplication
                        IndentLevel = 2,
                        IsExpanded = false,
                        ReportDirectoryPath = test.ReportDirectoryPath,
                        WorkItemReferences = _workItemLinkService.GetTicketReferences(test.WorkItemIds),
                        // Error fields will be populated from the latest build's result (see below)
                        ErrorMessage = null,
                        StackTrace = null
                    };

                    // Build history cells for this test
                    testNode.HistoryCells = BuildHistoryCells(test, selectedBuilds, historyColumns, testResultsIndex);

                    // Extract error information from the latest build's HistoryCellData
                    // This ensures error messages match the result that determines the cell color
                    if (testNode.HistoryCells.Any())
                    {
                        // First = latest build (selectedBuilds is sorted descending, so HistoryCells[0] is latest)
                        var latestBuildCell = testNode.HistoryCells.First();
                        if (latestBuildCell.SourceTestResult != null)
                        {
                            testNode.ErrorMessage = latestBuildCell.SourceTestResult.ErrorMessage;
                            testNode.StackTrace = latestBuildCell.SourceTestResult.StackTrace;
                            testNode.FirstErrorMessage = latestBuildCell.SourceTestResult.ErrorMessage;
                        }
                    }

                    // Latest stats - use the test object which is already from the latest build
                    testNode.LatestStats = new TestNodeStats
                    {
                        Passed = test.Status == TestStatus.Pass ? 1 : 0,
                        Failed = test.Status == TestStatus.Fail ? 1 : 0,
                        Skipped = test.Status == TestStatus.Skip ? 1 : 0
                    };

                    suiteNode.Children.Add(testNode);
                }

                // Calculate suite stats
                CalculateNodeStats(suiteNode, selectedBuilds, testResults);
                featureNode.Children.Add(suiteNode);
            }

            // Calculate feature stats
            CalculateNodeStats(featureNode, selectedBuilds, testResults);
            featureNodes.Add(featureNode);
        }

        // Wrap all features in a synthetic root node so we get a single table
        var rootNode = new HierarchyNode
        {
            Name = "Root",
            NodeType = HierarchyNodeType.Domain,
            NodeId = "root",
            IndentLevel = -1,
            IsExpanded = true,
            Children = featureNodes
        };

        // Calculate root node stats (including HistoryCells)
        CalculateNodeStats(rootNode, selectedBuilds, testResults);

        return new List<HierarchyNode> { rootNode };
    }

    /// <summary>
    /// Build history cells for a node across all history columns
    /// </summary>
    private List<HistoryCellData> BuildHistoryCells(
        TestResult test,
        List<string> selectedBuilds,
        List<HistoryColumn> historyColumns,
        Dictionary<(string TestFullName, string BuildId), List<TestResult>> testResultsIndex)
    {
        // Build cells using selectedBuilds to maintain consistent order with parent nodes
        var cells = selectedBuilds
            .Select(buildId =>
            {
                // O(1) lookup using pre-built index instead of O(N) linear search
                var buildResults = testResultsIndex.TryGetValue((test.TestFullName, buildId), out var results)
                    ? results
                    : new List<TestResult>();

                // Results are already sorted by timestamp descending in the index
                var latestResult = buildResults.FirstOrDefault();

                // Count based on the latest result only
                var passed = latestResult?.Status == TestStatus.Pass ? 1 : 0;
                var failed = latestResult?.Status == TestStatus.Fail ? 1 : 0;
                var skipped = latestResult?.Status == TestStatus.Skip ? 1 : 0;

                // Log if there were multiple results (indicates retries/reruns)
                if (buildResults.Count > 1)
                {
                    _logger.LogDebug("Test '{TestName}' had {Count} runs in {BuildId}. Using latest: {LatestStatus}. All statuses: {Statuses}",
                        test.TestFullName, buildResults.Count, buildId, latestResult?.Status,
                        string.Join(",", buildResults.OrderBy(r => r.Timestamp).Select(r => r.Status)));
                }

                return new HistoryCellData
                {
                    Passed = passed,
                    Failed = failed,
                    Skipped = skipped,
                    ReportDirectoryPath = latestResult?.ReportDirectoryPath,
                    SourceTestResult = latestResult  // Store the actual result used for this cell
                };
            })
            .ToList();

        return cells;
    }

    /// <summary>
    /// Calculate aggregated stats for a parent node (Suite/Feature/Domain)
    /// </summary>
    private void CalculateNodeStats(
        HierarchyNode node,
        List<string> selectedBuilds,
        List<TestResult> allResults)
    {
        // Get all unique test full names under this node (handles deduplication)
        var allTestFullNames = GetAllTestFullNamesUnderNode(node);

        // Latest build stats - deduplicate by TestFullName, selecting latest by timestamp
        // Note: selectedBuilds is sorted descending, so First() is the latest
        var latestBuild = selectedBuilds.First();
        var latestTests = allResults
            .Where(r => r.BuildId == latestBuild && allTestFullNames.Contains(r.TestFullName))
            .GroupBy(r => r.TestFullName)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())  // Take latest by timestamp
            .ToList();

        node.LatestStats = new TestNodeStats
        {
            Passed = latestTests.Count(t => t.Status == TestStatus.Pass),
            Failed = latestTests.Count(t => t.Status == TestStatus.Fail),
            Skipped = latestTests.Count(t => t.Status == TestStatus.Skip)
        };

        // History cells - deduplicate by TestFullName per build, selecting latest by timestamp
        node.HistoryCells = selectedBuilds
            .Select(buildId =>
            {
                var buildTests = allResults
                    .Where(r => r.BuildId == buildId && allTestFullNames.Contains(r.TestFullName))
                    .GroupBy(r => r.TestFullName)
                    .Select(g => g.OrderByDescending(r => r.Timestamp).First())  // Take latest by timestamp
                    .ToList();

                return new HistoryCellData
                {
                    Passed = buildTests.Count(t => t.Status == TestStatus.Pass),
                    Failed = buildTests.Count(t => t.Status == TestStatus.Fail),
                    Skipped = buildTests.Count(t => t.Status == TestStatus.Skip),
                    ReportDirectoryPath = null  // Parent nodes don't have a single report path
                };
            })
            .ToList();

        // Totals across all builds - count unique test names
        node.TotalTestsAcrossAllBuilds = allTestFullNames.Count;

        node.TotalFailuresAcrossAllBuilds = allResults
            .Where(r => selectedBuilds.Contains(r.BuildId) && allTestFullNames.Contains(r.TestFullName) && r.Status == TestStatus.Fail)
            .GroupBy(r => new { r.TestFullName, r.BuildId })
            .Count();  // Count unique test+build combinations with failures

        // Use the first available report path from descendants so parent rows can link to report
        var firstReportPath = allResults
            .Where(r => selectedBuilds.Contains(r.BuildId) && allTestFullNames.Contains(r.TestFullName))
            .OrderByDescending(r => BuildNumberExtractor.ExtractBuildNumber(r.BuildId))
            .Select(r => r.ReportDirectoryPath)
            .FirstOrDefault(path => !string.IsNullOrEmpty(path));

        if (!string.IsNullOrEmpty(firstReportPath))
        {
            node.ReportDirectoryPath = firstReportPath;
        }

        // For parent nodes, capture the first error message from any failed child test
        if (node.NodeType != HierarchyNodeType.Test)
        {
            var firstFailedTest = allResults
                .Where(r => selectedBuilds.Contains(r.BuildId)
                    && allTestFullNames.Contains(r.TestFullName)
                    && r.Status == TestStatus.Fail
                    && !string.IsNullOrEmpty(r.ErrorMessage))
                .OrderByDescending(r => BuildNumberExtractor.ExtractBuildNumber(r.BuildId))
                .ThenByDescending(r => r.Timestamp)  // For same build, get latest timestamp
                .FirstOrDefault();

            if (firstFailedTest != null)
            {
                node.FirstErrorMessage = firstFailedTest.ErrorMessage;
            }
        }
    }

    /// <summary>
    /// Get all unique test full names under a node (recursively includes children)
    /// This is used for proper deduplication when calculating parent stats
    /// </summary>
    private HashSet<string> GetAllTestFullNamesUnderNode(HierarchyNode node)
    {
        var testFullNames = new HashSet<string>();

        if (node.NodeType == HierarchyNodeType.Test && !string.IsNullOrEmpty(node.TestFullName))
        {
            testFullNames.Add(node.TestFullName);
        }

        foreach (var child in node.Children)
        {
            var childNames = GetAllTestFullNamesUnderNode(child);
            testFullNames.UnionWith(childNames);
        }

        return testFullNames;
    }

    /// <summary>
    /// Get build timestamp from test data using optimized service method
    /// </summary>
    private DateTime GetBuildTime(string buildId)
    {
        // Use dedicated method to avoid fetching all results
        return _testDataService.GetBuildTimestamp(buildId) ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Extract readable suite name from suite ID
    /// </summary>
    private string ExtractSuiteName(string suiteId)
    {
        // suiteId format: "Domain_SuiteName"
        // Example: "CORE_UserService" → "UserService"
        if (string.IsNullOrWhiteSpace(suiteId))
            return "Unknown";
        
        var parts = suiteId.Split('_');
        return parts.Length > 1 ? parts[^1] : suiteId;
    }
}
