using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TestResultBrowser.Web.Security;
using TestResultBrowser.Web.Services;

namespace TestResultBrowser.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly ILogger<AssetsController> _logger;
    private readonly TestResultBrowserOptions _options;
    private const string ReportDirectoryKey = "ReportDirectory";

    public AssetsController(ILogger<AssetsController> logger, IOptions<TestResultBrowserOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Stores the report directory in request context for asset resolution (request-scoped)
    /// </summary>
    public void SetReportDirectory(string directory)
    {
        HttpContext.Items[ReportDirectoryKey] = directory;
    }

    /// <summary>
    /// Serves assets (images, CSS, JS) from the test report directory
    /// </summary>
    [HttpGet("{**assetPath}")]
    public async Task<IActionResult> GetAsset(string assetPath, [FromQuery] string? reportPath)
    {
        try
        {
            // Get report directory from query parameter or request context (fallback)
            string? reportDirectory = reportPath;

            if (string.IsNullOrEmpty(reportDirectory))
            {
                if (!HttpContext.Items.TryGetValue(ReportDirectoryKey, out var reportDirObj) || reportDirObj is not string contextDirectory)
                {
                    _logger.LogWarning("No report directory provided for asset: {AssetPath}", assetPath);
                    return NotFound("Report directory not set");
                }
                reportDirectory = contextDirectory;
            }
            else
            {
                reportDirectory = Uri.UnescapeDataString(reportDirectory);
                // CRITICAL: Validate reportPath is under the allowed base directory
                var allowedBaseDir = Path.GetFullPath(_options.FileSharePath);
                var resolvedReportPath = Path.GetFullPath(Path.Combine(allowedBaseDir, reportDirectory));
                var allowedBaseDirWithSeparator = allowedBaseDir.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if (!resolvedReportPath.StartsWith(allowedBaseDirWithSeparator, StringComparison.OrdinalIgnoreCase) &&
                    !resolvedReportPath.Equals(allowedBaseDir, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Report path outside allowed base directory: {ReportPath}", reportPath);
                    return BadRequest(new { error = "Invalid report path" });
                }

                // Use the resolved report path for subsequent operations
                reportDirectory = resolvedReportPath;
            }

            // Validate assetPath to prevent path traversal attacks and invalid characters
            if (!PathValidator.IsPathSafe(assetPath))
            {
                _logger.LogWarning("Invalid asset path requested: {AssetPath}", assetPath);
                return BadRequest(new { error = "Invalid asset path" });
            }

            // Combine the report directory with the asset path and resolve to absolute path
            var fullPath = Path.Combine(reportDirectory, assetPath);
            var resolvedPath = Path.GetFullPath(fullPath);
            var resolvedReportDir = Path.GetFullPath(reportDirectory);

            // Ensure the resolved path is under the report directory
            if (!resolvedPath.StartsWith(resolvedReportDir, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {AssetPath}", assetPath);
                return BadRequest(new { error = "Invalid asset path" });
            }

            if (!System.IO.File.Exists(resolvedPath))
            {
                _logger.LogWarning("Asset file not found: {Path}", resolvedPath);
                return NotFound($"Asset not found: {assetPath}");
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(resolvedPath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".html" => "text/html",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };

            // Read and return the file
            var fileBytes = await System.IO.File.ReadAllBytesAsync(resolvedPath);
            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving asset: {AssetPath}", assetPath);
            return StatusCode(500, "Error loading asset");
        }
    }
}
