using Microsoft.AspNetCore.SignalR;

namespace Dao.Aspire.Angular.Api.Hubs;

public partial class NotificationHub(ILogger<NotificationHub> logger) : Hub<INotificationHubClient>
{
    public override async Task OnConnectedAsync()
    {
        LogClientConnected(logger, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        LogClientDisconnected(logger, Context.ConnectionId, exception?.Message ?? "none");
        await base.OnDisconnectedAsync(exception);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Client {ConnectionId} connected to NotificationHub")]
    private static partial void LogClientConnected(ILogger logger, string connectionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Client {ConnectionId} disconnected. Exception: {Exception}")]
    private static partial void LogClientDisconnected(ILogger logger, string connectionId, string exception);
}
