using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class LANDiscoveryMenu : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;

    private ushort finalPort;
    private int listenPort;

    public ushort baseGamePort = 7777;
    public int baseBroadcastPort = 47777;

    private static HashSet<int> usedPorts = new HashSet<int>(); // 💾 याद रखे कौन से ports पहले use हुए


    [System.Serializable]
    public class DiscoveredServer
    {
        public string serverName;
        public string address;
        public int port;
    }


    void Awake()
    {
        finalPort = baseGamePort;
        listenPort = baseBroadcastPort;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {

            StartCoroutine(DiscoverAllLANHosts_Unlimited((hosts) =>
            {
                Debug.Log("========== 🌐 ALL LAN HOSTS FOUND ==========");
                foreach (var h in hosts)
                    Debug.Log($"🏠 {h.address}:{h.port} ({h.serverName})");
            }));


        }

    }

    public void HostGame()
    {
        FindFreePortAndHost();
    }

    void FindFreePortAndHost()
    {
        int tryGamePort = baseGamePort;
        int tryBroadcastPort = baseBroadcastPort;

        Debug.Log("🔍 Checking for free LAN game port...");

        while (true)
        {
            // Step 1️⃣ : Local TCP check (Mirror’s Telepathy will use TCP)
            if (!IsLocalTcpPortFree(tryGamePort))
            {
                tryGamePort++;
                tryBroadcastPort++;
                continue;
            }

            // Step 2️⃣ : Local UDP check (for broadcast listen port)
            if (!IsLocalUdpPortFree(tryBroadcastPort))
            {
                tryGamePort++;
                tryBroadcastPort++;
                continue;
            }

            // Step 3️⃣ : LAN discovery check
            bool portUsedOnLAN = false;
            bool discoveryDone = false;

            networkDiscovery.OnServerFound.RemoveAllListeners();
            networkDiscovery.OnServerFound.AddListener((response) =>
            {
                int discoveredPort = response.uri?.Port ?? 0;
                if (discoveredPort == tryGamePort)
                    portUsedOnLAN = true;
            });

            networkDiscovery.StartDiscovery();
            //yield return new WaitForSeconds(0.1f);
            networkDiscovery.StopDiscovery();
            discoveryDone = true;

            if (!portUsedOnLAN && discoveryDone)
                break;

            tryGamePort++;
            tryBroadcastPort++;
        }

        finalPort = (ushort)tryGamePort;
        listenPort = tryBroadcastPort;

        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port = finalPort;

        NetworkManager.singleton.StartHost();

        networkDiscovery.serverBroadcastListenPort = listenPort;
        networkDiscovery.AdvertiseServer();

        Debug.Log($"🏠 Hosting LAN on free port {finalPort}, broadcast {listenPort}");
    }


    // ✅ Local TCP availability check
    private bool IsLocalTcpPortFree(int port)
    {
        try
        {
            TcpListener tcp = new TcpListener(IPAddress.Any, port);
            tcp.Start();
            tcp.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    // ✅ Local UDP availability check
    private bool IsLocalUdpPortFree(int port)
    {
        try
        {
            UdpClient udp = new UdpClient(port);
            udp.Close();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }


    public void FindGames()
    {
        listenPort = baseBroadcastPort;
        networkDiscovery.serverBroadcastListenPort = listenPort;

        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port = baseGamePort;

        Debug.Log($"🔎 Searching for LAN hosts (game={transport.port}, broadcast={listenPort})...");
        networkDiscovery.StartDiscovery();
    }


    // 📡 यह function LAN में सारे hosts ढूंढता है और result return करता है
    public IEnumerator DiscoverAllLANHosts_Unlimited(System.Action<List<DiscoveredServer>> onComplete)
    {
        List<DiscoveredServer> foundServers = new List<DiscoveredServer>();

        int currentPort = baseBroadcastPort; // 47777 से शुरू
        int silenceCounter = 0;              // लगातार खाली ports की गिनती
        int silenceLimit = 15;               // अगर 15 लगातार ports पर कुछ नहीं मिला तो scan रोक दो

        Debug.Log("🌐 Starting full LAN host discovery...");

        while (silenceCounter < silenceLimit)
        {
            bool foundOnThisPort = false;

            networkDiscovery.OnServerFound.RemoveAllListeners();

            networkDiscovery.OnServerFound.AddListener((response) =>
            {
                if (response.uri != null)
                {
                    string ip = response.EndPoint.Address.ToString();
                    int port = response.uri.Port;
                    string name = response.serverId.ToString();

                    if (!foundServers.Exists(s => s.port == port && s.address == ip))
                    {
                        foundServers.Add(new DiscoveredServer()
                        {
                            serverName = name,
                            address = ip,
                            port = port
                        });

                        Debug.Log($"📡 Found Host → {ip}:{port} ({name})");
                    }

                    foundOnThisPort = true;
                }
            });

            networkDiscovery.serverBroadcastListenPort = currentPort;
            networkDiscovery.StartDiscovery();
            Debug.Log($"🔎 Scanning LAN for hosts on broadcast port {currentPort}...");
            yield return new WaitForSeconds(0.25f);
            networkDiscovery.StopDiscovery();

            if (foundOnThisPort)
                silenceCounter = 0;
            else
                silenceCounter++;

            currentPort++;
        }

        Debug.Log($"✅ Found {foundServers.Count} total hosts on LAN (scanned until port {currentPort - 1}).");
        onComplete?.Invoke(foundServers);
    }



}
