using Mirror;
using UnityEngine;

namespace NetworkService.Shared
{
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private NetworkManagerMode _autoStartMode = NetworkManagerMode.Host;

        private void Start()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("[NetworkBootstrap] NetworkManager.singleton is null");
                return;
            }

            switch (_autoStartMode)
            {
                case NetworkManagerMode.Host:
                    NetworkManager.singleton.StartHost();
                    break;
                case NetworkManagerMode.ServerOnly:
                    NetworkManager.singleton.StartServer();
                    break;
                case NetworkManagerMode.ClientOnly:
                    NetworkManager.singleton.StartClient();
                    break;
            }
        }
    }
}