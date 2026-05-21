namespace Dao.Aspire.Angular.Api.Hubs;

public interface INotificationHubClient
{
    Task ReceiveNotification(string message);
}
