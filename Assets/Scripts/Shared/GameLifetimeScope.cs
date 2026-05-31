using VContainer;
using VContainer.Unity;
using UnityEngine;
using Mirror;
using NetworkService.Server;
using NetworkService.Client;
using System.Linq;
using Cysharp.Threading.Tasks;
using System;

namespace NetworkService.Shared
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private float _networkInitTimeout = 5f;

        [Header("Server Prefab")]
        [SerializeField] private GameObject _serverScopePrefab;

        [Header("Client Prefab")]
        [SerializeField] private GameObject _clientScopePrefab;

        private ServerLifetimeScope _activeServerScope;
        private ClientLifetimeScope _activeClientScope;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<NetworkSpawnMediator>().AsSelf();
            builder.RegisterComponentInHierarchy<NetworkBootstrap>().AsSelf();
        }

        private void Start()
        {
            DetectAndConfigureScopesAsync().Forget();
        }

        private async UniTaskVoid DetectAndConfigureScopesAsync()
        {
            bool networkReady = await WaitForNetworkManagerAsync();

            if (!networkReady)
            {
                Debug.LogError("[GameLifetimeScope] NetworkManager init timed out");
                return;
            }

            bool isHost = NetworkManager.singleton.mode == NetworkManagerMode.Host;
            bool isServer = NetworkManager.singleton.mode == NetworkManagerMode.ServerOnly;
            bool isClient = NetworkManager.singleton.mode == NetworkManagerMode.ClientOnly;

            bool serverActive = isServer || isHost;
            bool clientActive = isClient || isHost;

            CreateScopes(serverActive, clientActive);
        }

        private async UniTask<bool> WaitForNetworkManagerAsync()
        {
            try
            {
                await UniTask.WaitUntil(() => NetworkManager.singleton != null,
                    cancellationToken: destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            var timeout = UniTask.Delay(TimeSpan.FromSeconds(_networkInitTimeout));
            var waitForReady = UniTask.WaitUntil(
                () => NetworkManager.singleton.mode != NetworkManagerMode.Offline,
                cancellationToken: destroyCancellationToken);

            var result = await UniTask.WhenAny(timeout, waitForReady);
            return result == 1;
        }

        private void CreateScopes(bool serverActive, bool clientActive)
        {
            if (serverActive && _serverScopePrefab != null && _activeServerScope == null)
            {
                var serverGo = Instantiate(_serverScopePrefab, transform);
                serverGo.name = "ServerScope (Dynamic)";
                _activeServerScope = serverGo.GetComponent<ServerLifetimeScope>();
                
                if (_activeServerScope != null)
                {
                    _activeServerScope.Build();
                }
                else
                {
                    Debug.LogError("[GameLifetimeScope] ServerScopePrefab missing ServerLifetimeScope component");
                }
            }

            if (clientActive && _clientScopePrefab != null && _activeClientScope == null)
            {
                var clientGo = Instantiate(_clientScopePrefab, transform);
                clientGo.name = "ClientScope (Dynamic)";
                _activeClientScope = clientGo.GetComponent<ClientLifetimeScope>();
                
                if (_activeClientScope != null)
                {
                    _activeClientScope.Build();
                }
                else
                {
                    Debug.LogError("[GameLifetimeScope] ClientScopePrefab missing ClientLifetimeScope component");
                }
            }

            var mediator = Container?.Resolve<NetworkSpawnMediator>();
            if (mediator != null)
            {
                mediator.UpdateScopes(_activeServerScope?.Container, _activeClientScope?.Container);
            }
        }

        protected override void OnDestroy()
        {
            DisposeScope(_activeServerScope);
            DisposeScope(_activeClientScope);
            base.OnDestroy();
        }

        private void DisposeScope(LifetimeScope scope)
        {
            if (scope == null)
                return;

            scope.Dispose();
            if (scope.gameObject != null)
                Destroy(scope.gameObject);
        }
    }
}