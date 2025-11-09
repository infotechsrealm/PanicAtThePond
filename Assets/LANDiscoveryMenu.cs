using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class DiscoveredServer
{
    public string roomName;
    public string address;
    public int port;
    public int baseBroadcastPort;
    public string roomPassword;
    public string playerName;
    public int playerCount;
    public int maxPlayers;

    public DiscoveredServer()
    {
        roomName = "abc";
        address = "";
        port = 7777;
        baseBroadcastPort = 47777;
        roomPassword = "";
        playerName = "";
        playerCount = 0;
        maxPlayers = 0;
    }
}

public class LANDiscoveryMenu : MonoBehaviour
{
    public static LANDiscoveryMenu Instance;
    public NetworkDiscovery networkDiscovery;

    private ushort finalPort;
    private int listenPort;

    public ushort baseGamePort = 7777;
    public int baseBroadcastPort = 47777;

    public InputField roomNameInput;

    internal string roomName,roomPassword;

    public List<DiscoveredServer> discoveredServers = new List<DiscoveredServer>();

    public DiscoveredServer DiscoveredServerInfo;

    public CreateJoinManager createJoinManager;

    public Text
        createRoomName,
        createRoomNameError,
        playerLimit,
        playerLimitError,
        roomPasswordInput,
        roomPasswordInputError;

    [Header("Room Settings")]
    internal int maxPlayers;

    internal bool isRoomJoined = false;
    void Awake()
    {
        Instance = this;
        finalPort = baseGamePort;
        listenPort = baseBroadcastPort;
        DiscoveredServerInfo.playerName = GS.Instance.nickName;
    }

    public void HostGame()
    {
        if (playerLimit.text != "")
        {
            maxPlayers = int.Parse(playerLimit.text);
        }

        // ✅ Username validation
        string room = createRoomName.text.Trim();

        if (string.IsNullOrEmpty(room))
        {
            createRoomNameError.text = "Username is required";
            return;
        }
        else if (room.Length < 3 || room.Length > 10)
        {
            createRoomNameError.text = "Username must be between 3 and 10 characters long";
            return;
        }
        else if (!Regex.IsMatch(room, @"^[a-zA-Z0-9_]+$"))
        {
            createRoomNameError.text = "Username can only contain letters, numbers, and underscores";
            return;
        }
        else
        {
            roomName = room;
            createRoomNameError.text = "";
        }

        // ✅ Player Limit validation
        if (maxPlayers < 2 || maxPlayers > 7)
        {
            playerLimitError.text = "Player Limit must be between 2 to 7 members";
            return;
        }
        else
        {
            playerLimitError.text = "";
        }

        // ✅ Password validation
        if (string.IsNullOrEmpty(roomPasswordInput.text))
        {
            /* roomPasswordInputError.text = "Password is required";
             return;*/
        }
        else if (roomPasswordInput.text.Length < 6)
        {
            roomPasswordInputError.text = "A minimum 6-digit password is required";
            return;
        }
        else
        {
            roomPasswordInputError.text = "";
        }

        roomPassword = roomPasswordInput.text.ToString().Trim();

        createRoomNameError.text = "";

        GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);


