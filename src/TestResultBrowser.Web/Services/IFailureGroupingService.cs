using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public interface IFailureGroupingService
{
    /// <summary>
    /// Groups failed test results by error message using exact match first,
    /// then merges near-duplicates using Levenshtein similarity threshold.
    /// </summary>
    /// <param name="failedResults">Collection of failed test results</param>
    /// <param name="similarityThreshold">0.0-1.0 threshold (default 0.8)</param>
    /// <returns>List of failure groups</returns>
    List<FailureGroup> GroupFailures(IEnumerable<TestResult> failedResults, double similarityThreshold = 0.8);
}
