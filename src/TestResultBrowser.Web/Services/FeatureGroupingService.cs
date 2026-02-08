namespace TestResultBrowser.Web.Services;

public class FeatureGroupingService : IFeatureGroupingService
{
    private static readonly string[] GroupSuffixes =
    {
        "Regression",
        "Smoke",
        "Archive Viewer",
        "Public API"
    };

    public Dictionary<string, List<string>> BuildFeatureGroups(IEnumerable<string> featureNames)
    {
        var featureGroups = new Dictionary<string, List<string>>();
        var allFeatures = featureNames
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(f => f)
            .ToList();

        foreach (var feature in allFeatures)
        {
            var group = ExtractGroupFromFeatureName(feature);
            if (!featureGroups.TryGetValue(group, out var list))
            {
                list = new List<string>();
                featureGroups[group] = list;
            }
            list.Add(feature);
        }

        foreach (var key in featureGroups.Keys.ToList())
        {
            featureGroups[key] = featureGroups[key]
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToList();
        }

        return featureGroups;
    }

    public Dictionary<string, List<string>> GetFilteredFeatureGroups(
        Dictionary<string, List<string>> featureGroups,
        string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return featureGroups;
        }

        var filtered = new Dictionary<string, List<string>>();
        foreach (var group in featureGroups)
        {
            var matchingFeatures = group.Value
                .Where(f => f.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (group.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase) || matchingFeatures.Count > 0)
            {
                filtered[group.Key] = matchingFeatures.Count > 0 ? matchingFeatures : group.Value;
            }
        }

        return filtered;
    }

    public string ExtractGroupFromFeatureName(string featureName)
    {
        if (string.IsNullOrEmpty(featureName))
        {
            return "Other";
        }

        var name = featureName.Trim();
        var groupName = name;

        var dashParts = name.Split(" - ");
        if (dashParts.Length > 1)
        {
            groupName = dashParts[0].Trim();
        }
        else
        {
            foreach (var suffix in GroupSuffixes)
            {
                if (name.EndsWith(" " + suffix, StringComparison.OrdinalIgnoreCase))
                {
                    groupName = name.Substring(0, name.Length - (" " + suffix).Length).Trim();
                    break;
                }
            }
        }

        if (groupName.StartsWith("Px ", StringComparison.OrdinalIgnoreCase))
        {
            var withoutPx = groupName.Substring(3).Trim();
            return "PX " + withoutPx;
        }

        return groupName;
    }
}
