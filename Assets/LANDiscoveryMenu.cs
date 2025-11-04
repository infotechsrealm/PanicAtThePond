using Mirror;
using Mirror.Discovery;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
public class LANDiscoveryMenu : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;

    private ushort finalPort;

    public InputField roomNameInputField;

    public void HostGame()
    {
        finalPort = ushort.Parse(roomNameInputField.text.Trim());

        // Transport में पोर्ट सेट करें
        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port = finalPort;

        // Host शुरू करें
        NetworkManager.singleton.StartHost();

        // Broadcast करें
        networkDiscovery.AdvertiseServer();

        Debug.Log($"🏠 Hosting LAN room '{roomNameInputField.text}' on port {finalPort}");
    }

    public void FindGames()
    {
        Debug.Log("🔎 Searching for LAN games...");
        networkDiscovery.StartDiscovery();
    }

    // ------------------------
    // 🔍 Check कौन-सा port free है
    // ------------------------
    private ushort FindFreePort(ushort start, ushort end)
    {
        for (ushort port = start; port <= end; port++)
        {
            if (IsPortAvailable(port))
                return port;
        }
        return 0; // कोई free port नहीं मिला
    }

    // ------------------------
    // ✅ Check if port is free
    // ------------------------
    private bool IsPortAvailable(int port)
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
