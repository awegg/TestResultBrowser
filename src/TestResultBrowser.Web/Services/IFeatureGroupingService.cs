namespace TestResultBrowser.Web.Services;

public interface IFeatureGroupingService
{
    Dictionary<string, List<string>> BuildFeatureGroups(IEnumerable<string> featureNames);
    Dictionary<string, List<string>> GetFilteredFeatureGroups(
        Dictionary<string, List<string>> featureGroups,
        string? searchText);
    string ExtractGroupFromFeatureName(string featureName);
}
