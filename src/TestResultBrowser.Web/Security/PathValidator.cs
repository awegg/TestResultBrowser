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
