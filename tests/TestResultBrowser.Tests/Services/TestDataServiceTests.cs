using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class TestDataServiceTests
{
    private TestResult Make(string id, string domain, string feature, string cfg, string buildId, int buildNumber, string testName, DateTime ts)
    {
        return new TestResult
        {
            Id = id,
            TestFullName = testName,
            ClassName = "Class",
            MethodName = "Method",
            Status = TestStatus.Pass,
            ExecutionTimeSeconds = 1.23,
            Timestamp = ts,
            DomainId = domain,
            FeatureId = feature,
            TestSuiteId = "suite",
            ConfigurationId = cfg,
            BuildId = buildId,
            BuildNumber = buildNumber,
            Machine = "host",
            Feature = feature,
            PolarionTickets = new List<string>(),
            ReportDirectoryPath = null
        };
    }

    [Fact]
    public void AddOrUpdateTestResult_ShouldBeRetrievable()
    {
        var svc = new TestDataService();
        var r = Make("cfgA_build1_TestA", "Core", "Alarm", "1.14.0_E2E_Default1_Core", "Release-252_181639", 252, "TestA", DateTime.UtcNow);
        svc.AddOrUpdateTestResult(r);

        svc.GetTotalCount().ShouldBe(1);
        svc.GetTestResultById(r.Id).ShouldNotBeNull();
        svc.GetTestResultsByDomain("Core").ShouldHaveSingleItem();
        svc.GetTestResultsByFeature("Alarm").ShouldHaveSingleItem();
    }

    [Fact]
    public void AddOrUpdateTestResults_Batch_ShouldPopulateIndices()
    {
        var svc = new TestDataService();
        var now = DateTime.UtcNow;
        var results = new[]
        {
            Make("cfgA_b1_tA", "Core", "Alarm", "1.14.0_E2E_Default1_Core", "Release-252_181639", 252, "A.A", now.AddMinutes(-5)),
            Make("cfgA_b1_tB", "Core", "Alarm", "1.14.0_E2E_Default1_Core", "Release-252_181639", 252, "A.B", now.AddMinutes(-4)),
            Make("cfgB_b2_tC", "Core", "Dashboard", "1.14.0_E2E_Default2_Core", "Release-253_181700", 253, "B.C", now.AddMinutes(-3)),
            Make("cfgC_b2_tD", "TnT_Prod", "Dashboard", "1.14.0_E2E_Default2_TnT_Prod", "Release-253_181700", 253, "B.D", now.AddMinutes(-2)),
        };
        svc.AddOrUpdateTestResults(results);

        svc.GetTotalCount().ShouldBe(4);
        svc.GetTestResultsByDomain("Core").Count().ShouldBe(3);
        svc.GetTestResultsByFeature("Dashboard").Count().ShouldBe(2);
        svc.GetTestResultsByConfiguration("1.14.0_E2E_Default1_Core").Count().ShouldBe(2);
        svc.GetTestResultsByBuild("Release-252_181639").Count().ShouldBe(2);
        svc.GetTestResultsByTestName("A.A").Count().ShouldBe(1);
    }

    [Fact]
    public void GetAllIds_Aggregates_ShouldReturnSets()
    {
        var svc = new TestDataService();
        var now = DateTime.UtcNow;
        svc.AddOrUpdateTestResults(new[]
        {
            Make("cfgA_b1_a", "Core", "Alarm", "1.14.0_E2E_Default1_Core", "Release-252_181639", 252, "A", now),
            Make("cfgB_b2_b", "Core", "Alarm", "1.14.0_E2E_Default2_Core", "Release-253_181700", 253, "B", now),
            Make("cfgC_b3_c", "Core", "Alarm", "1.15.0_E2E_Default3_Core", "Release-254_181800", 254, "C", now)
        });

        var buildIds = svc.GetAllBuildIds();
        buildIds.ShouldContain("Release-252_181639");
        buildIds.ShouldContain("Release-253_181700");
        buildIds.ShouldContain("Release-254_181800");

        var configurationIds = svc.GetAllConfigurationIds();
        configurationIds.ShouldContain("1.14.0_E2E_Default1_Core");
        configurationIds.ShouldContain("1.14.0_E2E_Default2_Core");
        configurationIds.ShouldContain("1.15.0_E2E_Default3_Core");

        var domainIds = svc.GetAllDomainIds();
        domainIds.ShouldContain("Core");

        var versions = svc.GetAllVersions();
        versions.ShouldContain("1.14.0");
        versions.ShouldContain("1.15.0");

        var namedConfigs = svc.GetAllNamedConfigs();
        namedConfigs.ShouldContain("Default1");
        namedConfigs.ShouldContain("Default2");
        namedConfigs.ShouldContain("Default3");
    }

    [Fact]
    public void GetDateRange_ShouldReturnEarliestAndLatest()
    {
        var svc = new TestDataService();
        var now = DateTime.UtcNow;
        svc.AddOrUpdateTestResults(new[]
        {
            Make("id1", "Core", "Alarm", "vA_E2E_Default1_Core", "B1", 1, "T1", now.AddMinutes(-10)),
            Make("id2", "Core", "Alarm", "vA_E2E_Default1_Core", "B1", 1, "T2", now.AddMinutes(0)),
        });

        var (earliest, latest) = svc.GetDateRange();
        earliest.ShouldNotBeNull();
        (earliest.Value - now.AddMinutes(-10)).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
        latest.ShouldNotBeNull();
        (latest.Value - now).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetBuildTimestamp_ShouldReturnEarliestForBuild()
    {
        var svc = new TestDataService();
        var now = DateTime.UtcNow;
        svc.AddOrUpdateTestResults(new[]
        {
            Make("id1", "Core", "Alarm", "cfg", "B1", 1, "T1", now.AddMinutes(-3)),
            Make("id2", "Core", "Alarm", "cfg", "B1", 1, "T2", now.AddMinutes(-1)),
        });

        var ts = svc.GetBuildTimestamp("B1");
        ts.ShouldNotBeNull();
        (ts.Value - now.AddMinutes(-3)).Duration().ShouldBeLessThan(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Clear_ShouldResetAll()
    {
        var svc = new TestDataService();
        svc.AddOrUpdateTestResult(Make("id", "Core", "Feat", "cfg", "B1", 1, "T", DateTime.UtcNow));
        svc.Clear();
        svc.GetTotalCount().ShouldBe(0);
        svc.GetAllBuildIds().ShouldBeEmpty();
        svc.GetAllConfigurationIds().ShouldBeEmpty();
        svc.GetAllDomainIds().ShouldBeEmpty();
    }
}
