using Mirror;
using Mirror.Discovery;
using System;
using UnityEngine;

public class LANDiscoveryHandler : MonoBehaviour
{
    public NetworkDiscovery discovery;

    void Start()
    {
        discovery.OnServerFound.AddListener(OnDiscoveredServer);
    }

    void OnDiscoveredServer(ServerResponse info)
    {
        Debug.Log($"✅ Found host: {info.EndPoint.Address}:{info.uri}");
        // You can auto-join or show this in a UI list
        NetworkManager.singleton.networkAddress = info.EndPoint.Address.ToString();
        NetworkManager.singleton.StartClient();
    }
}
