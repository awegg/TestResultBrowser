using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Security;
using TestResultBrowser.Web.Services;

namespace TestResultBrowser.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestReportController : ControllerBase
{
    private readonly ILogger<TestReportController> _logger;
    private readonly TestResultBrowserOptions _options;

    public TestReportController(ILogger<TestReportController> logger, IOptions<TestResultBrowserOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Serves the test report HTML file from the file system
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReport([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return BadRequest(new { error = "Path parameter is required" });
            }

            // Validate path to prevent traversal attacks and invalid characters
            if (!PathValidator.IsPathSafe(path))
            {
                _logger.LogWarning("Invalid path requested: {Path}", path);
                return BadRequest(new { error = "Invalid file path" });
            }

            // Resolve the path relative to FileSharePath and validate it exists and is safe
            var allowedBaseDir = Path.GetFullPath(_options.FileSharePath);
            var resolvedPath = Path.GetFullPath(Path.Combine(allowedBaseDir, path));

            // CRITICAL: Ensure resolved path is under the allowed base directory
            if (!resolvedPath.StartsWith(allowedBaseDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path outside allowed base directory: {Path}", path);
                return BadRequest(new { error = "Invalid file path" });
            }

            // Combine with index.html
            var reportPath = Path.Combine(resolvedPath, "index.html");
            var resolvedReportPath = Path.GetFullPath(reportPath);

            // Verify the report file is under the requested directory
            if (!resolvedReportPath.StartsWith(resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {Path}", path);
                return BadRequest(new { error = "Invalid file path" });
            }

            if (!System.IO.File.Exists(resolvedReportPath))
            {
                _logger.LogWarning("Report file not found: {Path}", resolvedReportPath);
                return NotFound("Report file not found");
            }

            // Read the HTML file
            var htmlContent = await System.IO.File.ReadAllTextAsync(resolvedReportPath);

            // Rewrite asset URLs to include reportPath parameter for asset resolution
            // This allows the /api/assets endpoint to access the correct directory
            var reportPathParam = Uri.EscapeDataString(resolvedPath);
            var rewrittenContent = System.Text.RegularExpressions.Regex.Replace(
                htmlContent,
                @"(src|href)=""([^""]+)""",
                match =>
                {
                    var attrName = match.Groups[1].Value;
                    var assetUrl = match.Groups[2].Value;

                    // Skip fragments, absolute URLs, and root-relative paths
                    if (assetUrl.StartsWith("#") || assetUrl.StartsWith("http") || assetUrl.StartsWith("/"))
                    {
                        return match.Value;
                    }

                    // Only rewrite relative paths (asset files)
                    // Preserve existing query string and fragment
                    var separator = assetUrl.Contains("?") ? "&" : "?";
                    return $"{attrName}=\"/api/assets/{assetUrl}{separator}reportPath={reportPathParam}\"";
                });

            // Return as HTML
            return Content(rewrittenContent, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving test report");
            return StatusCode(500, "Error loading report");
        }
    }
}
