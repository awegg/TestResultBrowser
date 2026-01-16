using System.Collections.Concurrent;
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
        foreach (var testResult in testResults)
        {
            _testResults[testResult.Id] = testResult;
        }

        lock (_indexLock)
        {
            foreach (var testResult in testResults)
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
            return ids.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                     .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByFeature(string featureId)
    {
        if (_byFeature.TryGetValue(featureId, out var ids))
        {
            return ids.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                     .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByConfiguration(string configurationId)
    {
        if (_byConfiguration.TryGetValue(configurationId, out var ids))
        {
            return ids.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                     .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByBuild(string buildId)
    {
        if (_byBuild.TryGetValue(buildId, out var ids))
        {
            return ids.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
                     .Where(r => r != null)!;
        }
        return Enumerable.Empty<TestResult>();
    }

    /// <inheritdoc/>
    public IEnumerable<TestResult> GetTestResultsByTestName(string testFullName)
    {
        if (_byTestName.TryGetValue(testFullName, out var ids))
        {
            return ids.Select(id => _testResults.TryGetValue(id, out var result) ? result : null)
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
        // Approximate calculation:
        // Each TestResult ~400 bytes base + strings
        // Secondary indices overhead ~100 bytes per entry
        return _testResults.Count * 500L;
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
