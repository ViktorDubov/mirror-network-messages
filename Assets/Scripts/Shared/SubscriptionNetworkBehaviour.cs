using Mirror;
using UnityEngine;
using VContainer;
using Cysharp.Threading.Tasks;
using System;

namespace NetworkService.Shared
{
    public class SubscriptionNetworkBehaviour : NetworkBehaviour
    {
        private IClientSubscriptionCallbacks _clientCallbacks;
        private IServerSubscriptionHandler _serverHandler;
        private static NetworkSpawnMediator _mediator;

        private bool _clientInitialized;
        private bool _serverInitialized;
        private UniTaskCompletionSource _serverReadyTcs;
        private NetworkConnection _cachedConnection;

        public void SetClientCallbacks(IClientSubscriptionCallbacks clientCallbacks)
        {
            if (clientCallbacks == null)
                throw new ArgumentNullException(nameof(clientCallbacks));
            _clientCallbacks = clientCallbacks;
        }

        public void SetServerHandler(IServerSubscriptionHandler serverHandler)
        {
            if (serverHandler == null)
                throw new ArgumentNullException(nameof(serverHandler));
            _serverHandler = serverHandler;
        }

        public static void SetMediator(NetworkSpawnMediator mediator)
        {
            _mediator = mediator;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (!isLocalPlayer && !authority)
                return;

            if (_clientInitialized)
                return;

            _clientInitialized = true;

            if (_mediator != null)
            {
                _mediator.OnBehaviourStartClient(this);
            }

            if (_clientCallbacks != null)
            {
                _clientCallbacks.OnNetworkBehaviourReady(this);
            }
            else
            {
                Debug.LogError("[SubscriptionNetworkBehaviour] Client callbacks not set");
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            _clientCallbacks = null;
            _clientInitialized = false;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (_serverInitialized)
                return;

            _serverInitialized = true;
            _serverReadyTcs = new UniTaskCompletionSource();
            _cachedConnection = connectionToClient;

            if (_mediator != null)
            {
                _mediator.OnBehaviourStartServer(this);
            }

            WaitForServerInitializationAsync().Forget();
        }

        private async UniTaskVoid WaitForServerInitializationAsync()
        {
            try
            {
                await UniTask.WaitUntil(
                    () => (connectionToClient != null || _cachedConnection != null) && _serverHandler != null,
                    cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (OperationCanceledException)
            {
                return;
            }

            var conn = connectionToClient ?? _cachedConnection;

            if (conn == null || _serverHandler == null)
                return;

            _serverHandler.OnClientConnected(conn, this);
            _serverReadyTcs.TrySetResult();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            var conn = connectionToClient ?? _cachedConnection;

            if (conn != null && _serverHandler != null)
                _serverHandler.OnClientDisconnected(conn);

            _serverInitialized = false;
            _cachedConnection = null;
            _serverReadyTcs.TrySetCanceled();
            _serverReadyTcs = null;
        }

        [Command]
        public void CmdSubscribe(string messageTypeName)
        {
            if (string.IsNullOrEmpty(messageTypeName))
            {
                Debug.LogWarning("[SubscriptionNetworkBehaviour] CmdSubscribe rejected: empty type name");
                return;
            }

            ProcessSubscriptionAsync(messageTypeName, isSubscribing: true).Forget();
        }

        [Command]
        public void CmdUnsubscribe(string messageTypeName)
        {
            if (string.IsNullOrEmpty(messageTypeName))
            {
                Debug.LogWarning("[SubscriptionNetworkBehaviour] CmdUnsubscribe rejected: empty type name");
                return;
            }

            ProcessSubscriptionAsync(messageTypeName, isSubscribing: false).Forget();
        }

        private async UniTaskVoid ProcessSubscriptionAsync(string messageTypeName, bool isSubscribing)
        {
            if (_serverReadyTcs == null)
            {
                Debug.LogWarning($"[SubscriptionNetworkBehaviour] Subscription {messageTypeName} rejected: server not initialized");
                return;
            }

            try
            {
                await _serverReadyTcs.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[SubscriptionNetworkBehaviour] Subscription {messageTypeName} cancelled");
                return;
            }

            var conn = connectionToClient ?? _cachedConnection;

            if (conn == null)
            {
                Debug.LogWarning($"[SubscriptionNetworkBehaviour] Subscription {messageTypeName} rejected: no connection");
                return;
            }

            if (_serverHandler == null)
            {
                Debug.LogWarning($"[SubscriptionNetworkBehaviour] Subscription {messageTypeName} rejected: no handler");
                return;
            }

            _serverHandler.HandleSubscription(conn, messageTypeName, isSubscribing);
        }

        [TargetRpc]
        public void TargetSubscriptionConfirmed(string messageTypeName)
        {
            if (_clientCallbacks == null)
            {
                Debug.LogWarning($"[SubscriptionNetworkBehaviour] Cannot confirm {messageTypeName}: callbacks not set");
                return;
            }

            _clientCallbacks.OnSubscriptionConfirmed(messageTypeName);
        }
    }
}