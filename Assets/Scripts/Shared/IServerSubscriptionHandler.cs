using Mirror;

namespace NetworkService.Shared
{
    public interface IServerSubscriptionHandler
    {
        void OnClientConnected(NetworkConnection conn, SubscriptionNetworkBehaviour behaviour);
        void OnClientDisconnected(NetworkConnection conn);
        void HandleSubscription(NetworkConnection conn, string messageTypeName, bool isSubscribing);
    }
}