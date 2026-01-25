namespace TestResultBrowser.Web.Common;

/// <summary>
/// Constants used throughout the test result browser application
/// Eliminates magic strings and provides single source of truth
/// </summary>
public static class TestResultConstants
{
    /// <summary>
    /// File system path patterns
    /// </summary>
    public static class Paths
    {
        public const string ReleaseFolderPrefix = "Release-";
        /// <summary>
        /// Pattern matches all XML files. This is intentionally broad because the search
        /// directory (sample_data/Release-{number}) only contains JUnit result files.
        /// </summary>
        public const string JUnitFilePattern = "*.xml";
        public const string TestResultsPattern = "tests-*.xml";
    }

    /// <summary>
    /// Regular expression patterns for parsing
    /// </summary>
    public static class RegexPatterns
    {
        /// <summary>
        /// Pattern for extracting work item IDs (e.g., PEXC-28044)
        /// </summary>
        public const string WorkItemId = @"PEXC-\d+";

        /// <summary>
        /// Pattern for parsing version codes (e.g., PXrel114 = 1.14.0)
        /// </summary>
        public const string VersionCode = @"PXrel(\d+)";

        /// <summary>
        /// Pattern for extracting build number from folder name
        /// </summary>
        public const string BuildNumber = @"Release-(\d+)";
    }

    /// <summary>
    /// Configuration keys and defaults
    /// </summary>
    public static class Configuration
    {
        public const string SectionName = "TestResultBrowser";
        public const int DefaultPollingIntervalMinutes = 15;
        public const int DefaultRollingWindowSize = 20;
        public const int DefaultFlakinessTriggerPercentage = 30;
        public const int DefaultClearAfterConsecutivePasses = 10;
        public const int MaxHistoryBuilds = 20;
        public const int DefaultHistoryBuilds = 5;
    }

    /// <summary>
    /// SignalR hub event names
    /// </summary>
    public static class SignalREvents
    {
        public const string TestDataUpdated = "TestDataUpdated";
    }

    /// <summary>
    /// Version mapping constants
    /// </summary>
    public static class Versions
    {
        public const string DevelopmentVersion = "dev";
        public const string DevelopmentDisplayName = "Development";
    }

    /// <summary>
    /// Default categorization values
    /// </summary>
    public static class Defaults
    {
        public const string UncategorizedDomain = "Uncategorized";
        public const string UnknownFeature = "Unknown";
        public const string UnknownVersion = "<unknown>";
    }
}
