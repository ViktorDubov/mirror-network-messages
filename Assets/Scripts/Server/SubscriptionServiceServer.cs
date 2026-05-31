using System.Collections.Generic;
using Mirror;
using UnityEngine;
using NetworkService.Shared;

namespace NetworkService.Server
{
    public class SubscriptionServiceServer : MonoBehaviour, IServerSubscriptionHandler
    {
        private readonly Dictionary<NetworkConnection, HashSet<string>> _clientSubscriptions = new();
        private readonly Dictionary<NetworkConnection, SubscriptionNetworkBehaviour> _behaviours = new();

        [System.NonSerialized] public MessageRouterServer MessageRouter;

        public void OnClientConnected(NetworkConnection conn, SubscriptionNetworkBehaviour behaviour)
        {
            if (conn == null)
            {
                Debug.LogError("[SubscriptionServiceServer] Attempted to register null connection");
                return;
            }

            _behaviours[conn] = behaviour;
            
            if (!_clientSubscriptions.TryGetValue(conn, out var subs))
            {
                subs = new HashSet<string>();
                _clientSubscriptions[conn] = subs;
            }
        }

        public void OnClientDisconnected(NetworkConnection conn)
        {
            if (conn == null)
                return;

            _clientSubscriptions.Remove(conn);
            _behaviours.Remove(conn);
        }

        public void HandleSubscription(NetworkConnection conn, string messageTypeName, bool isSubscribing)
        {
            if (conn == null)
            {
                Debug.LogWarning("[SubscriptionServiceServer] HandleSubscription: null connection");
                return;
            }

            if (!_clientSubscriptions.TryGetValue(conn, out var subs))
            {
                Debug.LogWarning($"[SubscriptionServiceServer] HandleSubscription: unknown connection");
                return;
            }

            if (string.IsNullOrEmpty(messageTypeName))
            {
                Debug.LogWarning("[SubscriptionServiceServer] HandleSubscription: empty message type");
                return;
            }

            if (isSubscribing)
            {
                if (subs.Add(messageTypeName))
                {
                    if (_behaviours.TryGetValue(conn, out var behaviour))
                    {
                        behaviour.TargetSubscriptionConfirmed(messageTypeName);
                    }
                }
            }
            else
            {
                subs.Remove(messageTypeName);
            }
        }

        public void SendToSubscribers<T>(T message) where T : struct, NetworkMessage
        {
            string typeName = typeof(T).FullName;
            int sentCount = 0;

            var disconnectedConnections = new List<NetworkConnection>();

            foreach (var kvp in _clientSubscriptions)
            {
                var conn = kvp.Key;
                var subs = kvp.Value;

                if (!conn.isReady)
                {
                    disconnectedConnections.Add(conn);
                    continue;
                }

                if (subs.Contains(typeName))
                {
                    conn.Send(message);
                    sentCount++;
                }
            }

            foreach (var conn in disconnectedConnections)
            {
                _clientSubscriptions.Remove(conn);
                _behaviours.Remove(conn);
            }

            if (sentCount == 0)
            {
                Debug.LogWarning($"[SubscriptionServiceServer] No subscribers for {typeName}");
            }
        }

        public bool IsClientSubscribed(NetworkConnection conn, string messageTypeName)
        {
            if (conn == null || string.IsNullOrEmpty(messageTypeName))
                return false;

            return _clientSubscriptions.TryGetValue(conn, out var subs)
                && subs.Contains(messageTypeName);
        }
    }
}