using Mirror;
using Mirror.Discovery;
using System.Collections;
using System.Collections.Generic;
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

    public DiscoveredServer()
    {
        roomName = "abc";
        address = "";
        port = 7777;
        baseBroadcastPort = 47777;
        roomPassword = "";
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
    //public InputField roomPasswordInput;

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

    void Awake()
    {
        Instance = this;
        finalPort = baseGamePort;
        listenPort = baseBroadcastPort;
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


        createRoomNameError.text = "";

        if (Preloader.instance == null)
        {
            Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
        }

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

                        if (Preloader.instance!=null)
                        {
                            Destroy(Preloader.instance.gameObject);
                        }
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
        
        networkDiscovery.roomName = roomName;

        finalPort = (ushort)tryGamePort;
        listenPort = tryBroadcastPort;

        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port = finalPort;

        NetworkManager.singleton.StartHost();

        networkDiscovery.serverBroadcastListenPort = listenPort;
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

    public void CallDiscoverAllLANHosts_Unlimited()
    {
        StartCoroutine(DiscoverAllLANHosts_Unlimited());
    }

    // 📡 यह function LAN में सारे hosts ढूंढता है और result return करता है
    public IEnumerator DiscoverAllLANHosts_Unlimited()
    {
        List<DiscoveredServer> foundServers = new List<DiscoveredServer>();

        int currentPort = baseBroadcastPort; // 47777 से शुरू
        int silenceCounter = 0;              // लगातार खाली ports की गिनती
        int silenceLimit = 15;               // अगर 15 लगातार ports पर कुछ नहीं मिला तो scan रोक दो

        discoveredServers.Clear();

        Debug.Log("🌐 Starting full LAN host discovery...");

        discoveredServers.Clear();


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


                        discoveredServers.Add(new DiscoveredServer()
                        {
                            roomName = name,
                            roomPassword = response.roomPassword,
                            address = ip,
                            port = port,
                            baseBroadcastPort = currentPort,
                        });
                        Debug.Log($"📡 Found Host → {ip}:{port} ({name})");
                    }

                    foundOnThisPort = true;
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

        Debug.Log($"✅ Found {foundServers.Count} total hosts on LAN (scanned until port {currentPort - 1}).");

        if(RoomTableManager.instance!=null)
        {
            RoomTableManager.instance.UpdateLANRoomTableUI();
        }

        // Restart scanning after a delay
        yield return new WaitForSeconds(2f);
        CallDiscoverAllLANHosts_Unlimited();
    }

    public void FindGames()
    {
        networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);

        listenPort = DiscoveredServerInfo.baseBroadcastPort;
        networkDiscovery.serverBroadcastListenPort = listenPort;

        var transport = (TelepathyTransport)NetworkManager.singleton.transport;
        transport.port =(ushort)DiscoveredServerInfo.port;

        Debug.Log($"🔎 Searching for LAN hosts (game={transport.port}, broadcast={listenPort})...");
        networkDiscovery.StartDiscovery();
    }

    private bool isConnected = false;


    void OnDiscoveredServer(ServerResponse info)
    {
        if (isConnected) return; // Already joined

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