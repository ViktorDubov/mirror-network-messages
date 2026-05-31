using Mirror;

namespace NetworkService.Shared
{
    public interface ISubscriptionService
    {
        void Subscribe<T>() where T : struct, NetworkMessage;
        void Unsubscribe<T>() where T : struct, NetworkMessage;
        bool IsSubscribed<T>() where T : struct, NetworkMessage;
    }
}