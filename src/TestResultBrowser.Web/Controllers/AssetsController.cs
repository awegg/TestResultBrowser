using Microsoft.AspNetCore.Mvc;

namespace TestResultBrowser.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly ILogger<AssetsController> _logger;
    private static string? _lastReportDirectory;

    public AssetsController(ILogger<AssetsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Stores the last accessed report directory for asset resolution
    /// </summary>
    public static void SetReportDirectory(string directory)
    {
        _lastReportDirectory = directory;
    }

    /// <summary>
    /// Serves assets (images, CSS, JS) from the test report directory
    /// </summary>
    [HttpGet("{**assetPath}")]
    public IActionResult GetAsset(string assetPath)
    {
        try
        {
            if (string.IsNullOrEmpty(_lastReportDirectory))
            {
                _logger.LogWarning("No report directory set for asset: {AssetPath}", assetPath);
                return NotFound("Report directory not set");
            }

            // Combine the report directory with the asset path
            var fullPath = Path.Combine(_lastReportDirectory, assetPath);

            if (!System.IO.File.Exists(fullPath))
            {
                _logger.LogWarning("Asset file not found: {Path}", fullPath);
                return NotFound($"Asset not found: {assetPath}");
            }

            // Determine content type based on file extension
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".html" => "text/html",
                ".json" => "application/json",
                _ => "application/octet-stream"
            };

            // Read and return the file
            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving asset: {AssetPath}", assetPath);
            return StatusCode(500, $"Error loading asset: {ex.Message}");
        }
    }
}
