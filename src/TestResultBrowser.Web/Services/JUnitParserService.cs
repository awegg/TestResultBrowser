using System.Xml.Linq;
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
        _versionMapper = versionMapper;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<TestResult>> ParseJUnitXmlAsync(string xmlFilePath, ParsedFilePath parsedPath)
    {
        var results = new List<TestResult>();
        
        // Load XML document
        var xml = await Task.Run(() => XDocument.Load(xmlFilePath));
        
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
            await ProcessTestSuite(testSuite, parsedPath, results, xmlFilePath);
        }

        return results;
    }

    private async Task ProcessTestSuite(XElement testSuite, ParsedFilePath parsedPath, List<TestResult> results, string xmlFilePath)
    {
        var testSuiteName = testSuite.Attribute("name")?.Value ?? Path.GetFileNameWithoutExtension(xmlFilePath);
        var timestamp = testSuite.Attribute("timestamp")?.Value;
        DateTime testTimestamp = DateTime.UtcNow;
        
        if (!string.IsNullOrEmpty(timestamp) && DateTime.TryParse(timestamp, out var parsed))
        {
            testTimestamp = parsed.ToUniversalTime();
        }

        // Parse each testcase
        foreach (var testCase in testSuite.Descendants("testcase"))
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
            
            if (!double.TryParse(timeStr, out var executionTime))
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

            // Extract Polarion ticket IDs from properties
            var polarionTickets = new List<string>();
            var properties = testCase.Element("properties");
            if (properties != null)
            {
                foreach (var property in properties.Elements("property"))
                {
                    var name = property.Attribute("name")?.Value;
                    var value = property.Attribute("value")?.Value;
                    
                    if (name == "Polarion" && !string.IsNullOrEmpty(value))
                    {
                        // Extract PEXC-xxxxx pattern
                        var matches = System.Text.RegularExpressions.Regex.Matches(value, @"PEXC-\d+");
                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            polarionTickets.Add(match.Value);
                        }
                    }
                }
            }

            // Map version
            var version = _versionMapper.MapVersion(parsedPath.VersionRaw);

            // Build IDs with validation for required fields
            var versionStr = version ?? "<unknown>";
            if (string.IsNullOrEmpty(version))
            {
                _logger.LogWarning("Failed to map version from {VersionRaw} in file {File}. Using '<unknown>'", parsedPath.VersionRaw, xmlFilePath);
            }

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
                PolarionTickets = polarionTickets,
                ReportDirectoryPath = reportDirectory
            };

            results.Add(testResult);
        }

        await Task.CompletedTask;
    }
}
