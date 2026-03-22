using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public interface IFailureClassificationService
{
    MorningFailureCategory Classify(TestResult testResult);

    string BuildFailureSignature(TestResult testResult);
}
