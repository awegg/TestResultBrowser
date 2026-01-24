using TestResultBrowser.Web.Common;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of triage service
/// </summary>
public class TriageService : ITriageService
{
    private readonly ITestDataService _testDataService;
    private readonly ILogger<TriageService> _logger;

    public TriageService(ITestDataService testDataService, ILogger<TriageService> logger)
    {
        _testDataService = testDataService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MorningTriageResult?> GetMorningTriageAsync(List<string>? selectedDomains = null)
    {
        // Get the two most recent builds
        var allResults = _testDataService.GetAllTestResults();
        var builds = allResults
            .Select(r => r.BuildId)
            .Distinct()
            .OrderByDescending(b => BuildNumberExtractor.ExtractBuildNumber(b))
            .Take(2)
            .ToList();

        if (builds.Count < 2)
        {
            _logger.LogWarning("Not enough builds for triage comparison. Found {Count} builds.", builds.Count);
            return null;
        }

        return await GetMorningTriageAsync(builds[0], builds[1], selectedDomains);
    }

    /// <inheritdoc/>
    public async Task<MorningTriageResult?> GetMorningTriageAsync(
        string todayBuildId, 
        string yesterdayBuildId, 
        List<string>? selectedDomains = null)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation("Starting morning triage: {Today} vs {Yesterday}", todayBuildId, yesterdayBuildId);

            // Get test results for both builds
            var todayTests = _testDataService.GetTestResultsByBuild(todayBuildId).ToList();
            var yesterdayTests = _testDataService.GetTestResultsByBuild(yesterdayBuildId).ToList();

            // Apply domain filter if specified
            if (selectedDomains != null && selectedDomains.Any())
            {
                todayTests = todayTests.Where(t => selectedDomains.Contains(t.DomainId)).ToList();
                yesterdayTests = yesterdayTests.Where(t => selectedDomains.Contains(t.DomainId)).ToList();
            }

            if (!todayTests.Any() || !yesterdayTests.Any())
            {
                _logger.LogWarning("No test results found for comparison");
                return null;
            }

            // Create lookup dictionaries
            var todayByTestName = todayTests
                .GroupBy(t => t.TestFullName)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            var yesterdayByTestName = yesterdayTests
                .GroupBy(t => t.TestFullName)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Find new failures (passed yesterday, failed today)
            var newFailures = new List<TriageNewFailure>();
            foreach (var testName in todayByTestName.Keys)
            {
                if (!yesterdayByTestName.ContainsKey(testName))
                    continue;

                var todayResults = todayByTestName[testName];
                var yesterdayResults = yesterdayByTestName[testName];

                // Check if any configuration failed today that passed yesterday
                foreach (var todayResult in todayResults.Where(t => t.Status == TestStatus.Fail))
                {
                    var yesterdayResult = yesterdayResults.FirstOrDefault(y => 
                        y.ConfigurationId == todayResult.ConfigurationId);

                    if (yesterdayResult != null && yesterdayResult.Status == TestStatus.Pass)
                    {
                        // Found a new failure
                        var existing = newFailures.FirstOrDefault(f => f.TestFullName == testName &&
                                                                     f.DomainId == todayResult.DomainId &&
                                                                     f.FeatureId == todayResult.FeatureId);
                        if (existing != null)
                        {
                            // Add config to existing entry
                            existing.AffectedConfigs.Add(todayResult.ConfigurationId);
                        }
                        else
                        {
                            newFailures.Add(new TriageNewFailure
                            {
                                TestFullName = testName,
                                DomainId = todayResult.DomainId,
                                FeatureId = todayResult.FeatureId,
                                AffectedConfigs = new List<string> { todayResult.ConfigurationId },
                                ErrorMessage = todayResult.ErrorMessage ?? "No error message",
                                StackTrace = todayResult.StackTrace,
                                FailedOn = todayResult.Timestamp
                            });
                        }
                    }
                }
            }

            // Find fixed tests (failed yesterday, passed today)
            var fixedTests = new List<TriageFixedTest>();
            foreach (var testName in todayByTestName.Keys)
            {
                if (!yesterdayByTestName.ContainsKey(testName))
                    continue;

                var todayResults = todayByTestName[testName];
                var yesterdayResults = yesterdayByTestName[testName];

                // Check if any configuration passed today that failed yesterday
                foreach (var todayResult in todayResults.Where(t => t.Status == TestStatus.Pass))
                {
                    var yesterdayResult = yesterdayResults.FirstOrDefault(y => 
                        y.ConfigurationId == todayResult.ConfigurationId);

                    if (yesterdayResult != null && yesterdayResult.Status == TestStatus.Fail)
                    {
                        // Found a fixed test
                        var existing = fixedTests.FirstOrDefault(f => f.TestFullName == testName &&
                                                                     f.DomainId == todayResult.DomainId &&
                                                                     f.FeatureId == todayResult.FeatureId);
                        if (existing != null)
                        {
                            existing.FixedInConfigs.Add(todayResult.ConfigurationId);
                        }
                        else
                        {
                            fixedTests.Add(new TriageFixedTest
                            {
                                TestFullName = testName,
                                DomainId = todayResult.DomainId,
                                FeatureId = todayResult.FeatureId,
                                FixedInConfigs = new List<string> { todayResult.ConfigurationId },
                                PassedOn = todayResult.Timestamp
                            });
                        }
                    }
                }
            }

            // Find still failing tests (failed yesterday, failed today)
            var stillFailing = new List<TestResult>();
            foreach (var testName in todayByTestName.Keys)
            {
                if (!yesterdayByTestName.ContainsKey(testName))
                    continue;

                var todayResults = todayByTestName[testName];
                var yesterdayResults = yesterdayByTestName[testName];

                foreach (var todayResult in todayResults.Where(t => t.Status == TestStatus.Fail))
                {
                    var yesterdayResult = yesterdayResults.FirstOrDefault(y => 
                        y.ConfigurationId == todayResult.ConfigurationId);

                    if (yesterdayResult != null && yesterdayResult.Status == TestStatus.Fail)
                    {
                        stillFailing.Add(todayResult);
                    }
                }
            }

            // Calculate pass rates
            var todayPassed = todayTests.Count(t => t.Status == TestStatus.Pass);
            var todayTotal = todayTests.Count(t => t.Status != TestStatus.Skip);
            var todayPassRate = todayTotal > 0 ? (double)todayPassed / todayTotal * 100 : 0;

            var yesterdayPassed = yesterdayTests.Count(t => t.Status == TestStatus.Pass);
            var yesterdayTotal = yesterdayTests.Count(t => t.Status != TestStatus.Skip);
            var yesterdayPassRate = yesterdayTotal > 0 ? (double)yesterdayPassed / yesterdayTotal * 100 : 0;

            _logger.LogInformation(
                "Triage complete: {NewFailures} new failures, {FixedTests} fixed, {StillFailing} still failing",
                newFailures.Count, fixedTests.Count, stillFailing.Count);

            return new MorningTriageResult
            {
                TodayBuildId = todayBuildId,
                YesterdayBuildId = yesterdayBuildId,
                NewFailures = newFailures,
                FixedTests = fixedTests,
                StillFailing = stillFailing,
                TodayPassRate = todayPassRate,
                YesterdayPassRate = yesterdayPassRate,
                TotalTestsToday = todayTotal,
                TotalTestsYesterday = yesterdayTotal
            };
        }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<ReleaseTriageResult?> GetReleaseTriageAsync(string releaseBuildId, string? previousReleaseBuildId = null)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation("Starting release triage for build {ReleaseBuildId}", releaseBuildId);

