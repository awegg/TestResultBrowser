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
            .OrderByDescending(b => b)
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
                        var existing = newFailures.FirstOrDefault(f => f.TestFullName == testName);
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
                        var existing = fixedTests.FirstOrDefault(f => f.TestFullName == testName);
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
        });
    }
}
