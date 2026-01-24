using Microsoft.AspNetCore.Mvc;

namespace TestResultBrowser.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestReportController : ControllerBase
{
    private readonly ILogger<TestReportController> _logger;

    public TestReportController(ILogger<TestReportController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Serves the test report HTML file from the file system
    /// </summary>
    [HttpGet]
    public IActionResult GetReport([FromQuery] string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
            {
                return BadRequest("Path parameter is required");
            }

            // Validate path to prevent traversal attacks
            if (path.Contains("..") || Path.IsPathRooted(path))
            {
                _logger.LogWarning("Invalid path requested: {Path}", path);
                return BadRequest("Invalid path");
            }

            // Resolve the path to absolute and validate it exists and is safe
            var resolvedPath = Path.GetFullPath(path);

            // Combine with index.html
            var reportPath = Path.Combine(resolvedPath, "index.html");
            var resolvedReportPath = Path.GetFullPath(reportPath);

            // Verify the report file is under the requested directory
            if (!resolvedReportPath.StartsWith(resolvedPath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {Path}", path);
                return BadRequest("Invalid path");
            }

            if (!System.IO.File.Exists(resolvedReportPath))
            {
                _logger.LogWarning("Report file not found: {Path}", resolvedReportPath);
                return NotFound("Report file not found");
            }

            // Read the HTML file
            var htmlContent = System.IO.File.ReadAllText(resolvedReportPath);

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
