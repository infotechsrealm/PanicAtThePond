using Mirror;
using Mirror.Discovery;
using UnityEngine;

public class LANDiscoveryMenu : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;

    public void HostGame()
    {
        NetworkManager.singleton.StartHost();
        networkDiscovery.AdvertiseServer();
        Debug.Log("🏠 Hosting LAN game (broadcasting)...");
    }

    public void FindGames()
    {
        Debug.Log("🔎 Searching for LAN games...");
        networkDiscovery.StartDiscovery();
    }
}
