namespace TestResultBrowser.Web.Security;

/// <summary>
/// Validates file paths for security vulnerabilities
/// </summary>
public static class PathValidator
{
    // Get invalid characters but exclude directory separators which are valid in paths
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars()
        .Concat(Path.GetInvalidFileNameChars())
        .Except(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar })
        .Distinct()
        .ToArray();

    // Characters that should be treated as invalid across platforms
    private static readonly char[] AlwaysInvalidChars = new[] { '<', '>', ':', '"', '|', '?', '*' };

    /// <summary>
    /// Validates that a path is safe (no traversal, invalid characters, etc.)
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <returns>True if the path is safe, false otherwise</returns>
    public static bool IsPathSafe(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Treat Windows drive rooted (e.g., C:\ or C:/) and UNC paths (\\server\share) as rooted across platforms
        if (System.Text.RegularExpressions.Regex.IsMatch(path, @"^(?:[A-Za-z]:[\\/])|^(?:\\\\)"))
        {
            return false;
        }

        // Check for path traversal
        if (path.Contains(".."))
        {
            return false;
        }

        // Check for rooted paths (absolute paths)
        if (Path.IsPathRooted(path))
        {
            return false;
        }

        // Check for invalid characters
        if (path.IndexOfAny(InvalidPathChars) >= 0)
        {
            return false;
        }

        // Cross-platform invalid characters
        if (path.IndexOfAny(AlwaysInvalidChars) >= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Validates that a path contains only valid filename characters
    /// </summary>
    public static bool HasValidCharacters(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.IndexOfAny(InvalidPathChars) < 0;
    }
}
