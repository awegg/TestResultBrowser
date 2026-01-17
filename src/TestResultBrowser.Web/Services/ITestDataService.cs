using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for managing in-memory test data cache
/// Provides O(1) lookups via secondary indices
/// </summary>
public interface ITestDataService
{
    /// <summary>
    /// Adds or updates a test result in the cache
    /// Updates all secondary indices
    /// </summary>
    void AddOrUpdateTestResult(TestResult testResult);

    /// <summary>
    /// Adds or updates multiple test results in batch
    /// More efficient than individual calls
    /// </summary>
    void AddOrUpdateTestResults(IEnumerable<TestResult> testResults);

    /// <summary>
    /// Gets all test results (use with caution for large datasets)
    /// </summary>
    IEnumerable<TestResult> GetAllTestResults();

    /// <summary>
    /// Gets test results by domain ID
    /// </summary>
    IEnumerable<TestResult> GetTestResultsByDomain(string domainId);

    /// <summary>
    /// Gets test results by feature ID
    /// </summary>
    IEnumerable<TestResult> GetTestResultsByFeature(string featureId);

    /// <summary>
    /// Gets test results by configuration ID
    /// </summary>
    IEnumerable<TestResult> GetTestResultsByConfiguration(string configurationId);

    /// <summary>
    /// Gets test results by build ID
    /// </summary>
    IEnumerable<TestResult> GetTestResultsByBuild(string buildId);

    /// <summary>
    /// Gets test results by test name (full class.method name)
    /// </summary>
    IEnumerable<TestResult> GetTestResultsByTestName(string testFullName);

    /// <summary>
    /// Gets a specific test result by ID
    /// </summary>
    TestResult? GetTestResultById(string id);

    /// <summary>
    /// Gets count of test results in cache
    /// </summary>
    int GetTotalCount();

    /// <summary>
    /// Gets approximate memory usage in bytes
    /// </summary>
    long GetApproximateMemoryUsage();

    /// <summary>
    /// Clears all cached data
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets all unique build IDs
    /// </summary>
    IEnumerable<string> GetAllBuildIds();

    /// <summary>
    /// Gets all unique configuration IDs
    /// </summary>
    IEnumerable<string> GetAllConfigurationIds();

    /// <summary>
    /// Gets all unique domain IDs
    /// </summary>
    IEnumerable<string> GetAllDomainIds();

    /// <summary>
    /// Gets date range of test results (earliest and latest execution times)
    /// </summary>
    (DateTime? Earliest, DateTime? Latest) GetDateRange();

    /// <summary>
    /// Gets all unique versions from configurations
    /// </summary>
    IEnumerable<string> GetAllVersions();

    /// <summary>
    /// Gets all unique named configurations
    /// </summary>
    IEnumerable<string> GetAllNamedConfigs();
}
