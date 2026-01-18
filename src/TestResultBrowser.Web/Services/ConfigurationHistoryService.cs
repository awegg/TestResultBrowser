namespace TestResultBrowser.Web.Services;

using TestResultBrowser.Web.Models;

/// <summary>
/// Implementation of configuration history service
/// Builds hierarchical test tree and multi-build history for configuration browsing
/// </summary>
public class ConfigurationHistoryService : IConfigurationHistoryService
{
    private readonly ITestDataService _testDataService;
    private readonly ILogger<ConfigurationHistoryService> _logger;

    public ConfigurationHistoryService(ITestDataService testDataService, ILogger<ConfigurationHistoryService> logger)
    {
        _testDataService = testDataService;
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
                .OrderByDescending(b => ExtractBuildNumber(b))
                .ToList();

            if (!allBuilds.Any())
            {
                _logger.LogWarning("No builds found for configuration {ConfigurationId}", configurationId);
                return Task.FromResult(result);
            }

            // Take last N builds
            var selectedBuilds = allBuilds.Take(numberOfBuilds).OrderBy(b => ExtractBuildNumber(b)).ToList();

            // Build history columns (newest first for display)
            result.HistoryColumns = selectedBuilds
                .OrderByDescending(b => ExtractBuildNumber(b))
                .Select((buildId, index) => new HistoryColumn
                {
                    BuildId = buildId,
                    BuildTime = GetBuildTime(buildId),
                    ColumnIndex = index
                })
                .ToList();

            // Set latest build info
            if (selectedBuilds.Any())
            {
                result.LatestBuildId = selectedBuilds.Last();
                result.LatestBuildTime = GetBuildTime(result.LatestBuildId);
            }

            // Get all test results for this configuration across all selected builds
            var testResults = _testDataService.GetAllTestResults()
                .Where(t => t.ConfigurationId == configurationId && selectedBuilds.Contains(t.BuildId))
                .ToList();

            _logger.LogInformation("Retrieved {TestCount} test results for {ConfigurationId} across {BuildCount} builds", 
                testResults.Count, configurationId, selectedBuilds.Count);

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
            result.HierarchyNodes = BuildHierarchyTree(testResults, selectedBuilds, result.HistoryColumns);

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
                .OrderByDescending(b => ExtractBuildNumber(b))
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

    /// <summary>
    /// Build the hierarchical tree: Domain → Feature → Suite → Test
    /// </summary>
    private List<HierarchyNode> BuildHierarchyTree(
        List<TestResult> testResults,
        List<string> selectedBuilds,
        List<HistoryColumn> historyColumns)
    {
        // Group by feature directory path from filesystem (e.g., "Px Core - Alarm Dashboard")
        var features = testResults
            .GroupBy(t => ExtractFeatureDirectoryName(t.ReportDirectoryPath))
            .OrderBy(g => g.Key)
            .ToList();

        var featureNodes = new List<HierarchyNode>();

        foreach (var featureGroup in features)
        {
            var featureName = featureGroup.Key;
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
                var tests = suiteGroup
                    .GroupBy(t => t.TestFullName)
                    .Select(g => g.First())  // Take first occurrence of each unique test
                    .OrderBy(t => t.TestFullName)
                    .ToList();

                foreach (var test in tests)
                {
                    var testNode = new HierarchyNode
                    {
                        Name = test.MethodName,
                        NodeType = HierarchyNodeType.Test,
                        NodeId = test.Id,
                        TestFullName = test.TestFullName,  // Store for deduplication
                        IndentLevel = 2,
                        IsExpanded = false,
                        ReportDirectoryPath = test.ReportDirectoryPath
                    };

                    // Build history cells for this test
                    testNode.HistoryCells = BuildHistoryCells(test, selectedBuilds, historyColumns, testResults);
                    
                    // Latest stats
                    var latestBuild = selectedBuilds.Last();
                    var latestResult = testResults.FirstOrDefault(r => r.Id == test.Id && r.BuildId == latestBuild);
                    if (latestResult != null)
                    {
                        testNode.LatestStats = new TestNodeStats
                        {
                            Passed = latestResult.Status == TestStatus.Pass ? 1 : 0,
                            Failed = latestResult.Status == TestStatus.Fail ? 1 : 0,
                            Skipped = latestResult.Status == TestStatus.Skip ? 1 : 0
                        };
                    }

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
        List<TestResult> allResults)
    {
        // Build cells using selectedBuilds to maintain consistent order with parent nodes
        var cells = selectedBuilds
            .Select(buildId => 
            {
                var buildResults = allResults
                    .Where(r => r.BuildId == buildId && r.TestFullName == test.TestFullName)
                    .ToList();

                return new HistoryCellData
                {
                    Passed = buildResults.Count(r => r.Status == TestStatus.Pass),
                    Failed = buildResults.Count(r => r.Status == TestStatus.Fail),
                    Skipped = buildResults.Count(r => r.Status == TestStatus.Skip),
                    ReportDirectoryPath = buildResults.FirstOrDefault()?.ReportDirectoryPath
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

        // Latest build stats - deduplicate by TestFullName
        var latestBuild = selectedBuilds.Last();
        var latestTests = allResults
            .Where(r => r.BuildId == latestBuild && allTestFullNames.Contains(r.TestFullName))
            .GroupBy(r => r.TestFullName)
            .Select(g => g.First())  // Take first of each unique test
            .ToList();

        node.LatestStats = new TestNodeStats
        {
            Passed = latestTests.Count(t => t.Status == TestStatus.Pass),
            Failed = latestTests.Count(t => t.Status == TestStatus.Fail),
            Skipped = latestTests.Count(t => t.Status == TestStatus.Skip)
        };

        // History cells - deduplicate by TestFullName per build
        node.HistoryCells = selectedBuilds
            .Select(buildId => 
            {
                var buildTests = allResults
                    .Where(r => r.BuildId == buildId && allTestFullNames.Contains(r.TestFullName))
                    .GroupBy(r => r.TestFullName)
                    .Select(g => g.First())  // Take first of each unique test
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
            .OrderByDescending(r => ExtractBuildNumber(r.BuildId))
            .Select(r => r.ReportDirectoryPath)
            .FirstOrDefault(path => !string.IsNullOrEmpty(path));

        if (!string.IsNullOrEmpty(firstReportPath))
        {
            node.ReportDirectoryPath = firstReportPath;
        }
    }

    /// <summary>
    /// Get all test IDs under a node (recursively includes children)
    /// </summary>
    private HashSet<string> GetAllTestIdsUnderNode(HierarchyNode node)
    {
        var testIds = new HashSet<string>();

        if (node.NodeType == HierarchyNodeType.Test)
        {
            testIds.Add(node.NodeId);
        }

        foreach (var child in node.Children)
        {
            var childIds = GetAllTestIdsUnderNode(child);
            testIds.UnionWith(childIds);
        }

        return testIds;
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
    /// Extract build number from build ID (Release-252 → 252)
    /// </summary>
    private int ExtractBuildNumber(string buildId)
    {
        var parts = buildId.Split('-');
        if (parts.Length > 1 && int.TryParse(parts[^1], out var number))
        {
            return number;
        }
        return 0;
    }

    /// <summary>
    /// Get build timestamp from test data
    /// </summary>
    private DateTime GetBuildTime(string buildId)
    {
        // Get the actual timestamp from the first test result for this build
        var testForBuild = _testDataService.GetAllTestResults()
            .FirstOrDefault(t => t.BuildId == buildId);
        
        return testForBuild?.Timestamp ?? DateTime.UtcNow;
    }

    /// <summary>
    /// Extract feature directory name from the report path
    /// e.g., "C:\data\Release-252_181639\Px Core - Alarm Dashboard" → "Px Core - Alarm Dashboard"
    /// </summary>
    private string ExtractFeatureDirectoryName(string? reportDirectoryPath)
    {
        if (string.IsNullOrEmpty(reportDirectoryPath))
            return "Unknown";

        // Get the last directory component
        var dirInfo = new System.IO.DirectoryInfo(reportDirectoryPath);
        return dirInfo.Name ?? "Unknown";
    }

    /// <summary>
    /// Extract readable feature name from feature ID
    /// </summary>
    private string ExtractFeatureName(string featureId)
    {
        // featureId format: "Domain_NamespacePrefix"
        // Example: "CORE_com.example" → "com.example"
        var parts = featureId.Split('_');
        return parts.Length > 1 ? parts[^1] : featureId;
    }

    /// <summary>
    /// Extract readable suite name from suite ID
    /// </summary>
    private string ExtractSuiteName(string suiteId)
    {
        // suiteId format: "Domain_SuiteName"
        // Example: "CORE_UserService" → "UserService"
        var parts = suiteId.Split('_');
        return parts.Length > 1 ? parts[^1] : suiteId;
    }
}
