using System.Xml.Linq;
using TestResultBrowser.Web.Common;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Implementation of JUnit XML parser service
/// Parses JUnit XML files into TestResult objects
/// </summary>
public class JUnitParserService : IJUnitParserService
{
    private readonly IVersionMapperService _versionMapper;
    private readonly ILogger<JUnitParserService> _logger;

    public JUnitParserService(IVersionMapperService versionMapper, ILogger<JUnitParserService> logger)
    {
        _versionMapper = versionMapper ?? throw new ArgumentNullException(nameof(versionMapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<List<TestResult>> ParseJUnitXmlAsync(string xmlFilePath, ParsedFilePath parsedPath)
    {
        var results = new List<TestResult>();

        // Load XML document
        XDocument xml;
        try
        {
            xml = await Task.Run(() => XDocument.Load(xmlFilePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse JUnit XML file: {FilePath}", xmlFilePath);
            return results;
        }

        // Get all testsuite elements (handle both <testsuite> root and <testsuites><testsuite> nested)
        var testSuites = xml.Root?.Name.LocalName == "testsuite"
            ? new[] { xml.Root }
            : xml.Descendants("testsuite");

        if (testSuites == null || !testSuites.Any())
        {
            return results;
        }

        // Process each testsuite
        foreach (var testSuite in testSuites)
        {
            ProcessTestSuite(testSuite, parsedPath, results, xmlFilePath);
        }

        return results;
    }

    private void ProcessTestSuite(XElement testSuite, ParsedFilePath parsedPath, List<TestResult> results, string xmlFilePath)
    {
        var testSuiteName = testSuite.Attribute("name")?.Value ?? Path.GetFileNameWithoutExtension(xmlFilePath);
        var timestamp = testSuite.Attribute("timestamp")?.Value;
        DateTime testTimestamp = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, out var parsed))
        {
            testTimestamp = parsed.ToUniversalTime();
        }

        // Parse each testcase (direct children only to avoid duplicate parsing from nested suites)
        foreach (var testCase in testSuite.Elements("testcase"))
        {
            var classNameAttr = testCase.Attribute("classname")?.Value;
            var methodNameAttr = testCase.Attribute("name")?.Value;
            var timeStr = testCase.Attribute("time")?.Value ?? "0";

            // Validate required fields and log warnings for missing data
            var className = classNameAttr;
            if (string.IsNullOrEmpty(className))
            {
                className = "<unknown>";
                _logger.LogWarning("Test case missing 'classname' attribute in file {File}. Using '<unknown>'", xmlFilePath);
            }

            var methodName = methodNameAttr;
            if (string.IsNullOrEmpty(methodName))
            {
                methodName = "<unknown>";
                _logger.LogWarning("Test case missing 'name' attribute in file {File}. Using '<unknown>'", xmlFilePath);
            }

            if (!double.TryParse(timeStr, System.Globalization.CultureInfo.InvariantCulture, out var executionTime))
            {
                executionTime = 0;
            }

            // Determine status and error details
            var failure = testCase.Element("failure");
            var error = testCase.Element("error");
            var skipped = testCase.Element("skipped");

            TestStatus status;
            string? errorMessage = null;
            string? stackTrace = null;

            if (failure != null)
            {
                status = TestStatus.Fail;
                errorMessage = failure.Attribute("message")?.Value;
                stackTrace = failure.Value;
            }
            else if (error != null)
            {
                status = TestStatus.Fail;
                errorMessage = error.Attribute("message")?.Value;
                stackTrace = error.Value;
            }
            else if (skipped != null)
            {
                status = TestStatus.Skip;
            }
            else
            {
                status = TestStatus.Pass;
            }

            // Extract work item IDs from multiple sources: properties, classname, method name
            var workItemIds = new List<string>();
            var ticketSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Try to extract from <property name="Polarion">
            var properties = testCase.Element("properties");
            if (properties != null)
            {
                foreach (var property in properties.Elements("property"))
                {
                    var name = property.Attribute("name")?.Value;
                    var value = property.Attribute("value")?.Value;

                    if (name == "Polarion" && !string.IsNullOrEmpty(value))
                    {
                        // Extract PEXC-xxxxx pattern using constant from TestResultConstants
                        var matches = System.Text.RegularExpressions.Regex.Matches(value, TestResultConstants.RegexPatterns.WorkItemId);
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            ticketSet.Add(match.Value);
                        }
                    }
                }
            }

            // 2. Extract from classname (e.g., "PEXC-3471 - The current time is displayed...")
            if (!string.IsNullOrEmpty(classNameAttr))
            {
                var classMatches = System.Text.RegularExpressions.Regex.Matches(classNameAttr, TestResultConstants.RegexPatterns.WorkItemId);
                foreach (System.Text.RegularExpressions.Match match in classMatches)
                {
                    ticketSet.Add(match.Value);
                }
            }

            // 3. Extract from method name (test case name)
            if (!string.IsNullOrEmpty(methodNameAttr))
            {
                var methodMatches = System.Text.RegularExpressions.Regex.Matches(methodNameAttr, TestResultConstants.RegexPatterns.WorkItemId);
                foreach (System.Text.RegularExpressions.Match match in methodMatches)
                {
                    ticketSet.Add(match.Value);
                }
            }

            // Convert set to list for deduplication
            workItemIds = ticketSet.ToList();

            // Map version (always returns non-null string per IVersionMapperService contract)
            var version = _versionMapper.MapVersion(parsedPath.VersionRaw);
            var versionStr = version ?? "<unknown>";

            var configurationId = $"{versionStr}_{parsedPath.TestType}_{parsedPath.NamedConfig}_{parsedPath.DomainId}";
            var testFullName = $"{className}.{methodName}";
            var testResultId = $"{configurationId}_{parsedPath.BuildId}_{testFullName}";

            // Extract OS and DB from NamedConfig (e.g., Win2019SQLServer2022)
            string? os = null;
            string? db = null;

            if (parsedPath.NamedConfig.Contains("Win"))
            {
                var osMatch = System.Text.RegularExpressions.Regex.Match(parsedPath.NamedConfig, @"Win\d+").Value;
                os = !string.IsNullOrEmpty(osMatch) ? osMatch : "<unknown>";
            }
            else if (parsedPath.NamedConfig.Contains("Ubuntu"))
            {
                os = "Ubuntu";
            }
            else
            {
                os = "<unknown>";
            }

            if (parsedPath.NamedConfig.Contains("SQL"))
            {
                var dbMatch = System.Text.RegularExpressions.Regex.Match(parsedPath.NamedConfig, @"SQLServer\d+").Value;
                db = !string.IsNullOrEmpty(dbMatch) ? dbMatch : "<unknown>";
            }
            else if (parsedPath.NamedConfig.Contains("MySQL"))
            {
                db = "MySQL";
            }
            else
            {
                db = "<unknown>";
            }

            // Extract feature directory name from the report path
            var reportDirectory = Path.GetDirectoryName(xmlFilePath);
            var featureDirectoryName = !string.IsNullOrEmpty(reportDirectory)
                ? new System.IO.DirectoryInfo(reportDirectory).Name
                : "Unknown";

            var testResult = new TestResult
            {
                Id = testResultId,
                TestFullName = testFullName,
                ClassName = className,
                MethodName = methodName,
                Status = status,
                ExecutionTimeSeconds = executionTime,
                Timestamp = testTimestamp,
                ErrorMessage = errorMessage,
                StackTrace = stackTrace,
                DomainId = parsedPath.DomainId,
                FeatureId = $"{parsedPath.DomainId}_{className.Split('.')[0]}", // Approximate feature from namespace
                TestSuiteId = $"{parsedPath.DomainId}_{testSuiteName}",
                ConfigurationId = configurationId,
                BuildId = parsedPath.BuildId,
                BuildNumber = parsedPath.BuildNumber,
                Machine = os ?? parsedPath.NamedConfig,
                Feature = featureDirectoryName,
                WorkItemIds = workItemIds,
                ReportDirectoryPath = reportDirectory
            };

            results.Add(testResult);
        }
    }
}
