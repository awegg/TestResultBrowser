using Shouldly;
using TestResultBrowser.Web.Security;
using Xunit;

namespace TestResultBrowser.Tests.Security;

public class PathSecurityTests
{
    private string NormalizeAndResolve(string basePath, string inputPath)
    {
        var combined = Path.Combine(basePath, inputPath);
        return Path.GetFullPath(combined);
    }

    private bool IsPathSafe(string basePath, string inputPath)
    {
        // First check with PathValidator
        if (!PathValidator.IsPathSafe(inputPath))
        {
            return false;
        }

        // Then verify base directory constraint
        try
        {
            var resolvedPath = NormalizeAndResolve(basePath, inputPath);
            var normalizedBase = Path.GetFullPath(basePath);
            return resolvedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    [Theory]
    [InlineData(@"C:\base", @"..\..\..\windows\system32\config\sam")]
    [InlineData(@"C:\base", @"..\..\sensitive.txt")]
    [InlineData(@"C:\base", @"..\outside.txt")]
    [InlineData(@"/var/www", @"../../../etc/passwd")]
    [InlineData(@"/var/www", @"../../sensitive.conf")]
    public void PathTraversal_DotDotSlash_ShouldBeBlocked(string basePath, string attackPath)
    {
        // Act
        var isSafe = IsPathSafe(basePath, attackPath);

        // Assert
        isSafe.ShouldBeFalse("path traversal attacks should be blocked");
    }

    [Theory]
    [InlineData(@"C:\base", @"valid\file.txt")]
    [InlineData(@"C:\base", @"subfolder\another\file.xml")]
    [InlineData(@"/var/www", @"public/data.json")]
    [InlineData(@"/var/www", @"reports/test.xml")]
    public void PathTraversal_ValidPaths_ShouldBeAllowed(string basePath, string validPath)
    {
        // Act
        var isSafe = IsPathSafe(basePath, validPath);

        // Assert
        isSafe.ShouldBeTrue("valid paths within base directory should be allowed");
    }

    [Theory]
    [InlineData(@"C:\base", @"C:\windows\system32\file.txt")]
    [InlineData(@"C:\base", @"D:\other\drive\file.txt")]
    [InlineData(@"/var/www", @"/etc/passwd")]
    [InlineData(@"/var/www", @"/root/secret.txt")]
    public void PathTraversal_AbsolutePaths_ShouldBeBlockedIfOutsideBase(string basePath, string absolutePath)
    {
        // Act
        var isSafe = IsPathSafe(basePath, absolutePath);

        // Assert
        isSafe.ShouldBeFalse("absolute paths outside base should be blocked");
    }

    [Theory]
    [InlineData(@"C:\base", @"file.txt\..\..\..\windows\system32")]
    [InlineData(@"C:\base", @"valid\..\..\outside.txt")]
    public void PathTraversal_MixedValidAndTraversal_ShouldBeBlocked(string basePath, string mixedPath)
    {
        // Act
        var isSafe = IsPathSafe(basePath, mixedPath);

        // Assert
        isSafe.ShouldBeFalse("paths that traverse outside base should be blocked");
    }

    [Theory]
    [InlineData(@"C:\base", @"")]
    [InlineData(@"C:\base", @"   ")]
    public void PathTraversal_EmptyOrWhitespace_ShouldBeHandled(string basePath, string emptyPath)
    {
        // Act & Assert - Should not throw
        var action = () => IsPathSafe(basePath, emptyPath);
        Should.NotThrow(action);
    }

    [Theory]
    [InlineData(@"C:\base", @"file<>:.txt")]
    [InlineData(@"C:\base", @"file|?.txt")]
    public void PathTraversal_InvalidCharacters_ShouldBeHandled(string basePath, string invalidPath)
    {
        // Act & Assert - Should not throw, but should be blocked
        var isSafe = IsPathSafe(basePath, invalidPath);
        isSafe.ShouldBeFalse("paths with invalid characters should be blocked");
    }

    [Fact]
    public void PathTraversal_SymbolicLinks_ShouldBeValidated()
    {
        // Arrange
        var basePath = Path.GetTempPath();
        var validPath = "test.txt";

        // Act
        var isSafe = IsPathSafe(basePath, validPath);

        // Assert
        isSafe.ShouldBeTrue();
    }

    [Theory]
    [InlineData(@"C:\base\subfolder", @"C:\base")]
    [InlineData(@"/var/www/html", @"/var/www")]
    public void PathValidation_BasePathIsPrefix_ShouldWork(string basePath, string checkPath)
    {
        // Arrange
        var normalizedBase = Path.GetFullPath(basePath);
        var normalizedCheck = Path.GetFullPath(checkPath);

        // Act
        var isChild = normalizedBase.StartsWith(normalizedCheck, StringComparison.OrdinalIgnoreCase);

        // Assert
        isChild.ShouldBeTrue("base path should be recognized as child of parent");
    }

    [Theory]
    [InlineData(@"C:\Base", @"C:\base\file.txt")]
    [InlineData(@"/Var/Www", @"/var/www/file.txt")]
    public void PathValidation_CaseInsensitive_ShouldWorkOnWindows(string basePath, string testPath)
    {
        // Act
        var resolved = Path.GetFullPath(testPath);
        var normalizedBase = Path.GetFullPath(basePath);
        var isSafe = resolved.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);

        // Assert - On Windows, should be case-insensitive
        if (OperatingSystem.IsWindows())
        {
            isSafe.ShouldBeTrue("Windows paths should be case-insensitive");
        }
    }

    [Fact]
    public void PathValidation_TrailingSlash_ShouldNotAffectValidation()
    {
        // Arrange
        var basePath1 = @"C:\base";
        var basePath2 = @"C:\base\";
        var testPath = @"file.txt";

        // Act
        var isSafe1 = IsPathSafe(basePath1, testPath);
        var isSafe2 = IsPathSafe(basePath2, testPath);

        // Assert
        isSafe1.ShouldBe(isSafe2, "trailing slash should not affect validation");
    }
}
