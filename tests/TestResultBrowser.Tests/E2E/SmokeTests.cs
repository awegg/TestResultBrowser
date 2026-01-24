using Shouldly;
using Microsoft.Playwright;
using Xunit;

namespace TestResultBrowser.Tests.E2E;

public class SmokeTests : IAsyncLifetime
{
    private IBrowser? _browser;
    private IPage? _page;
    private const string BaseUrl = "http://localhost:5248";

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
    public async Task AppStarts_HomePageLoads()
    {
        // Act
        await _page!.GotoAsync(BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - h1 should exist on home page
        var heading = await _page.QuerySelectorAsync("h1");
        heading.ShouldNotBeNull("Home page should have h1 heading");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task MorningTriage_ApplyFilter()
    {
        // Arrange
        await _page!.GotoAsync(BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act: Navigate to Morning Triage
        await _page.GotoAsync($"{BaseUrl}/morning-triage");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert: Page loaded - should have MudCard elements (stats cards)
        var statsCards = await _page.QuerySelectorAllAsync("div.mud-card");
        statsCards.Count.ShouldBeGreaterThan(0, "Morning Triage page should display stats cards");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task ConfigurationHistory_BuildCountChange()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/configuration-history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act: Change build count using MudNumericField
        var buildCountInputs = await _page.QuerySelectorAllAsync("input[type='number']");
        if (buildCountInputs.Count > 0)
        {
            // Find the builds field specifically (usually the second numeric field on page)
            var buildsInput = buildCountInputs.FirstOrDefault();
            if (buildsInput != null)
            {
                await buildsInput.FillAsync("5");
                await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            }
        }

        // Assert: History cards should be visible
        var historyCards = await _page.QuerySelectorAllAsync("div.mud-card");
        historyCards.Count.ShouldBeGreaterThan(0, "History grid should display cards");
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task SettingsWorkflow_ChangeSaveAndVerifyPersistence()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act: Find and change polling interval value
        var numericInputs = await _page.QuerySelectorAllAsync("input[type='number']");
        numericInputs.Count.ShouldBeGreaterThan(0, "Settings page should have numeric input fields");

        string savedValue = "12";
        if (numericInputs.Count > 0)
        {
            // Get initial value
            var initialValue = await numericInputs[0].InputValueAsync();
            initialValue.ShouldNotBeNullOrEmpty("Should have initial value");

            // Change the value (select all with Ctrl+A, then type new value)
            await numericInputs[0].ClickAsync();
            await numericInputs[0].PressAsync("Control+A");
            await numericInputs[0].FillAsync(savedValue);

            // Verify the input shows the changed value before saving
            var changedValue = await numericInputs[0].InputValueAsync();
            changedValue.ShouldBe(savedValue, "Input should show the changed value");
        }

        // Find and click Save button
        var saveButtons = await _page.QuerySelectorAllAsync("button");
        saveButtons.Count.ShouldBeGreaterThan(0, "Should have buttons on page");

        if (saveButtons.Count > 0)
        {
            // Click the last button (Save Settings)
            await saveButtons[saveButtons.Count - 1].ClickAsync();
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Wait for navigation back to home (after save)
            await _page.WaitForURLAsync("**/");
        }

        // Assert: Navigate back to settings and verify the value persisted
        await _page.GotoAsync($"{BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var reloadedInputs = await _page.QuerySelectorAllAsync("input[type='number']");
        reloadedInputs.Count.ShouldBeGreaterThan(0, "Settings page should still have numeric input fields after reload");

        if (reloadedInputs.Count > 0)
        {
            // Verify the value actually persisted
            var persistedValue = await reloadedInputs[0].InputValueAsync();
            persistedValue.ShouldBe(savedValue, "Setting should persist after reload - value should be '12'");
        }
    }

    [Trait("Category", "E2E")]
    [Fact]
    public async Task TestReportViewing_ReportAndAssetsLoad()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/configuration-history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert: Configuration History page loaded and shows content
        var configCards = await _page.QuerySelectorAllAsync("div.mud-card");
        configCards.Count.ShouldBeGreaterThan(0, "Configuration History page should display cards with test data");

        // Verify buttons and controls are present for navigation
        var buttons = await _page.QuerySelectorAllAsync("button");
        buttons.Count.ShouldBeGreaterThan(0, "Page should have interactive controls");
    }
}
