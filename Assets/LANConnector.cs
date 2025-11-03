using UnityEngine;
using Mirror;
using System.Net;
using System.Net.Sockets;

public class LANConnector : MonoBehaviour
{
    public MyNetworkManager manager;
    public string myIP;
    public int startPort = 7777; // starting point for ports
    public static LANConnector Instence;

    private void Awake()
    {
        Instence = this;
    }

    private void Start()
    {
        myIP = GetLocalIPAddress();
    }

    // 🟢 HOST START
    public void StartHost()
    {
        string ip = GetLocalIPAddress();
        var transport = manager.GetComponent<TelepathyTransport>();

        int port = startPort;

        while (port < 7800)
        {
            transport.port = (ushort)port;
            manager.networkAddress = ip;

            try
            {
                manager.StartHost();
                Debug.Log($"✅ Host started at {ip}:{port}");
                return;
            }
            catch (System.Net.Sockets.SocketException)
            {
                Debug.LogWarning($"⚠️ Port {port} in use, trying next...");
                port++;
            }
        }

        Debug.LogError("❌ No available ports found between 7777–7800!");
    }


    // 🔴 STOP
    public void StopAll()
    {
        manager.StopHost();
        manager.StopClient();
        Debug.Log("🛑 All connections stopped");
    }

    // 🟡 CLIENT START
    public void StartClient()
    {
        string ip = myIP.Trim();
        var transport = manager.GetComponent<TelepathyTransport>();

        int port = FindFirstActivePort(ip, startPort, 7800);
        if (port == -1)
        {
            Debug.LogWarning("⚠️ No active host found to connect.");
            return;
        }

        transport.port = (ushort)port;
        manager.networkAddress = ip;
        manager.StartClient();

        Debug.Log($"🕹 Connecting to {ip}:{port}");
    }

    // 🌐 LOCAL IP
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

    // 🔍 Find next available port for hosting
    int FindAvailablePort(int start, int end)
    {
        for (int port = start; port <= end; port++)
        {
            if (IsPortAvailable(port))
                return port;
        }
        Debug.LogWarning("⚠️ No free port found in range!");
        return start; // fallback
    }

    // ✅ Check if a port is free
    bool IsPortAvailable(int port)
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // 🔎 Find first open host to connect
    int FindFirstActivePort(string ip, int start, int end)
    {
        for (int port = start; port <= end; port++)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(ip, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(100); // small timeout
                    if (success)
                    {
                        client.Close();
                        return port;
                    }
                }
            }
            catch { }
        }
        return -1;
    }
}
