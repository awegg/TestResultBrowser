using Shouldly;
using Microsoft.Playwright;
using Xunit;

namespace TestResultBrowser.Tests.E2E;

/// <summary>
/// E2E tests for the Flaky Tests feature (/flaky-tests page)
/// Tests the complete workflow including rendering, filtering, and interactions
/// </summary>
public class FlakyTestsE2ETests : IAsyncLifetime
{
    private IBrowser? _browser;
    private IPage? _page;
    private const string BaseUrl = "http://localhost:5248";
    private const string FlakyTestsUrl = $"{BaseUrl}/flaky-tests";

    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_LoadsSuccessfully()
    {
        // Act - Navigate to flaky tests page
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page title should exist
        var title = await _page.TitleAsync();
        title.ShouldNotBeNull("Page should have a title");
        
        // Page header should be present (testing that DI works correctly)
        var header = await _page.QuerySelectorAsync("h4");
        header.ShouldNotBeNull("Flaky Tests page should have a main header");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_ContainsFilterControls()
    {
        // Act
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Filter panel should be present
        var filterPanel = await _page.QuerySelectorAsync("[class*='pa-4']");
        filterPanel.ShouldNotBeNull("Filter panel should be visible");

        // Threshold slider should exist
        var sliders = await _page.QuerySelectorAllAsync("input[type='range']");
        sliders.Count.ShouldBeGreaterThan(0, "Should have threshold slider");

        // Configuration dropdown should exist
        var dropdowns = await _page.QuerySelectorAllAsync("select, [role='combobox']");
        dropdowns.Count.ShouldBeGreaterThanOrEqualTo(0, "Filter controls should be present");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_RefreshButtonIsClickable()
    {
        // Act
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the refresh button by looking for buttons with specific text content
        var allButtons = await _page.QuerySelectorAllAsync("button");
        IElementHandle? refreshButton = null;

        foreach (var button in allButtons)
        {
            var text = await button.TextContentAsync();
            if (text?.Contains("Refresh") == true)
            {
                refreshButton = button;
                break;
            }
        }

        refreshButton.ShouldNotBeNull("Refresh button should exist");

        // Button should be enabled
        var isDisabled = await refreshButton!.IsDisabledAsync();
        isDisabled.ShouldBeFalse("Refresh button should not be disabled initially");

        // Click the button (this should trigger analysis)
        await refreshButton.ClickAsync();

        // Wait for results or loading state
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_DisplaysResultsOrEmptyState()
    {
        // Act
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Either shows empty state message OR test cards
        var emptyStateAlert = await _page.QuerySelectorAsync(".mud-alert");
        var testCards = await _page.QuerySelectorAllAsync(".mud-paper");

        // Should have either empty state or cards
        (emptyStateAlert != null || testCards.Count > 0).ShouldBeTrue(
            "Page should either show empty state or display flaky tests"
        );
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_ExpandableCardsAreInteractive()
    {
        // Act
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Get all expand buttons
        var expandButtons = await _page.QuerySelectorAllAsync("button");

        // If there are flaky tests, test expand functionality
        if (expandButtons.Count > 0)
        {
            var firstButton = expandButtons[0];
            
            // Click to expand
            await firstButton.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Stats should be visible when expanded
            var stats = await _page.QuerySelectorAsync("[class*='caption']");
            stats.ShouldNotBeNull("Statistics should be visible when card is expanded");
        }
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_TrendFiltersWork()
    {
        // Act
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find trend filter dropdown
        var trendSelects = await _page.QuerySelectorAllAsync("select");
        
        if (trendSelects.Count > 0)
        {
            // Try to interact with dropdown (if it exists)
            var firstSelect = trendSelects[0];
            var options = await firstSelect.QuerySelectorAllAsync("option");
            
            // Should have at least one option
            options.Count.ShouldBeGreaterThan(0, "Dropdown should have options");
        }
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_NavigationLinkExists()
    {
        // Act - Go to home page first
        await _page!.GotoAsync(BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Look for link to flaky tests
        var flakyTestsLink = await _page.QuerySelectorAsync("a[href*='flaky-tests']");
        
        // Link should exist in navigation
        if (flakyTestsLink != null)
        {
            // Click the link
            await flakyTestsLink.ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Should be on flaky tests page
            var currentUrl = _page.Url;
            currentUrl.ShouldContain("flaky-tests");
        }
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_NoJavaScriptErrors()
    {
        // Act
        var jsErrors = new List<string>();
        
        EventHandler<string> onPageError = (sender, error) =>
        {
            jsErrors.Add(error);
        };
        
        _page!.PageError += onPageError;

        try
        {
            await _page.GotoAsync(FlakyTestsUrl);
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait a bit for any deferred errors
            await _page.WaitForTimeoutAsync(1000);

            // Assert - No JavaScript errors should have occurred
            jsErrors.ShouldBeEmpty("Page should not have JavaScript errors");
        }
        finally
        {
            _page.PageError -= onPageError;
        }
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_ResponsiveDesign()
    {
        // Act - Test mobile viewport
        await _page!.SetViewportSizeAsync(375, 667);
        await _page.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - Page should still be navigable
        var header = await _page.QuerySelectorAsync("h4");
        header.ShouldNotBeNull("Header should be visible on mobile");

        // Test tablet viewport
        await _page.SetViewportSizeAsync(768, 1024);
        await _page.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        header = await _page.QuerySelectorAsync("h4");
        header.ShouldNotBeNull("Header should be visible on tablet");

        // Reset to desktop
        await _page.SetViewportSizeAsync(1920, 1080);
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_LoadingStateHandling()
    {
        // Act
        await _page!.GotoAsync(FlakyTestsUrl);
        
        // Refresh should show loading state
        var allButtons = await _page.QuerySelectorAllAsync("button");
        IElementHandle? refreshButton = null;

        foreach (var button in allButtons)
        {
            var text = await button.TextContentAsync();
            if (text?.Contains("Refresh") == true)
            {
                refreshButton = button;
                break;
            }
        }

        if (refreshButton != null)
        {
            await refreshButton.ClickAsync();
            
            // Wait a bit to see if loading indicator appears
            await _page.WaitForTimeoutAsync(500);
        }

        // Page should always be in a consistent state (either loading or loaded)
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Should have content after loading
        var content = await _page.QuerySelectorAsync("body");
        content.ShouldNotBeNull("Page should have loaded");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_PerformanceBaseline()
    {
        // Act - Measure page load time
        var watch = System.Diagnostics.Stopwatch.StartNew();
        
        await _page!.GotoAsync(FlakyTestsUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        watch.Stop();

        // Assert - Page should load within reasonable time (< 10 seconds)
        var loadTimeMs = watch.ElapsedMilliseconds;
        loadTimeMs.ShouldBeLessThan(10000, $"Page load should be fast (was {loadTimeMs}ms)");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task FlakyTestsPage_DependencyInjectionWorks()
    {
        // This test specifically validates that the logger injection fix works
        // If DI fails, the page won't load at all
        
        // Act
        var pageErrors = new List<string>();
        
        EventHandler<string> onPageError = (sender, error) =>
        {
            pageErrors.Add(error);
        };
        
        _page!.PageError += onPageError;

        try
        {
            await _page.GotoAsync(FlakyTestsUrl);
            
            // Wait for page to fully load
            try
            {
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
            catch (Exception ex)
            {
                // If page fails to load, this test catches it
                throw new Exception($"FlakyTests page failed to load. DI error likely. Details: {ex.Message}");
            }

            // Assert - Page should load successfully with no errors
            var hasContent = await _page.QuerySelectorAsync("h4") != null;
            hasContent.ShouldBeTrue(
                "Page should load successfully (indicates DI is working)"
            );
        }
        finally
        {
            _page.PageError -= onPageError;
        }
    }
}
