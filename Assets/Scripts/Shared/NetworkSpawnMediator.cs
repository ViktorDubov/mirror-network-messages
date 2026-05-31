using Mirror;
using UnityEngine;
using VContainer;

namespace NetworkService.Shared
{
    public class NetworkSpawnMediator : MonoBehaviour
    {
        private IObjectResolver _parentResolver;
        private IObjectResolver _serverResolver;
        private IObjectResolver _clientResolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _parentResolver = resolver;
            SubscriptionNetworkBehaviour.SetMediator(this);
        }

        private void OnDestroy()
        {
            SubscriptionNetworkBehaviour.SetMediator(null);
        }

        public void UpdateScopes(IObjectResolver serverResolver, IObjectResolver clientResolver)
        {
            _serverResolver = serverResolver;
            _clientResolver = clientResolver;
        }

        public void OnBehaviourStartClient(SubscriptionNetworkBehaviour behaviour)
        {
            var resolver = _clientResolver ?? _parentResolver;
            
            if (resolver == null)
            {
                Debug.LogError("[NetworkSpawnMediator] No resolver available for client");
                return;
            }

            try
            {
                var callbacks = resolver.Resolve<IClientSubscriptionCallbacks>();
                behaviour.SetClientCallbacks(callbacks);
            }
            catch (VContainerException ex)
            {
                Debug.LogError($"[NetworkSpawnMediator] Failed to resolve IClientSubscriptionCallbacks: {ex.Message}");
            }
        }

        public void OnBehaviourStartServer(SubscriptionNetworkBehaviour behaviour)
        {
            var resolver = _serverResolver ?? _parentResolver;
            
            if (resolver == null)
            {
                Debug.LogError("[NetworkSpawnMediator] No resolver available for server");
                return;
            }

            try
            {
                var handler = resolver.Resolve<IServerSubscriptionHandler>();
                behaviour.SetServerHandler(handler);
            }
            catch (VContainerException ex)
            {
                Debug.LogError($"[NetworkSpawnMediator] Failed to resolve IServerSubscriptionHandler: {ex.Message}");
            }
        }
    }
}