            var currentResults = _testDataService.GetTestResultsByBuild(releaseBuildId).ToList();
            if (!currentResults.Any())
            {
                _logger.LogWarning("No test results found for build {ReleaseBuildId}", releaseBuildId);
                return null;
            }

            // Build configuration matrix (Version × NamedConfig)
            // Aggregate all results by (Version, NamedConfig) since multiple ConfigurationIds can map to the same cell
            var versions = new HashSet<string>();
            var namedConfigs = new HashSet<string>();
            var cellsByVersionAndConfig = new Dictionary<(string Version, string NamedConfig), (int Total, int Passed, int Failed, List<string> ConfigIds)>();
            var configIdsByVersionAndConfig = new Dictionary<(string Version, string NamedConfig), HashSet<string>>();

            foreach (var config in currentResults.Select(r => r.ConfigurationId).Distinct())
            {
                var parts = config.Split('_');
                if (parts.Length < 4) continue; // {Version}_{TestType}_{NamedConfig}_{Domain}
                var version = parts[0];
                var namedConfig = parts[2];

                versions.Add(version);
                namedConfigs.Add(namedConfig);

                if (!configIdsByVersionAndConfig.ContainsKey((version, namedConfig)))
                    configIdsByVersionAndConfig[(version, namedConfig)] = new HashSet<string>();
                configIdsByVersionAndConfig[(version, namedConfig)].Add(config);
            }

            // Aggregate test results by (Version, NamedConfig)
            foreach (var kvp in configIdsByVersionAndConfig)
            {
                var (version, namedConfig) = kvp.Key;
                var configIds = kvp.Value;
                
                var resultsForCell = currentResults.Where(r => configIds.Contains(r.ConfigurationId)).ToList();
                var total = resultsForCell.Count(r => r.Status != Models.TestStatus.Skip);
                var passed = resultsForCell.Count(r => r.Status == Models.TestStatus.Pass);
                var failed = resultsForCell.Count(r => r.Status == Models.TestStatus.Fail);
                var passRate = total > 0 ? (double)passed / total * 100.0 : 0.0;

                cellsByVersionAndConfig[(version, namedConfig)] = (total, passed, failed, configIds.ToList());
            }

