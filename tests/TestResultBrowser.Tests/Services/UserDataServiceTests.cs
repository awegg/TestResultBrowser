using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using TestResultBrowser.Web.Models;
using TestResultBrowser.Web.Services;
using Xunit;

namespace TestResultBrowser.Tests.Services;

public class UserDataServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly Mock<ILogger<UserDataService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly UserDataService _service;

    public UserDataServiceTests()
    {
        // Create a temporary database file for each test
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
        
        _loggerMock = new Mock<ILogger<UserDataService>>();
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(c => c["TestResultBrowser:UserDataDatabasePath"]).Returns(_tempDbPath);
        
        _service = new UserDataService(_loggerMock.Object, _configurationMock.Object);
    }

    public void Dispose()
    {
        // Clean up the temporary database file after each test
        if (File.Exists(_tempDbPath))
        {
            try
            {
                File.Delete(_tempDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task SaveFilterAsync_ValidFilter_ShouldPersistAndReturnWithId()
    {
        // Arrange
        var filter = new SavedFilterConfiguration
        {
            Name = "Test Filter",
            SavedBy = "test_user",
            SavedDate = DateTime.UtcNow,
            Description = "Test description",
            Features = new List<string> { "Feature1", "Feature2" },
            Domains = new List<string> { "Domain1" },
            SelectedConfiguration = "Release",
            NumberOfBuilds = 5
        };

        // Act
        var result = await _service.SaveFilterAsync(filter);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBeGreaterThan(0);
        result.Name.ShouldBe("Test Filter");
        result.SavedBy.ShouldBe("test_user");
        result.Features.ShouldBe(filter.Features);
    }

    [Fact]
    public async Task LoadFilterAsync_ExistingFilter_ShouldReturnFilter()
    {
        // Arrange
        var filter = new SavedFilterConfiguration
        {
            Name = "Load Test Filter",
            SavedBy = "test_user",
            SavedDate = DateTime.UtcNow,
            Features = new List<string> { "Feature1" }
        };
        var saved = await _service.SaveFilterAsync(filter);

        // Act
        var loaded = await _service.LoadFilterAsync(saved.Id, "test_user");

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Id.ShouldBe(saved.Id);
        loaded.Name.ShouldBe("Load Test Filter");
        loaded.SavedBy.ShouldBe("test_user");
        loaded.Features.Count.ShouldBe(1);
        loaded.Features[0].ShouldBe("Feature1");
    }

    [Fact]
    public async Task LoadFilterAsync_NonExistentFilter_ShouldReturnNull()
    {
        // Act
        var loaded = await _service.LoadFilterAsync(999, "test_user");

        // Assert
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllFiltersAsync_MultipleFilters_ShouldReturnOnlyUserFilters()
    {
        // Arrange
        var user1Filters = new[]
        {
            new SavedFilterConfiguration { Name = "Filter 1", SavedBy = "user1", SavedDate = DateTime.UtcNow },
            new SavedFilterConfiguration { Name = "Filter 2", SavedBy = "user1", SavedDate = DateTime.UtcNow }
        };
        
        var user2Filter = new SavedFilterConfiguration 
        { 
            Name = "Filter 3", 
            SavedBy = "user2", 
            SavedDate = DateTime.UtcNow 
        };

        await _service.SaveFilterAsync(user1Filters[0]);
        await _service.SaveFilterAsync(user1Filters[1]);
        await _service.SaveFilterAsync(user2Filter);

        // Act
        var user1Results = await _service.GetAllFiltersAsync("user1");
        var user2Results = await _service.GetAllFiltersAsync("user2");

        // Assert
        user1Results.Count.ShouldBe(2);
        user1Results.ShouldAllBe(f => f.SavedBy == "user1");
        
        user2Results.Count.ShouldBe(1);
        user2Results[0].SavedBy.ShouldBe("user2");
        user2Results[0].Name.ShouldBe("Filter 3");
    }

    [Fact]
    public async Task GetAllFiltersAsync_NoFilters_ShouldReturnEmptyList()
    {
        // Act
        var results = await _service.GetAllFiltersAsync("nonexistent_user");

        // Assert
        results.ShouldNotBeNull();
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteFilterAsync_ExistingFilter_ShouldReturnTrueAndRemoveFilter()
    {
        // Arrange
        var filter = new SavedFilterConfiguration
        {
            Name = "To Delete",
            SavedBy = "test_user",
            SavedDate = DateTime.UtcNow
        };
        var saved = await _service.SaveFilterAsync(filter);

        // Act
        var deleted = await _service.DeleteFilterAsync(saved.Id);
        var loaded = await _service.LoadFilterAsync(saved.Id, "test_user");

        // Assert
        deleted.ShouldBeTrue();
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteFilterAsync_NonExistentFilter_ShouldReturnFalse()
    {
        // Act
        var deleted = await _service.DeleteFilterAsync(999);

        // Assert
        deleted.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateFilterAsync_ExistingFilter_ShouldModifyAndReturnTrue()
    {
        // Arrange
        var filter = new SavedFilterConfiguration
        {
            Name = "Original Name",
            SavedBy = "test_user",
            SavedDate = DateTime.UtcNow,
            Description = "Original description"
        };
        var saved = await _service.SaveFilterAsync(filter);

        saved.Name = "Updated Name";
        saved.Description = "Updated description";

        // Act
        var updated = await _service.UpdateFilterAsync(saved, "test_user");
        var loaded = await _service.LoadFilterAsync(saved.Id, "test_user");

        // Assert
        updated.ShouldBeTrue();
        loaded.ShouldNotBeNull();
        loaded.Name.ShouldBe("Updated Name");
        loaded.Description.ShouldBe("Updated description");
    }

    [Fact]
    public async Task UpdateFilterAsync_NonExistentFilter_ShouldReturnFalse()
    {
        // Arrange
        var filter = new SavedFilterConfiguration
        {
            Id = 999,
            Name = "Non-existent",
            SavedBy = "test_user",
            SavedDate = DateTime.UtcNow
        };

        // Act
        var updated = await _service.UpdateFilterAsync(filter, "test_user");

        // Assert
        updated.ShouldBeFalse();
    }

    [Fact]
    public async Task SaveFilterAsync_SetsTimestamp()
    {
        // Arrange
        var beforeSave = DateTime.UtcNow;
        var filter = new SavedFilterConfiguration
        {
            Name = "Timestamp Test",
            SavedBy = "test_user",
            SavedDate = DateTime.MinValue // Will be overwritten
        };

        // Act
        var saved = await _service.SaveFilterAsync(filter);
        var afterSave = DateTime.UtcNow;

        // Assert
        saved.SavedDate.ShouldBeGreaterThanOrEqualTo(beforeSave);
        saved.SavedDate.ShouldBeLessThanOrEqualTo(afterSave);
    }

    [Fact]
    public async Task SaveFilterAsync_PreservesComplexProperties()
    {
        // Arrange
        var filter = new SavedFilterConfiguration
        {
            Name = "Complex Filter",
            SavedBy = "test_user",
            SavedDate = DateTime.UtcNow,
            Features = new List<string> { "Feature1", "Feature2", "Feature3" },
            Domains = new List<string> { "Domain1", "Domain2" },
            Versions = new List<string> { "1.0.0", "2.0.0" },
            NamedConfigs = new List<string> { "Config1", "Config2" },
            SelectedConfiguration = "Debug",
            NumberOfBuilds = 10,
            DateFrom = DateTime.UtcNow.AddDays(-7),
            DateTo = DateTime.UtcNow,
            OnlyFailures = true,
            HideFlakyTests = false
        };

        // Act
        var saved = await _service.SaveFilterAsync(filter);
        var loaded = await _service.LoadFilterAsync(saved.Id, "test_user");

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Features.ShouldBe(filter.Features);
        loaded.Domains.ShouldBe(filter.Domains);
        loaded.Versions.ShouldBe(filter.Versions);
        loaded.NamedConfigs.ShouldBe(filter.NamedConfigs);
        loaded.SelectedConfiguration.ShouldBe("Debug");
        loaded.NumberOfBuilds.ShouldBe(10);
        loaded.DateFrom.ShouldNotBeNull();
        loaded.DateTo.ShouldNotBeNull();

        var expectedFromUtc = filter.DateFrom!.Value.ToUniversalTime();
        var expectedToUtc = filter.DateTo!.Value.ToUniversalTime();

        loaded.DateFrom.Value.ToUniversalTime().ShouldBe(expectedFromUtc, TimeSpan.FromSeconds(1));
        loaded.DateTo.Value.ToUniversalTime().ShouldBe(expectedToUtc, TimeSpan.FromSeconds(1));
        loaded.OnlyFailures.ShouldBe(true);
        loaded.HideFlakyTests.ShouldBe(false);
    }

    [Fact]
    public async Task GetDashboardConfigAsync_ExistingConfig_ShouldReturnConfig()
    {
        // Arrange
        var config = new DashboardConfiguration
        {
            Username = "test_user",
            DefaultBuildCount = 5,
            Theme = "Dark"
        };
        await _service.SaveDashboardConfigAsync(config);

        // Act
        var loaded = await _service.GetDashboardConfigAsync("test_user");

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Username.ShouldBe("test_user");
        loaded.DefaultBuildCount.ShouldBe(5);
        loaded.Theme.ShouldBe("Dark");
    }

    [Fact]
    public async Task GetDashboardConfigAsync_NonExistentConfig_ShouldReturnNull()
    {
        // Act
        var loaded = await _service.GetDashboardConfigAsync("nonexistent_user");

        // Assert
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task SaveDashboardConfigAsync_NewConfig_ShouldPersist()
    {
        // Arrange
        var config = new DashboardConfiguration
        {
            Username = "new_user",
            DefaultBuildCount = 10,
            Theme = "Light",
            ShowFlakyIndicators = false
        };

        // Act
        await _service.SaveDashboardConfigAsync(config);
        var loaded = await _service.GetDashboardConfigAsync("new_user");

        // Assert
        loaded.ShouldNotBeNull();
        loaded.Username.ShouldBe("new_user");
        loaded.DefaultBuildCount.ShouldBe(10);
        loaded.Theme.ShouldBe("Light");
        loaded.ShowFlakyIndicators.ShouldBe(false);
    }

    [Fact]
    public async Task SaveDashboardConfigAsync_UpdateExisting_ShouldOverwrite()
    {
        // Arrange
        var config1 = new DashboardConfiguration
        {
            Username = "update_user",
            DefaultBuildCount = 5
        };
        await _service.SaveDashboardConfigAsync(config1);

        var config2 = new DashboardConfiguration
        {
            Username = "update_user",
            DefaultBuildCount = 15
        };

        // Act
        await _service.SaveDashboardConfigAsync(config2);
        var loaded = await _service.GetDashboardConfigAsync("update_user");

        // Assert
        loaded.ShouldNotBeNull();
        loaded.DefaultBuildCount.ShouldBe(15);
    }
}
