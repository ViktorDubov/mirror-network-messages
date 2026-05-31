using UnityEngine;
using NetworkMessages;
using Mirror;
using VContainer;
using System;

namespace NetworkService.Server
{
    public class MessageRouterServer : MonoBehaviour
    {
        private SubscriptionServiceServer _subscriptionService;

        [Inject]
        public void Construct(SubscriptionServiceServer subscriptionService)
        {
            if (subscriptionService == null)
                throw new ArgumentNullException(nameof(subscriptionService));

            _subscriptionService = subscriptionService;
            _subscriptionService.MessageRouter = this;
        }

        public void SendHelloToAllSubscribers()
        {
            if (_subscriptionService == null)
            {
                Debug.LogError("[MessageRouterServer] SubscriptionService not initialized");
                return;
            }

            if (!NetworkServer.active)
            {
                Debug.LogWarning("[MessageRouterServer] Server not active, cannot send");
                return;
            }

            var message = new HelloMessage { Text = "Hello Client!" };
            _subscriptionService.SendToSubscribers(message);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S) && NetworkServer.active)
            {
                Debug.Log("Server sends Hello");
                SendHelloToAllSubscribers();
            }
        }
    }
}