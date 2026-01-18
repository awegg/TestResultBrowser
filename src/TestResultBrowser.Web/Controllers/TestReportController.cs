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

            // Store the report directory for asset resolution
            AssetsController.SetReportDirectory(path);

            // Combine with index.html
            var reportPath = Path.Combine(path, "index.html");

            if (!System.IO.File.Exists(reportPath))
            {
                _logger.LogWarning("Report file not found: {Path}", reportPath);
                return NotFound($"Report file not found: {reportPath}");
            }

            // Read the HTML file
            var htmlContent = System.IO.File.ReadAllText(reportPath);

            // Return as HTML
            return Content(htmlContent, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving test report from {Path}", path);
            return StatusCode(500, $"Error loading report: {ex.Message}");
        }
    }
}
