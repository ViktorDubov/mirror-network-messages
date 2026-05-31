namespace NetworkService.Shared
{
    public interface IClientSubscriptionCallbacks
    {
        void OnSubscriptionConfirmed(string messageTypeName);
        void OnNetworkBehaviourReady(SubscriptionNetworkBehaviour behaviour);
    }
}