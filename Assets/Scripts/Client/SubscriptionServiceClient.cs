using System.Collections.Generic;
using UnityEngine;
using Mirror;
using NetworkService.Shared;
using NetworkMessages;
using Cysharp.Threading.Tasks;
using System;

namespace NetworkService.Client
{
    public enum SubscriptionState
    {
        Deferred,
        Pending,
        Confirmed
    }

    public class SubscriptionServiceClient : MonoBehaviour,
        ISubscriptionService,
        IClientSubscriptionCallbacks
    {
        private SubscriptionNetworkBehaviour _networkBehaviour;
        private readonly Dictionary<string, SubscriptionState> _subscriptions = new();
        private readonly Dictionary<string, float> _pendingTimestamps = new();
        private bool _isDestroyed;
        private const float PENDING_TIMEOUT = 5f;
        private const float RETRY_INTERVAL = 1f;

        public void OnNetworkBehaviourReady(SubscriptionNetworkBehaviour behaviour)
        {
            if (behaviour == null)
                throw new ArgumentNullException(nameof(behaviour));

            _networkBehaviour = behaviour;
            ProcessDeferredSubscriptionsAsync().Forget();
            ProcessPendingTimeoutsAsync().Forget();
        }

        public void OnSubscriptionConfirmed(string messageTypeName)
        {
            if (string.IsNullOrEmpty(messageTypeName))
                return;

            if (_subscriptions.TryGetValue(messageTypeName, out var state) && state == SubscriptionState.Pending)
            {
                _subscriptions[messageTypeName] = SubscriptionState.Confirmed;
                _pendingTimestamps.Remove(messageTypeName);
            }
        }

        public void Subscribe<T>() where T : struct, NetworkMessage
        {
            string typeName = typeof(T).FullName;

            if (_subscriptions.TryGetValue(typeName, out var state))
            {
                if (state == SubscriptionState.Confirmed || state == SubscriptionState.Pending)
                    return;
            }

            if (!IsClientReady() || _networkBehaviour == null)
            {
                _subscriptions[typeName] = SubscriptionState.Deferred;
                return;
            }

            _subscriptions[typeName] = SubscriptionState.Pending;
            _pendingTimestamps[typeName] = Time.time;
            _networkBehaviour.CmdSubscribe(typeName);
        }

        public void Unsubscribe<T>() where T : struct, NetworkMessage
        {
            string typeName = typeof(T).FullName;

            if (_subscriptions.ContainsKey(typeName))
            {
                _subscriptions.Remove(typeName);
                _pendingTimestamps.Remove(typeName);
            }

            if (_networkBehaviour != null)
            {
                _networkBehaviour.CmdUnsubscribe(typeName);
            }
        }

        public bool IsSubscribed<T>() where T : struct, NetworkMessage
        {
            return _subscriptions.TryGetValue(typeof(T).FullName, out var state) 
                && state == SubscriptionState.Confirmed;
        }

        private bool IsClientReady()
        {
            return NetworkClient.connection != null && NetworkClient.ready;
        }

        private async UniTaskVoid ProcessDeferredSubscriptionsAsync()
        {
            while (!_isDestroyed)
            {
                if (IsClientReady() && _networkBehaviour != null)
                {
                    var deferredTypes = new List<string>();

                    foreach (var kvp in _subscriptions)
                    {
                        if (kvp.Value == SubscriptionState.Deferred)
                            deferredTypes.Add(kvp.Key);
                    }

                    foreach (var typeName in deferredTypes)
                    {
                        if (_subscriptions[typeName] != SubscriptionState.Deferred)
                            continue;

                        _subscriptions[typeName] = SubscriptionState.Pending;
                        _pendingTimestamps[typeName] = Time.time;
                        _networkBehaviour.CmdSubscribe(typeName);
                    }
                }

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(RETRY_INTERVAL),
                        cancellationToken: this.GetCancellationTokenOnDestroy());
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async UniTaskVoid ProcessPendingTimeoutsAsync()
        {
            while (!_isDestroyed)
            {
                var now = Time.time;
                var retryList = new List<string>();

                foreach (var kvp in _pendingTimestamps)
                {
                    if (now - kvp.Value > PENDING_TIMEOUT)
                        retryList.Add(kvp.Key);
                }

                foreach (var typeName in retryList)
                {
                    _pendingTimestamps.Remove(typeName);

                    if (IsClientReady() && _networkBehaviour != null)
                    {
                        _pendingTimestamps[typeName] = now;
                        _networkBehaviour.CmdSubscribe(typeName);
                    }
                    else
                    {
                        _subscriptions[typeName] = SubscriptionState.Deferred;
                    }
                }

                try
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(RETRY_INTERVAL),
                        cancellationToken: this.GetCancellationTokenOnDestroy());
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;

            if (NetworkClient.ready && _networkBehaviour != null)
            {
                foreach (var kvp in _subscriptions)
                {
                    if (kvp.Value == SubscriptionState.Confirmed)
                    {
                        _networkBehaviour.CmdUnsubscribe(kvp.Key);
                    }
                }
            }

            _subscriptions.Clear();
            _pendingTimestamps.Clear();
        }
    }
}