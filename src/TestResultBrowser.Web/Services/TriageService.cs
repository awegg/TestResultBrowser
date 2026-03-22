using TestResultBrowser.Web.Common;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of triage service
/// </summary>
public class TriageService : ITriageService
{
    private readonly ITestDataService _testDataService;
    private readonly IFailureClassificationService _failureClassificationService;
    private readonly ILogger<TriageService> _logger;

    public TriageService(
        ITestDataService testDataService,
        IFailureClassificationService failureClassificationService,
        ILogger<TriageService> logger)
    {
        _testDataService = testDataService;
        _failureClassificationService = failureClassificationService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<MorningTriageResult?> GetMorningTriageAsync(List<string>? selectedDomains = null)
    {
        // Use the build ID index — avoids materializing all test results just to find the two latest builds
        var builds = _testDataService.GetAllBuildIds()
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
        return await GetMorningTriageAsync(todayBuildId, new List<string> { yesterdayBuildId }, selectedDomains);
    }

    /// <inheritdoc/>
    public async Task<MorningTriageResult?> GetMorningTriageAsync(
        string todayBuildId,
        List<string> baselineBuildIds,
        List<string>? selectedDomains = null)
    {
        return await Task.Run(() =>
        {
            var baselines = baselineBuildIds?
                .Where(buildId => !string.IsNullOrWhiteSpace(buildId))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (baselines.Count == 0)
            {
                _logger.LogWarning("No baseline builds supplied for triage comparison.");
                return null;
            }

            var primaryBaseline = baselines[0];

            _logger.LogInformation("Starting morning triage: {Today} vs {Baselines}", todayBuildId, string.Join(", ", baselines));

            // Get test results for both builds
            var todayTests = _testDataService.GetTestResultsByBuild(todayBuildId).ToList();
            var baselineTests = baselines
                .Select(buildId => (BuildId: buildId, Tests: _testDataService.GetTestResultsByBuild(buildId).ToList()))
                .ToList();
            var primaryBaselineTests = baselineTests[0].Tests;

            // Apply domain filter if specified
            if (selectedDomains != null && selectedDomains.Any())
            {
                var domainSet = new HashSet<string>(selectedDomains);
                todayTests = todayTests.Where(t => domainSet.Contains(t.DomainId)).ToList();
                for (var i = 0; i < baselineTests.Count; i++)
                {
                    baselineTests[i] = (baselineTests[i].BuildId, baselineTests[i].Tests.Where(t => domainSet.Contains(t.DomainId)).ToList());
                }
                primaryBaselineTests = baselineTests[0].Tests;
            }

            if (!todayTests.Any() || baselineTests.All(item => item.Tests.Count == 0))
            {
                _logger.LogWarning("No test results found for comparison");
                return null;
            }

            // Build fast lookup by (TestFullName, ConfigurationId)
            var todayByKey = todayTests
                .GroupBy(t => (t.TestFullName, t.ConfigurationId))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.Timestamp).First());

            var baselineLookups = baselineTests
                .Select(item => item.Tests
                    .GroupBy(t => (t.TestFullName, t.ConfigurationId))
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.Timestamp).First()))
                .ToList();

            var primaryBaselineByKey = baselineLookups[0];

            var newFailuresMap = new Dictionary<(string TestFullName, string DomainId, string FeatureId), TriageNewFailure>();
            var fixedTestsMap = new Dictionary<(string TestFullName, string DomainId, string FeatureId), TriageFixedTest>();
            var stillFailing = new List<TestResult>();

            foreach (var (key, todayResult) in todayByKey)
            {
                var matchingBaselines = baselineLookups
                    .Select(lookup => lookup.TryGetValue(key, out var baselineResult) ? baselineResult : null)
                    .Where(result => result != null)
                    .Select(result => result!)
                    .ToList();

                if (matchingBaselines.Count == 0)
                {
                    continue;
                }

                var passedInAnyBaseline = matchingBaselines.Any(result => result.Status == TestStatus.Pass);
                var failedInPrimaryBaseline = primaryBaselineByKey.TryGetValue(key, out var primaryBaselineResult)
                    && primaryBaselineResult.Status == TestStatus.Fail;

                if (todayResult.Status == TestStatus.Fail && passedInAnyBaseline)
                {
                    var bucketKey = (todayResult.TestFullName, todayResult.DomainId, todayResult.FeatureId);
                    if (!newFailuresMap.TryGetValue(bucketKey, out var entry))
                    {
                        entry = new TriageNewFailure
                        {
                            TestFullName = todayResult.TestFullName,
                            DomainId = todayResult.DomainId,
                            FeatureId = todayResult.FeatureId,
                            AffectedConfigs = new List<string>(),
                            ErrorMessage = todayResult.ErrorMessage ?? "No error message",
                            StackTrace = todayResult.StackTrace,
                            FailedOn = todayResult.Timestamp,
                            Category = _failureClassificationService.Classify(todayResult),
                            FailureSignature = _failureClassificationService.BuildFailureSignature(todayResult)
                        };
                        newFailuresMap[bucketKey] = entry;
                    }
                    entry.AffectedConfigs.Add(todayResult.ConfigurationId);
                }
                else if (todayResult.Status == TestStatus.Pass && failedInPrimaryBaseline)
                {
                    var bucketKey = (todayResult.TestFullName, todayResult.DomainId, todayResult.FeatureId);
                    if (!fixedTestsMap.TryGetValue(bucketKey, out var entry))
                    {
                        entry = new TriageFixedTest
                        {
                            TestFullName = todayResult.TestFullName,
                            DomainId = todayResult.DomainId,
                            FeatureId = todayResult.FeatureId,
                            FixedInConfigs = new List<string>(),
                            PassedOn = todayResult.Timestamp
                        };
                        fixedTestsMap[bucketKey] = entry;
                    }
                    entry.FixedInConfigs.Add(todayResult.ConfigurationId);
                }
                else if (todayResult.Status == TestStatus.Fail && !passedInAnyBaseline)
                {
                    stillFailing.Add(todayResult);
                }
            }

