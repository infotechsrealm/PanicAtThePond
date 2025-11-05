using Mirror;
using Mirror.Discovery;
using UnityEngine;

public class LANDiscoveryHandler : MonoBehaviour
{
    public NetworkDiscovery discovery;
    private bool isConnected = false;

    void Start()
    {
        discovery.OnServerFound.AddListener(OnDiscoveredServer);
    }

    void OnDiscoveredServer(ServerResponse info)
    {
        if (isConnected) return; // Already joined

        string hostAddress = info.EndPoint.Address.ToString();
        Debug.Log($"✅ Found host: {hostAddress} | URI: {info.uri}");

        NetworkManager.singleton.networkAddress = hostAddress;

        // Try connecting automatically
        NetworkManager.singleton.StartClient();

        isConnected = true;
        Debug.Log("🚀 Auto-joining host...");
    }

}
