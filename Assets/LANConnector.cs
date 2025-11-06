using Mirror;
using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class LANConnector : MonoBehaviour
{
    public MyNetworkManager manager;
    public static LANConnector Instence;

    public bool enableUdpAnnounce = false;
    public int udpAnnouncePort = 7777;

    private UdpClient udpAnnouncer;

    public string roomName = "DefaultRoom";
    public string roomPassword = "1234";

    int myPort;

    private void Awake()
    {
        Instence = this;
    }

    private void Start()
    {
        Debug.Log("🌐 LANConnector initialized");
        if (enableUdpAnnounce)
            StartUdpListener();
    }

    private void OnDestroy()
    {
        StopUdpAnnouncer();
    }

    // 🟢 Host Start — automatic dynamic port
    public void StartHost()
    {

        enableUdpAnnounce = true; // ensure broadcast is ons

        int port = GetAvailablePort();
        string ip = GetLocalIPAddress();

        var transport = manager.GetComponent<TelepathyTransport>();
        transport.port = (ushort)port;
        manager.networkAddress = ip;
        manager.StartHost();

        myPort = port;

        Debug.Log($"✅ Host Started at {ip}:{port}");

        if (enableUdpAnnounce)
        {
            InvokeRepeating(nameof(AnnounceHostRepeatedly), 1f, 2f);
        }
    }

    void AnnounceHostRepeatedly()
    {
        try
        {
            if (udpAnnouncer == null)
                udpAnnouncer = new UdpClient() { EnableBroadcast = true };

            int port = ((TelepathyTransport)manager.transport).port;
            string payloadText = $"HOST_ANNOUNCE:{roomName}:{roomPassword}:{port}";
            byte[] payload = Encoding.UTF8.GetBytes($"HOST_ANNOUNCE:{port}");
            string localIP = GetLocalIPAddress();

            string subnet = localIP.Substring(0, localIP.LastIndexOf('.') + 1);
            IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Parse(subnet + "255"), udpAnnouncePort);
            udpAnnouncer.Send(payload, payload.Length, broadcastEP);

          //  Debug.Log($"📢 Announced host on LAN {broadcastEP.Address}:{udpAnnouncePort} (port {port})");

            Debug.Log($"📢 Announced host '{roomName}' with password '{roomPassword}' on LAN {broadcastEP.Address}:{udpAnnouncePort} (port {port})");

        }
        catch (Exception ex)
        {
            Debug.LogWarning("AnnounceHostRepeatedly failed: " + ex.Message);
        }

        if(!enableUdpAnnounce)
        {
            CancelInvoke(nameof(AnnounceHostRepeatedly));
        }
    }

    // 🟡 Client Start — requires host IP manually or via auto-discovery
    public void StartClient()
    {
        Debug.Log("🔍 Searching for available LAN hosts...");

        UdpClient listener = new UdpClient(udpAnnouncePort);
        listener.EnableBroadcast = true;
        listener.Client.ReceiveTimeout = 5000; // wait 5 sec

        DateTime endTime = DateTime.Now.AddSeconds(5);
        while (DateTime.Now < endTime)
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, udpAnnouncePort);
                byte[] data = listener.Receive(ref remoteEP);
                string msg = Encoding.UTF8.GetString(data);

                if (msg.StartsWith("HOST_ANNOUNCE:"))
                {
                    int announcedPort = int.Parse(msg.Substring("HOST_ANNOUNCE:".Length));
                    string hostIP = remoteEP.Address.ToString();

                    Debug.Log($"✅ Found LAN host: {hostIP}:{announcedPort}");

                    var transport = manager.GetComponent<TelepathyTransport>();
                    transport.port = (ushort)announcedPort;
                    manager.networkAddress = hostIP;
                    manager.StartClient();

                    Debug.Log($"🕹️ Connecting to {hostIP}:{announcedPort}");
                    listener.Close();
                    return;
                }
            }
            catch (SocketException) { /* timeout retry */ }
        }

        listener.Close();
        Debug.LogWarning("⚠️ No hosts found on LAN (no announcements received).");
    }

    // 🔴 Stop all
    public void StopAll()
    {
        manager.StopHost();
        manager.StopClient();
        StopUdpAnnouncer();
        Debug.Log("🛑 All connections stopped");
    }

    // 🌐 Local IP
    string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("GetLocalIPAddress failed: " + ex.Message);
        }
        return "127.0.0.1";
    }

    // 🔹 Find an available (free) local port
    int GetAvailablePort(bool excludeAnnounced = false)
    {
        try
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            if (excludeAnnounced && enableUdpAnnounce && IsPortAnnouncedOnLan(port))
                return GetAvailablePort(excludeAnnounced: true);

            return port;
        }
        catch
        {
            return UnityEngine.Random.Range(20000, 40000);
        }
    }

    // 🔹 Check local port free or not
    bool IsPortFreeOnLocalMachine(int port)
    {
        IPGlobalProperties props = IPGlobalProperties.GetIPGlobalProperties();
        var tcpListeners = props.GetActiveTcpListeners();
        if (tcpListeners.Any(ep => ep.Port == port)) return false;

        var udpListeners = props.GetActiveUdpListeners();
        if (udpListeners.Any(ep => ep.Port == port)) return false;

        return true;
    }

    // 🛰️ UDP Announce
    void StartUdpAnnouncer(int port)
    {
        try
        {
            StopUdpAnnouncer();
            udpAnnouncer = new UdpClient();
            udpAnnouncer.EnableBroadcast = true;

            byte[] payload = Encoding.UTF8.GetBytes($"HOST_ANNOUNCE:{port}");
            IPEndPoint broadcastEP = new IPEndPoint(IPAddress.Broadcast, udpAnnouncePort);
            udpAnnouncer.Send(payload, payload.Length, broadcastEP);
            Debug.Log($"📢 Announced host on LAN (port {port})");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("StartUdpAnnouncer failed: " + ex.Message);
        }
    }

    void StopUdpAnnouncer()
    {
        if (udpAnnouncer != null)
        {
            udpAnnouncer.Close();
            udpAnnouncer = null;
        }
    }

    // 🛰️ UDP Listener (for discovering other hosts)
    void StartUdpListener()
    {
        try
        {
            UdpClient udpListener = new UdpClient(udpAnnouncePort);
            udpListener.EnableBroadcast = true;

            udpListener.BeginReceive((ar) =>
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, udpAnnouncePort);
                    byte[] data = udpListener.EndReceive(ar, ref remoteEP);
                    string msg = Encoding.UTF8.GetString(data);

                    if (msg.StartsWith("HOST_ANNOUNCE:"))
                    {
                        int announcedPort = int.Parse(msg.Substring("HOST_ANNOUNCE:".Length));
                        Debug.Log($"📡 Host announced: {remoteEP.Address}:{announcedPort}");
                        PlayerPrefs.SetInt("LastAnnouncedPort", announcedPort);
                        PlayerPrefs.Save();
                    }

                    udpListener.BeginReceive((cb) => { }, null);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"UDP listener error: {ex.Message}");
                }
            }, null);

            Debug.Log($"👂 UDP listener started on port {udpAnnouncePort}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("StartUdpListener failed: " + ex.Message);
        }
    }

    // 🔍 Check if port already announced on LAN
    bool IsPortAnnouncedOnLan(int portToCheck, int listenMs = 200)
    {
        if (!enableUdpAnnounce) return false;

        using (UdpClient listener = new UdpClient(udpAnnouncePort))
        {
            listener.Client.ReceiveTimeout = listenMs;
            DateTime until = DateTime.Now.AddMilliseconds(listenMs);
            try
            {
                while (DateTime.Now < until)
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = listener.Receive(ref remoteEP);
                    string msg = Encoding.UTF8.GetString(data);
                    if (msg.StartsWith("HOST_ANNOUNCE:") &&
                        int.TryParse(msg.Substring("HOST_ANNOUNCE:".Length), out int announcedPort) &&
                        announcedPort == portToCheck)
                    {
                        Debug.Log($"Detected existing LAN host using port {portToCheck} from {remoteEP.Address}");
                        return true;
                    }
                }
            }
            catch { }
        }
        return false;
    }

    int? GetLastAnnouncedPort()
    {
        if (PlayerPrefs.HasKey("LastAnnouncedPort"))
            return PlayerPrefs.GetInt("LastAnnouncedPort");
        return null;
    }
}
