using UnityEngine;
using Mirror;
using System.Net;
using System.Net.Sockets;
using UnityEngine.UI;

public class LANConnector : MonoBehaviour
{
    public MyNetworkManager manager;
    public string myIP;
    public InputField portInputField;

    public static LANConnector Instence;

    private void Awake()
    {
        Instence = this;
    }

    private void Start()
    {
        // Local IP auto fill
        if (myIP != null)
            myIP = GetLocalIPAddress();
    }

    // 🟢 Host Start
    public void StartHost()
    {
        string ip = GetLocalIPAddress();
        int port = int.Parse(portInputField.text.Trim());
        var transport = manager.GetComponent<TelepathyTransport>();
        transport.port = (ushort)port;
        manager.networkAddress = ip;
        manager.StartHost();
        Debug.Log($"✅ Host Started at {ip}:{port}");
    }

    // 🔴 Stop
    public void StopAll()
    {
        manager.StopHost();
        manager.StopClient();
        Debug.Log("🛑 All connections stopped");
    }

    // 🟡 Client Start
    public void StartClient()
    {
        string ip = myIP.Trim();
        int port = int.Parse(portInputField.text.Trim());
        var transport = manager.GetComponent<TelepathyTransport>();
        transport.port = (ushort)port;
        manager.networkAddress = ip;
        manager.StartClient();
        Debug.Log($"🕹️ Connecting to {ip}:{port}");
    }

    // 🌐 Get local IP
    string GetLocalIPAddress()
    {
        string localIP = "Not Found";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }
}
