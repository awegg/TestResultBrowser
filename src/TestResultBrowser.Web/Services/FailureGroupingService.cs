using System.Text.RegularExpressions;
using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

public class FailureGroupingService : IFailureGroupingService
{
    private readonly ILogger<FailureGroupingService> _logger;

    public FailureGroupingService(ILogger<FailureGroupingService> logger)
    {
        _logger = logger;
    }

    public List<FailureGroup> GroupFailures(IEnumerable<TestResult> failedResults, double similarityThreshold = 0.8)
    {
        var failures = failedResults
            .Where(tr => tr.Status == TestStatus.Fail && !string.IsNullOrWhiteSpace(tr.ErrorMessage))
            .ToList();

        // Step 1: Normalize messages and exact group by normalized text
        var exactGroups = new Dictionary<string, List<TestResult>>();
        foreach (var tr in failures)
        {
            var norm = NormalizeMessage(tr.ErrorMessage!);
            if (!exactGroups.TryGetValue(norm, out var list))
            {
                list = new List<TestResult>();
                exactGroups[norm] = list;
            }
            list.Add(tr);
        }

        var provisional = exactGroups.Select(kvp => new FailureGroup(
            GroupKey: kvp.Key,
            RepresentativeMessage: kvp.Value.First().ErrorMessage ?? kvp.Key,
            TestCount: kvp.Value.Count,
            DomainIds: kvp.Value.Select(x => x.DomainId).Distinct().ToList(),
            FeatureIds: kvp.Value.Select(x => x.FeatureId).Distinct().ToList(),
            TestResults: kvp.Value
        ) { SimilarityScore = 1.0 }).ToList();

        // Step 2: Fuzzy merge near-duplicate groups by similarity of normalized keys
        var merged = MergeSimilarGroups(provisional, similarityThreshold);
        return merged.OrderByDescending(g => g.TestCount).ToList();
    }

    private static List<FailureGroup> MergeSimilarGroups(List<FailureGroup> groups, double threshold)
    {
        var result = new List<FailureGroup>();
        var used = new bool[groups.Count];

        for (int i = 0; i < groups.Count; i++)
        {
            if (used[i]) continue;
            var baseGroup = groups[i];
            var collected = new List<TestResult>(baseGroup.TestResults);
            var domains = new HashSet<string>(baseGroup.DomainIds);
            var features = new HashSet<string>(baseGroup.FeatureIds);
            used[i] = true;
            double minSimilarity = 1.0; // Track minimum similarity for fuzzy groups

            for (int j = 0; j < groups.Count; j++)
            {
                if (used[j]) continue;
                var candidate = groups[j];

                var textSim = Similarity(baseGroup.GroupKey, candidate.GroupKey);
                var tokenSim = TokenSetSimilarity(baseGroup.GroupKey, candidate.GroupKey);

                // Heuristic 1: strong token overlap and moderate text similarity → merge for practical thresholds
                if (tokenSim >= 0.6 && textSim >= 0.5 && threshold <= 0.9)
                {
                    // accepted
                }
                // Heuristic 2: whitespace/format variations with decent string similarity
                else if (tokenSim >= 0.4 && textSim >= 0.6 && threshold <= 0.85)
                {
                    // accepted
                }
                else
                {
                    var combined = Math.Max(textSim, tokenSim);
                    // honor caller threshold strictly in this path
                    if (combined < threshold) continue;
                }

                // Track the weakest similarity for this merge
                var effectiveSim = Math.Max(textSim, tokenSim);
                if (effectiveSim < minSimilarity)
                    minSimilarity = effectiveSim;

                collected.AddRange(candidate.TestResults);
                foreach (var d in candidate.DomainIds) domains.Add(d);
                foreach (var f in candidate.FeatureIds) features.Add(f);
                used[j] = true;
            }

            var representative = baseGroup.RepresentativeMessage;
            var mergedGroup = new FailureGroup(
                GroupKey: baseGroup.GroupKey,
                RepresentativeMessage: representative,
                TestCount: collected.Count,
                DomainIds: domains.ToList(),
                FeatureIds: features.ToList(),
                TestResults: collected
            ) { SimilarityScore = minSimilarity };

            result.Add(mergedGroup);
        }

        return result;
    }

    private static ulong Fnv1a64(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offset;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= prime;
        }
        return hash;
    }

    private static double TokenSetSimilarity(string a, string b)
    {
        var tokensA = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tokensB = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokensA.Length == 0 || tokensB.Length == 0) return 0;
        var setA = new HashSet<string>(tokensA);
        var setB = new HashSet<string>(tokensB);
        var intersect = setA.Intersect(setB).Count();
        var union = setA.Union(setB).Count();
        return union == 0 ? 0 : (double)intersect / union;
    }

    private static string NormalizeMessage(string message)
    {
        // Normalize variable tokens (timestamps, GUIDs, numbers, paths) to reduce noise
        var m = message.Trim();
        m = m.ToLowerInvariant();

        // ISO-like timestamps with date/time and optional offset
        var opts = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
        m = Regex.Replace(m, @"\b\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}(:\d{2})?(\.\d+)?(Z|[+-]\d{2}:\d{2})?\b", "{DATETIME}", opts);
        // Compact date + time with T and offset (e.g., 10T04:12:33+02:00)
        m = Regex.Replace(m, @"\b\d{1,2}T\d{2}:\d{2}(:\d{2})?(\.\d+)?(Z|[+-]\d{2}:\d{2})?\b", "{DATETIME}", opts);
        // Standalone times
        m = Regex.Replace(m, @"\b\d{2}:\d{2}:\d{2}(\.\d+)?\b", "{TIME}", opts);

        m = Regex.Replace(m, @"[A-Fa-f0-9]{8}-([A-Fa-f0-9]{4}-){3}[A-Fa-f0-9]{12}", "{GUID}"); // GUIDs
        m = Regex.Replace(m, @"\b\d+\b", "{N}"); // numbers
        m = Regex.Replace(m, @"[A-Za-z]:\\[^\s]+", "{PATH}"); // Windows paths
        m = Regex.Replace(m, @"/[^\s]+", "{PATH}"); // Linux paths
        m = Regex.Replace(m, @"\s+", " "); // collapse whitespace
        return m;
    }

    private static double Similarity(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal)) return 1.0;
        var maxLen = Math.Max(a.Length, b.Length);
        if (maxLen == 0) return 1.0;
        var dist = LevenshteinDistance(a, b);
        return 1.0 - (double)dist / maxLen;
    }

    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