        StartCoroutine(CheckRooms());
       
    }

    //find room exist or not
    public IEnumerator CheckRooms()
    {
        bool roomExist = false;

        List<DiscoveredServer> foundServers = new List<DiscoveredServer>();

        int currentPort = 47777;             // 47777 से शुरू
        int silenceCounter = 0;              // लगातार खाली ports की गिनती
        int silenceLimit = 15;               // अगर 15 लगातार ports पर कुछ नहीं मिला तो scan रोक दो

        discoveredServers.Clear();
        Debug.Log("🌐 Starting full LAN host discovery...");

        bool foundOnThisPort = false;

        while (silenceCounter < silenceLimit)
        {
            networkDiscovery.OnServerFound.RemoveAllListeners();

            networkDiscovery.OnServerFound.AddListener((response) =>
            {
                if (response.uri != null)
                {
                    string ip = response.EndPoint.Address.ToString();
                    int port = response.uri.Port;
                    string name = response.roomName;

                    if (roomName == name)
                    {
                        Debug.Log(roomName + " = Room is all rady exist = " + name);
                        createRoomNameError.text = "Room is all rady exist , Please change it.";

                        GS.Instance.DestroyPreloder();
                        roomExist = true;
                    }
                }
            });

            networkDiscovery.serverBroadcastListenPort = currentPort;
            networkDiscovery.StartDiscovery();
            Debug.Log($"🔎 Scanning LAN for hosts on broadcast port {currentPort}...");
            yield return new WaitForSeconds(0.05f);
            networkDiscovery.StopDiscovery();

            if (foundOnThisPort)
                silenceCounter = 0;
            else
                silenceCounter++;

            currentPort++;
        }

        if (!roomExist)
        {
            Debug.Log("🏠 Room not exist , Hosting new room : " + roomName);
            FindFreePortAndHost();
        }
        else
        {
            Debug.Log("Room is allrady Exist .. Please change room name ");
        }
    }



    // Find free ports and host the game
    void FindFreePortAndHost()
    {
        int tryGamePort = baseGamePort;
        int tryBroadcastPort = baseBroadcastPort;

        Debug.Log("🔍 Checking for free LAN game port...");

        while (true)
        {
            Debug.Log("tryGamePort = " + tryGamePort + " tryBroadcastPort = "+ tryBroadcastPort);
            
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
            networkDiscovery.StopDiscovery();
            discoveryDone = true;

            if (!portUsedOnLAN && discoveryDone)
                break;

            tryGamePort++;
            tryBroadcastPort++;
        }

       

        if (!string.IsNullOrEmpty(roomPassword))
        {
            networkDiscovery.roomPassword = roomPassword;
        }

        finalPort = (ushort)tryGamePort;
        listenPort = tryBroadcastPort;

        networkDiscovery.roomName = roomName;
        networkDiscovery.playerName = DiscoveredServerInfo.playerName;
        networkDiscovery.serverBroadcastListenPortPortValue = listenPort;
        networkDiscovery.serverBroadcastListenPort = listenPort;
        networkDiscovery.maxPlayers = maxPlayers;

        var nm = NetworkManager.singleton;
        if (nm == null)
        {
            Debug.LogError("❌ No NetworkManager in scene!");
            return;
        }

        if (nm.transport == null)
        {
            Debug.LogError("❌ No Transport assigned on NetworkManager!");
            return;
        }

        var transport = nm.transport as TelepathyTransport;
        if (transport == null)
        {
            Debug.LogError("❌ Transport is not TelepathyTransport! Type: " + nm.transport.GetType().Name);
            return;
        }

        Debug.Log("✅ Transport found, port: " + transport.port);

        transport.port = finalPort;

        NetworkManager.singleton.StartHost();
        networkDiscovery.AdvertiseServer();

        Debug.Log($"🏠 Hosting LAN on free port {finalPort}, broadcast {listenPort}");

        createJoinManager.hostLobby.gameObject.SetActive(true);
        createJoinManager.hostLobby.playerTableManager.UpdatePlayerTable();
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
    public Coroutine discoverRoutine;

    public void CallDiscoverAllLANHosts_Unlimited()
    {
        isRoomJoined = false;
        discoverRoutine  = StartCoroutine(DiscoverAllLANHosts_Unlimited());
    }

    public IEnumerator DiscoverAllLANHosts_Unlimited()
    {
        int currentPort = baseBroadcastPort; // 47777 से शुरू
        int silenceCounter = 0;
        int silenceLimit = 15;

        Debug.Log("🌐 Starting full LAN host discovery...");

        // यह dictionary हर host का "last seen" time रखेगी
        Dictionary<string, float> lastSeenTime = new Dictionary<string, float>();

        while (silenceCounter < silenceLimit)
        {
            bool foundOnThisPort = false;
            List<DiscoveredServer> foundThisCycle = new List<DiscoveredServer>();

            networkDiscovery.OnServerFound.RemoveAllListeners();

            networkDiscovery.OnServerFound.AddListener((response) =>
            {
                if (response.uri != null)
                {
                    string ip = response.EndPoint.Address.ToString();
                    int port = response.uri.Port;
                    string key = ip + ":" + port;
                    string name = response.roomName;
                    int connectedPlayers = response.connectedPlayers;

                    var existingServer = discoveredServers.FirstOrDefault(s => s.address == ip && s.port == port);

                    if (existingServer != null)
                    {
                        // ✅ Update existing
                        existingServer.roomName = name;
                        existingServer.roomPassword = response.roomPassword;
                        existingServer.baseBroadcastPort = response.serverBroadcastListenPortPortValue;
                        existingServer.playerCount = connectedPlayers;

                        Debug.Log($"🔄 Updated host: {name} ({ip}:{port}) | Players: {connectedPlayers}");
                    }
                    else
                    {
                        // 🆕 Add new
                        var newServer = new DiscoveredServer()
                        {
                            roomName = name,
                            playerCount = connectedPlayers,
                            address = ip,
                            port = port,
                            roomPassword = response.roomPassword,
                            baseBroadcastPort = response.serverBroadcastListenPortPortValue,
                            maxPlayers = response.maxplayers,
                        };
                        discoveredServers.Add(newServer);
                        Debug.Log($"📡 Found new Host → {ip}:{port} ({name})");
                    }

                    // ⏱️ Update last seen time
                    lastSeenTime[key] = Time.time;

                    // Record for this cycle
                    if (!foundThisCycle.Any(s => s.address == ip && s.port == port))
                    {
                        foundThisCycle.Add(new DiscoveredServer() { roomName = name, address = ip, port = port });
                    }

                    foundOnThisPort = true;
                }
            });

            networkDiscovery.serverBroadcastListenPort = currentPort;
            networkDiscovery.StartDiscovery();
            Debug.Log($"🔎 Scanning LAN for hosts on broadcast port {currentPort}...");
            yield return new WaitForSeconds(0.1f);
            networkDiscovery.StopDiscovery();

            if (foundOnThisPort)
                silenceCounter = 0;
            else
                silenceCounter++;

            currentPort++;
        }

        // 🧹 Cleanup: remove hosts not seen for > 5 seconds (means host stopped broadcasting)
        float timeout = 5f;
        var toRemove = discoveredServers
            .Where(existing =>
            {
                string key = existing.address + ":" + existing.port;
                return !lastSeenTime.ContainsKey(key) || (Time.time - lastSeenTime[key] > timeout);
            })
            .ToList();

        foreach (var dead in toRemove)
        {
            discoveredServers.Remove(dead);
            Debug.Log($"❌ Host stopped broadcasting → Removed {dead.roomName} ({dead.address}:{dead.port})");
        }

        Debug.Log($"✅ Active hosts on LAN: {discoveredServers.Count}");

        // 🔄 Update UI only if not joined in room
        if (!isRoomJoined)
        {
            if (RoomTableManager.instance != null)
            {
                RoomTableManager.instance.UpdateLANRoomTableUI();
            }

            yield return new WaitForSeconds(2f);
            CallDiscoverAllLANHosts_Unlimited();
        }

        GS.Instance.DestroyPreloder();
    }


    // 📡 यह function LAN में सारे hosts ढूंढता है और result return करता है
 /*   public IEnumerator DiscoverAllLANHosts_Unlimited()
    {
        List<DiscoveredServer> foundServers = new List<DiscoveredServer>();

        discoveredServers.Clear();

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
                    string name = response.roomName;

                    if (!foundServers.Exists(s => s.port == port && s.address == ip))
                    {
                        foundServers.Add(new DiscoveredServer()
                        {
                            roomName = name,
                            roomPassword = response.roomPassword,
                            address = ip,
                            port = port
                        });


                        bool alreadyExists = discoveredServers.Any(server => server.roomName == name);


                        // Agar exist nahi karta tabhi add karo
                        if (!alreadyExists)
                        {
                            discoveredServers.Add(new DiscoveredServer()
                            {
                                roomName = name,
                                roomPassword = response.roomPassword,
                                address = ip,
                                port = port,
                                baseBroadcastPort = response.serverBroadcastListenPortPortValue,
                            });

                            Debug.Log($"✅ Added new server: {name} ({ip}:{port})");
                        }
                        else
                        {
                            Debug.Log($"⚠️ Duplicate server skipped: {name} ({ip}:{port})");
                        }
                        Debug.Log($"📡 Found Host → {ip}:{port} ({name})");
                    }

                    foundOnThisPort = true;
                }
            });

            networkDiscovery.serverBroadcastListenPort = currentPort;
            networkDiscovery.StartDiscovery();
            Debug.Log($"🔎 Scanning LAN for hosts on broadcast port {currentPort}...");
            yield return new WaitForSeconds(0.1f);
            networkDiscovery.StopDiscovery();

            if (foundOnThisPort)
                silenceCounter = 0;
            else
                silenceCounter++;

            currentPort++;
        }

        Debug.Log($"✅ Found {foundServers.Count} total hosts on LAN (scanned until port {currentPort - 1}).");



        if (!isRoomJoined)
        {
            if (RoomTableManager.instance != null)
            {
                RoomTableManager.instance.UpdateLANRoomTableUI();
            }
            yield return new WaitForSeconds(2f);
            CallDiscoverAllLANHosts_Unlimited();

        }
        GS.Instance.DestroyPreloder();
    }
*/
    public void FindGames()
    {
       StartCoroutine(isRoomisExist());
    }

    public IEnumerator isRoomisExist()
    {
        GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);


        listenPort = DiscoveredServerInfo.baseBroadcastPort;
        networkDiscovery.serverBroadcastListenPort = listenPort;

        List<DiscoveredServer> foundServers = new List<DiscoveredServer>();

        int currentPort = 47777; // 47777 से शुरू
        int silenceCounter = 0;              // लगातार खाली ports की गिनती
        int silenceLimit = 15;               // अगर 15 लगातार ports पर कुछ नहीं मिला तो scan रोक दो

        discoveredServers.Clear();
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
                    string name = response.roomName;

                    if (!foundServers.Exists(s => s.port == port && s.address == ip))
                    {
                        foundServers.Add(new DiscoveredServer()
                        {
                            roomName = name,
                            roomPassword = response.roomPassword,
                            address = ip,
                            port = port
                        });


                        Debug.Log($"📡 Found Host → {ip}:{port} ({name})");
                    }

                    foundOnThisPort = true;

                    if (port == DiscoveredServerInfo.port)
                    {
                        if (networkDiscovery.didStart)
                        {
                            networkDiscovery.StopDiscovery();
                        }
                        GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);

                        networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
                        isRoomJoined = true;
                        listenPort = DiscoveredServerInfo.baseBroadcastPort;
                        networkDiscovery.serverBroadcastListenPort = listenPort;

                        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
                        transport.port = (ushort)DiscoveredServerInfo.port;

                        Debug.Log($"🔎 Searching for LAN hosts (game={transport.port}, broadcast={listenPort})...");
                        networkDiscovery.StartDiscovery();
                        StopRoomFindCoroutine();
                    }
                    else
                    {
                        Debug.Log("Room not exist in this port : " + port);
                    }
                }
            });


            networkDiscovery.serverBroadcastListenPort = currentPort;
            networkDiscovery.StartDiscovery();

            Debug.Log($"🔎 Scanning LAN for hosts on broadcast port {currentPort}...");

            yield return new WaitForSeconds(0.1f);

            networkDiscovery.StopDiscovery();

            if (foundOnThisPort)
                silenceCounter = 0;
            else
                silenceCounter++;

            currentPort++;

        }
        Debug.Log($"✅ Found {foundServers.Count} total hosts on LAN (scanned until port {currentPort - 1}).");
    }

    public void StopRoomFindCoroutine()
    {
        if (discoverRoutine != null)
        {
            StopCoroutine(discoverRoutine);
            discoverRoutine = null;
            Debug.Log("🛑 LAN host discovery stopped.");
        }
    }

    internal bool isConnected = false;

    void OnDiscoveredServer(ServerResponse info)
    {
        if (isConnected) return; // Already joined

        createJoinManager.clientLobby.gameObject.SetActive(true);

        string hostAddress = info.EndPoint.Address.ToString();
        Debug.Log($"✅ Found host: {hostAddress} | URI: {info.uri}");

        NetworkManager.singleton.networkAddress = hostAddress;

        // Try connecting automatically
        NetworkManager.singleton.StartClient();

        isConnected = true;
        Debug.Log("🚀 Auto-joining host...");
        int count = NetworkServer.connections.Count;
        Debug.Log($"👥 Players connected: {count}");
    }
   
}