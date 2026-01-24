namespace TestResultBrowser.Web.Models;

/// <summary>
/// Represents the execution status of a test case
/// </summary>
public enum TestStatus
{
    /// <summary>Test passed successfully</summary>
    Pass,

    /// <summary>Test failed with an error or assertion failure</summary>
    Fail,

    /// <summary>Test was skipped or ignored</summary>
    Skip
}
