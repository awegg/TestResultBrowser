using Microsoft.AspNetCore.SignalR;

namespace TestResultBrowser.Web.Hubs;

/// <summary>
/// SignalR hub for broadcasting real-time test data updates to connected clients
/// </summary>
public class TestDataHub : Hub
{
    private readonly ILogger<TestDataHub> _logger;

    public TestDataHub(ILogger<TestDataHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to TestDataHub: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from TestDataHub: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