            // Build cells dictionary for matrix display
            var cells = new Dictionary<string, Dictionary<string, Models.ConfigCell>>();
            foreach (var kvp in cellsByVersionAndConfig)
            {
                var (version, namedConfig) = kvp.Key;
                var (total, passed, failed, configIds) = kvp.Value;
                var passRate = total > 0 ? (double)passed / total * 100.0 : 0.0;

                if (!cells.ContainsKey(version))
                    cells[version] = new Dictionary<string, Models.ConfigCell>();

                // Use first configuration ID as representative (all in this cell share Version+NamedConfig)
                cells[version][namedConfig] = new Models.ConfigCell
                {
                    ConfigurationId = string.Join(", ", configIds.OrderBy(c => c)),
                    TotalTests = total,
                    PassedTests = passed,
                    FailedTests = failed,
                    PassRate = passRate
                };
            }

            // Identify failing configurations (any failed tests in the cell)
            var failingConfigs = cellsByVersionAndConfig
                .Where(kvp => kvp.Value.Failed > 0)
                .SelectMany(kvp => kvp.Value.ConfigIds)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Domain and feature pass rates
            var domainPassRates = currentResults
                .GroupBy(r => r.DomainId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var total = g.Count(r => r.Status != Models.TestStatus.Skip);
                        var passed = g.Count(r => r.Status == Models.TestStatus.Pass);
                        return total > 0 ? (double)passed / total * 100.0 : 0.0;
                    });

            var featurePassRates = currentResults
                .GroupBy(r => r.FeatureId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var total = g.Count(r => r.Status != Models.TestStatus.Skip);
                        var passed = g.Count(r => r.Status == Models.TestStatus.Pass);
                        return total > 0 ? (double)passed / total * 100.0 : 0.0;
                    });

            // Comparison to previous release candidate (optional)
            Models.ComparisonMetrics? comparison = null;
            if (!string.IsNullOrWhiteSpace(previousReleaseBuildId))
            {
                var previousResults = _testDataService.GetTestResultsByBuild(previousReleaseBuildId!).ToList();
                if (previousResults.Any())
                {
                    var prevByTest = previousResults.GroupBy(r => r.TestFullName).ToDictionary(g => g.Key, g => g.ToList());
                    var currByTest = currentResults.GroupBy(r => r.TestFullName).ToDictionary(g => g.Key, g => g.ToList());

                    int regressed = 0;
                    int improved = 0;

                    foreach (var kvp in currByTest)
                    {
                        var testName = kvp.Key;
                        var currList = kvp.Value;
                        if (!prevByTest.TryGetValue(testName, out var prevList)) continue;

                        // Compare per configuration where both builds have entries
                        foreach (var curr in currList)
                        {
                            var prev = prevList.FirstOrDefault(p => p.ConfigurationId == curr.ConfigurationId);
                            if (prev == null) continue;

                            if (prev.Status == Models.TestStatus.Pass && curr.Status == Models.TestStatus.Fail) regressed++;
                            if (prev.Status == Models.TestStatus.Fail && curr.Status == Models.TestStatus.Pass) improved++;
                        }
                    }

                    var currTotal = currentResults.Count(r => r.Status != Models.TestStatus.Skip);
                    var currPassed = currentResults.Count(r => r.Status == Models.TestStatus.Pass);
                    var currRate = currTotal > 0 ? (double)currPassed / currTotal * 100.0 : 0.0;

                    var prevTotal = previousResults.Count(r => r.Status != Models.TestStatus.Skip);
                    var prevPassed = previousResults.Count(r => r.Status == Models.TestStatus.Pass);
                    var prevRate = prevTotal > 0 ? (double)prevPassed / prevTotal * 100.0 : 0.0;

                    comparison = new Models.ComparisonMetrics
                    {
                        TestsRegressed = regressed,
                        TestsImproved = improved,
                        PassRateChange = currRate - prevRate
                    };
                }
            }

            return new Models.ReleaseTriageResult
            {
                ReleaseBuildId = releaseBuildId,
                PreviousReleaseBuildId = previousReleaseBuildId,
                Matrix = new Models.ConfigurationMatrix
                {
                    Versions = versions.OrderBy(v => v).ToList(),
                    NamedConfigs = namedConfigs.OrderBy(n => n).ToList(),
                    Cells = cells
                },
                FailingConfigurations = failingConfigs,
                DomainPassRates = domainPassRates,
                FeaturePassRates = featurePassRates,
                ComparisonToPrevious = comparison
            };
        }).ConfigureAwait(false);
    }
}
