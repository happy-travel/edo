using System.Threading.Tasks;

namespace HappyTravel.Edo.Api.NotificationCenter.Hubs
{
    public interface INotificationClient
    {
        Task ReceiveMessage(int messageId, string message);
    }
}