using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for parsing JUnit XML test result files
/// Parses testsuite and testcase elements into TestResult objects
/// </summary>
public interface IJUnitParserService
{
    /// <summary>
    /// Parses a JUnit XML file and returns list of test results
    /// </summary>
    /// <param name="xmlFilePath">Full path to JUnit XML file</param>
    /// <param name="parsedPath">Parsed file path metadata (build/version/config)</param>
    /// <returns>List of TestResult objects parsed from XML</returns>
    /// <remarks>
    /// Parses XML structure:
    /// testsuite[@name]: Maps to TestSuite
    /// testcase[@classname, @name, @time]: Maps to TestResult
    /// failure/error: Maps to Status=Fail with ErrorMessage/StackTrace
    /// skipped: Maps to Status=Skip
    /// properties/property[@name="Polarion"]: Extracts PEXC-xxxxx ticket IDs
    /// </remarks>
    Task<List<TestResult>> ParseJUnitXmlAsync(string xmlFilePath, ParsedFilePath parsedPath);
}
