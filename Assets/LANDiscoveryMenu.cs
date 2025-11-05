using Mirror;
using Mirror.Discovery;
using UnityEngine;
using UnityEngine.UI;

public class LANDiscoveryMenu : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;

    private ushort finalPort;
    int listenPort;

    public InputField roomNameInputField, listenPortNameInputField2;

    public void HostGame()
    {
        finalPort = ushort.Parse(roomNameInputField.text.Trim());
        listenPort = int.Parse(listenPortNameInputField2.text.Trim());

        // Transport में पोर्ट सेट करें
        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port = finalPort;

        // Host शुरू करें
        NetworkManager.singleton.StartHost();

        networkDiscovery.serverBroadcastListenPort = listenPort;
        // Broadcast करें
        networkDiscovery.AdvertiseServer();

        Debug.Log($"🏠 Hosting LAN room '{roomNameInputField.text}' on port {finalPort} serverBroadcastListenPort '{networkDiscovery.serverBroadcastListenPort}' ");
    }

    
    public void FindGames()
    {
        Debug.Log("🔎 Searching for LAN games...");

        // ✅ Read ports from UI again
        finalPort = ushort.Parse(roomNameInputField.text.Trim());  // game port
        listenPort = int.Parse(listenPortNameInputField2.text.Trim()); // broadcast port

        // ✅ Ensure both ports are applied before discovery
        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port = finalPort;

        networkDiscovery.serverBroadcastListenPort = listenPort;

        Debug.Log($"🛰️ Client discovery started (gamePort={finalPort}, broadcastPort={listenPort})");

        // ✅ Start searching for servers
        networkDiscovery.StartDiscovery();
    }

}