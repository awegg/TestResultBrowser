using System.Collections.Concurrent;
using System.Diagnostics;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of test data service
/// Uses ConcurrentDictionary for thread-safe in-memory caching with secondary indices
/// </summary>
public class TestDataService : ITestDataService
{
    // Primary storage: TestResult.Id -> TestResult
    private readonly ConcurrentDictionary<string, TestResult> _testResults = new();

    // Secondary indices for O(1) lookups
    private readonly ConcurrentDictionary<string, HashSet<string>> _byDomain = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _byFeature = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _byConfiguration = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _byBuild = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _byTestName = new();

    // Lock for index updates
    private readonly object _indexLock = new();

    /// <inheritdoc/>
    public void AddOrUpdateTestResult(TestResult testResult)
    {
        _testResults[testResult.Id] = testResult;
        UpdateIndices(testResult);
    }

    /// <inheritdoc/>
    public void AddOrUpdateTestResults(IEnumerable<TestResult> testResults)
    {
        var resultsList = testResults.ToList();

        foreach (var testResult in resultsList)
        {
            _testResults[testResult.Id] = testResult;
        }

        lock (_indexLock)
        {
            foreach (var testResult in resultsList)
            {
                UpdateIndicesUnsafe(testResult);
            }
        }
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetAllTestResults()
    {
        return _testResults.Values;
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByDomain(string domainId)
    {
        if (_byDomain.TryGetValue(domainId, out var ids))
        {
            List<string> idSnapshot;
            lock (_indexLock)
            {
                idSnapshot = new List<string>(ids);
            }

            return idSnapshot.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                              .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByFeature(string featureId)
    {
        if (_byFeature.TryGetValue(featureId, out var ids))
        {
            List<string> idSnapshot;
            lock (_indexLock)
            {
                idSnapshot = new List<string>(ids);
            }

            return idSnapshot.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                              .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByConfiguration(string configurationId)
    {
        if (_byConfiguration.TryGetValue(configurationId, out var ids))
        {
            List<string> idSnapshot;
            lock (_indexLock)
            {
                idSnapshot = new List<string>(ids);
            }

            return idSnapshot.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                              .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByBuild(string buildId)
    {
        if (_byBuild.TryGetValue(buildId, out var ids))
        {
            List<string> idSnapshot;
            lock (_indexLock)
            {
                idSnapshot = new List<string>(ids);
            }

            return idSnapshot.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                              .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByTestName(string testFullName)
    {
        if (_byTestName.TryGetValue(testFullName, out var ids))
        {
            List<string> idSnapshot;
            lock (_indexLock)
            {
                idSnapshot = new List<string>(ids);
            }

            return idSnapshot.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                              .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public TestResult? GetTestResultById(string id)
    {
        _testResults.TryGetValue(id, out var result);
        return result;
    }

    /// <inheritdoc/>
    public int GetTotalCount()
    {
        return _testResults.Count;
    }

    /// <inheritdoc/>
    public long GetApproximateMemoryUsage()
    {
        // Use actual process memory instead of calculation
        using var currentProcess = Process.GetCurrentProcess();
        return currentProcess.WorkingSet64;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _testResults.Clear();
        
        lock (_indexLock)
        {
            _byDomain.Clear();
            _byFeature.Clear();
            _byConfiguration.Clear();
            _byBuild.Clear();
            _byTestName.Clear();
        }
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAllBuildIds()
    {
        return _byBuild.Keys.OrderByDescending(b => b);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAllConfigurationIds()
    {
        return _byConfiguration.Keys.OrderBy(c => c);
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAllDomainIds()
    {
        return _byDomain.Keys.OrderBy(d => d);
    }

    /// <inheritdoc/>
    public (DateTime? Earliest, DateTime? Latest) GetDateRange()
    {
        var results = _testResults.Values;
        if (!results.Any())
            return (null, null);

        return (results.Min(r => r.Timestamp), results.Max(r => r.Timestamp));
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAllVersions()
    {
        return _testResults.Values
            .Select(r => r.ConfigurationId.Split('_').FirstOrDefault())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct()
            .OrderBy(v => v)!;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetAllNamedConfigs()
    {
        return _testResults.Values
            .Select(r =>
            {
                var parts = r.ConfigurationId.Split('_');
                return parts.Length >= 3 ? parts[2] : null;
            })
            .Where(nc => !string.IsNullOrEmpty(nc))
            .Distinct()
            .OrderBy(nc => nc)!;
    }

    private void UpdateIndices(TestResult testResult)
    {
        lock (_indexLock)
        {
            UpdateIndicesUnsafe(testResult);
        }
    }

    private void UpdateIndicesUnsafe(TestResult testResult)
    {
        // Add to domain index
        _byDomain.AddOrUpdate(testResult.DomainId,
            _ => new HashSet<string> { testResult.Id },
            (_, set) => { set.Add(testResult.Id); return set; });

        // Add to feature index
        _byFeature.AddOrUpdate(testResult.FeatureId,
            _ => new HashSet<string> { testResult.Id },
            (_, set) => { set.Add(testResult.Id); return set; });

        // Add to configuration index
        _byConfiguration.AddOrUpdate(testResult.ConfigurationId,
            _ => new HashSet<string> { testResult.Id },
            (_, set) => { set.Add(testResult.Id); return set; });

        // Add to build index
        _byBuild.AddOrUpdate(testResult.BuildId,
            _ => new HashSet<string> { testResult.Id },
            (_, set) => { set.Add(testResult.Id); return set; });

        // Add to test name index
        _byTestName.AddOrUpdate(testResult.TestFullName,
            _ => new HashSet<string> { testResult.Id },
            (_, set) => { set.Add(testResult.Id); return set; });
    }
}
