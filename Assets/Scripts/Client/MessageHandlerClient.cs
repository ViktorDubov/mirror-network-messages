using Mirror;
using UnityEngine;
using NetworkMessages;
using NetworkService.Shared;
using VContainer;
using System;

namespace NetworkService.Client
{
    public class MessageHandlerClient : MonoBehaviour
    {
        private ISubscriptionService _subscriptionService;
        private bool _handlerRegistered;

        [Inject]
        public void Construct(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        }

        private void OnEnable()
        {
            NetworkClient.OnConnectedEvent += OnClientConnected;
            NetworkClient.OnDisconnectedEvent += OnClientDisconnected;

            if (NetworkClient.isConnected)
            {
                OnClientConnected();
            }
        }

        private void OnDisable()
        {
            NetworkClient.OnConnectedEvent -= OnClientConnected;
            NetworkClient.OnDisconnectedEvent -= OnClientDisconnected;
        }

        private void OnClientConnected()
        {
            if (!_handlerRegistered)
            {
                NetworkClient.ReplaceHandler<HelloMessage>(OnHelloMessage);
                _handlerRegistered = true;
            }

            _subscriptionService?.Subscribe<HelloMessage>();
        }

        private void OnClientDisconnected()
        {
            if (_handlerRegistered)
            {
                NetworkClient.UnregisterHandler<HelloMessage>();
                _handlerRegistered = false;
            }
        }

        private void OnHelloMessage(HelloMessage message)
        {
            if (_subscriptionService == null || !_subscriptionService.IsSubscribed<HelloMessage>())
            {
                Debug.LogWarning("[MessageHandlerClient] Received HelloMessage but not subscribed — ignoring");
                return;
            }

            Debug.Log($"[MessageHandlerClient] Received: {message.Text}");
        }
    }
}