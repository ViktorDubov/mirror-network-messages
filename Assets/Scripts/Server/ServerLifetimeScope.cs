using VContainer;
using VContainer.Unity;
using UnityEngine;
using NetworkService.Shared;

namespace NetworkService.Server
{
    public class ServerLifetimeScope : LifetimeScope
    {
        [SerializeField] private SubscriptionServiceServer _subscriptionService;
        [SerializeField] private MessageRouterServer _messageRouter;

        protected override void Configure(IContainerBuilder builder)
        {
            if (_subscriptionService == null)
            {
                Debug.LogError("[ServerLifetimeScope] SubscriptionServiceServer not assigned in inspector");
                return;
            }

            builder.RegisterComponent(_subscriptionService)
                .As<IServerSubscriptionHandler>()
                .AsSelf();

            if (_messageRouter == null)
            {
                Debug.LogError("[ServerLifetimeScope] MessageRouterServer not assigned in inspector");
                return;
            }

            builder.RegisterComponent(_messageRouter).AsSelf();
        }
    }
}