            var newFailures = newFailuresMap.Values.ToList();
            var fixedTests = fixedTestsMap.Values.ToList();

            // Calculate pass rates — single pass each to avoid 4 separate enumerations
            int todayPassed = 0, todayTotal = 0;
            foreach (var t in todayTests) { if (t.Status != TestStatus.Skip) { todayTotal++; if (t.Status == TestStatus.Pass) todayPassed++; } }
            var todayPassRate = todayTotal > 0 ? (double)todayPassed / todayTotal * 100 : 0;

            int yesterdayPassed = 0, yesterdayTotal = 0;
            foreach (var t in primaryBaselineTests) { if (t.Status != TestStatus.Skip) { yesterdayTotal++; if (t.Status == TestStatus.Pass) yesterdayPassed++; } }
            var yesterdayPassRate = yesterdayTotal > 0 ? (double)yesterdayPassed / yesterdayTotal * 100 : 0;

            var expectedConfigurations = GetExpectedConfigurations(todayBuildId, primaryBaseline, selectedDomains);
            var actualConfigurations = todayTests
                .Select(t => t.ConfigurationId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingConfigurations = expectedConfigurations
                .Where(configId => !actualConfigurations.Contains(configId))
                .OrderBy(configId => configId, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var completenessDrop = yesterdayTotal > 0
                ? Math.Max(0, yesterdayTotal - todayTotal)
                : 0;
            var isRunComplete = missingConfigurations.Count == 0 && (yesterdayTotal == 0 || todayTotal >= yesterdayTotal * 0.9);
            var completenessMessage = BuildCompletenessMessage(isRunComplete, missingConfigurations.Count, completenessDrop, todayTotal, yesterdayTotal);

            _logger.LogInformation(
                "Triage complete: {NewFailures} new failures, {FixedTests} fixed, {StillFailing} still failing",
                newFailures.Count, fixedTests.Count, stillFailing.Count);

            return new MorningTriageResult
            {
                TodayBuildId = todayBuildId,
                YesterdayBuildId = primaryBaseline,
                NewFailures = newFailures,
                FixedTests = fixedTests,
                StillFailing = stillFailing,
                TodayPassRate = todayPassRate,
                YesterdayPassRate = yesterdayPassRate,
                TotalTestsToday = todayTotal,
                TotalTestsYesterday = yesterdayTotal,
                ExpectedConfigurations = expectedConfigurations,
                MissingConfigurations = missingConfigurations,
                IsRunComplete = isRunComplete,
                CompletenessMessage = completenessMessage
            };
        }).ConfigureAwait(false);
    }

    private List<string> GetExpectedConfigurations(string todayBuildId, string yesterdayBuildId, List<string>? selectedDomains)
    {
        var buildIds = _testDataService.GetAllBuildIds()
            .OrderByDescending(BuildNumberExtractor.ExtractBuildNumber)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (!buildIds.Contains(todayBuildId, StringComparer.OrdinalIgnoreCase))
        {
            buildIds.Insert(0, todayBuildId);
        }

        if (!buildIds.Contains(yesterdayBuildId, StringComparer.OrdinalIgnoreCase))
        {
            buildIds.Insert(1, yesterdayBuildId);
        }

        var domainSet = selectedDomains != null && selectedDomains.Any()
            ? new HashSet<string>(selectedDomains, StringComparer.OrdinalIgnoreCase)
            : null;

        return buildIds
            .Take(5)
            .SelectMany(buildId => _testDataService.GetTestResultsByBuild(buildId))
            .Where(test => domainSet == null || domainSet.Contains(test.DomainId))
            .Select(test => test.ConfigurationId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(configId => configId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildCompletenessMessage(bool isRunComplete, int missingConfigCount, int completenessDrop, int todayTotal, int yesterdayTotal)
    {
        if (isRunComplete)
        {
            return "Run looks complete for overnight triage.";
        }

        var parts = new List<string>();
        if (missingConfigCount > 0)
        {
            parts.Add($"{missingConfigCount} configurations missing");
        }

        if (completenessDrop > 0)
        {
            parts.Add($"{completenessDrop} fewer executed tests than previous run");
        }

        if (parts.Count == 0 && todayTotal < yesterdayTotal)
        {
            parts.Add("test volume is lower than previous run");
        }

        return parts.Count == 0
            ? "Run may be incomplete."
            : string.Join("; ", parts) + ".";
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
