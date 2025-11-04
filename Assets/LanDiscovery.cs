using UnityEngine;
using Mirror;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LanDiscovery : MonoBehaviour
{
    public int discoveryPort = 47777;
    public string gameIdentifier = "PanicPond";

    private UdpClient udpClient;
    private NetworkManager networkManager;

    public List<HostInfo> foundHosts = new List<HostInfo>();

    public struct HostInfo
    {
        public string ip;
        public int port;
    }

    void Start()
    {
        networkManager = GetComponent<NetworkManager>();
    }

    public void StartHosting()
    {
        networkManager.StartHost();
        StartBroadcasting();
    }

    public void StartSearching()
    {
        foundHosts.Clear();
        StartListening();
        SendBroadcast();
    }

    private void StartBroadcasting()
    {
        udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        InvokeRepeating(nameof(SendBroadcast), 0f, 1f);
    }

    private void SendBroadcast()
    {
        if (udpClient == null) return;

        string message = $"{gameIdentifier}|{GetLocalIPAddress()}|{networkManager.transport.ServerUri().Port}";
        byte[] data = Encoding.UTF8.GetBytes(message);
        IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
        udpClient.Send(data, data.Length, ep);
    }

    private async void StartListening()
    {
        udpClient = new UdpClient(discoveryPort);
        udpClient.EnableBroadcast = true;

        float timeout = Time.realtimeSinceStartup + 3f;

        while (Time.realtimeSinceStartup < timeout)
        {
            if (udpClient.Available > 0)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (message.StartsWith(gameIdentifier))
                {
                    var parts = message.Split('|');
                    if (parts.Length == 3)
                    {
                        string ip = parts[1];
                        int port = int.Parse(parts[2]);
                        if (!foundHosts.Exists(h => h.ip == ip && h.port == port))
                        {
                            foundHosts.Add(new HostInfo { ip = ip, port = port });
                            Debug.Log($"Found LAN host: {ip}:{port}");
                        }
                    }
                }
            }
            await Task.Yield();
        }

        udpClient.Close();
        udpClient = null;
    }

    public void JoinGame(HostInfo host)
    {
        networkManager.networkAddress = host.ip;
        networkManager.StartClient();
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
    }

    void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}
