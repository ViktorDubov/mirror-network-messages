using VContainer;
using VContainer.Unity;
using UnityEngine;
using NetworkService.Shared;

namespace NetworkService.Client
{
    public class ClientLifetimeScope : LifetimeScope
    {
        [SerializeField] private SubscriptionServiceClient _subscriptionService;
        [SerializeField] private MessageHandlerClient _messageHandler;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_subscriptionService == null)
            {
                Debug.LogError("[ClientLifetimeScope] SubscriptionServiceClient not assigned in inspector");
                return;
            }

            builder.RegisterComponent(_subscriptionService)
                .As<IClientSubscriptionCallbacks>()
                .As<ISubscriptionService>()
                .AsSelf();

            if (_messageHandler == null)
            {
                Debug.LogError("[ClientLifetimeScope] MessageHandlerClient not assigned in inspector");
                return;
            }

            builder.RegisterComponent(_messageHandler).AsSelf();
        }
    }
}