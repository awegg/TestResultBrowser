using TestResultBrowser.Web.Models;

namespace TestResultBrowser.Web.Services;

/// <summary>
/// Service for managing user-configurable application settings
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current application settings (from userdata or defaults)
    /// </summary>
    ApplicationSettings GetSettings();

    /// <summary>
    /// Saves application settings to userdata
    /// </summary>
    Task SaveSettingsAsync(ApplicationSettings settings);

    /// <summary>
    /// Resets settings to defaults
    /// </summary>
    Task ResetToDefaultsAsync();

    /// <summary>
    /// Event raised when settings are changed
    /// </summary>
    event EventHandler? SettingsChanged;
}
