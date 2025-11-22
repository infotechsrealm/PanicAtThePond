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
        roomName = "";
        address = "";
        port = 0;
        baseBroadcastPort = 0;
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
    public NetworkManager networkManager;

    private ushort finalPort;
    private int listenPort;

    public ushort baseGamePort;
    public int baseBroadcastPort;

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
        roomPasswordInputError,
        noRoomExistError;

    [Header("Room Settings")]
    public int maxPlayers;

    public bool isRoomJoined = false;

    void Awake()
    {
        Instance = this;
        finalPort = baseGamePort;
        listenPort = baseBroadcastPort;
        DiscoveredServerInfo.playerName = GS.Instance.nickName;

    }

    private void Start()
    {
        networkManager = GS.Instance.networkManager;
        networkDiscovery = GS.Instance.networkDiscovery;
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

        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

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
            //  FindFreePortAndHost();
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
        int maxPort = 7792;                      // <-- LIMIT

        Debug.Log("🔍 Checking for free LAN game port...");

        while (true)
        {
            // ❌ If we cross the limit → stop searching
            if (tryGamePort >= maxPort)
            {
                createRoomNameError.text = "All rooms are full";
                GS.Instance.DestroyPreloder();
                Debug.Log($"❌ all rooms is full  {tryGamePort} and {maxPort}!");
                return;
            }
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

        networkDiscovery.serverBroadcastListenPort = listenPort;

        networkDiscovery.roomName = roomName;
        networkDiscovery.playerName = DiscoveredServerInfo.playerName;
        networkDiscovery.serverBroadcastListenPortPortValue = listenPort;
        networkDiscovery.maxPlayers = maxPlayers;

        DiscoveredServerInfo.maxPlayers = maxPlayers;


        var nm = networkManager;

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

        networkManager.StartHost();
        networkDiscovery.AdvertiseServer();

        Debug.Log($"🏠 Hosting LAN on free port {finalPort}, broadcast {listenPort}");

        createJoinManager.hostLobby.gameObject.SetActive(true);
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
        isRoomJoined  = false;
        discoverRoutine  = StartCoroutine(DiscoverAllLANHosts_Unlimited());
    }

    public  bool isDiscovering = false;

    IEnumerator DiscoverAllLANHosts_Unlimited()
    {
        if (isRoomJoined)
            yield break;

        if (isDiscovering)
            yield break;

        isDiscovering = true;

        int currentPort = baseBroadcastPort;
        int silenceCounter = 0;
        int silenceLimit = 15;

        Debug.Log("🌐 Starting full LAN host discovery...");

        // ❗ IMPORTANT: REMOVE THIS LINE
        // discoveredServers.Clear();  // ❌ अब कभी clear नहीं करेंगे

        while (silenceCounter < silenceLimit && !isRoomJoined)
        {
            bool foundOnThisPort = false;

            networkDiscovery.OnServerFound.RemoveAllListeners();

            networkDiscovery.OnServerFound.AddListener((response) =>
            {
                if (response.uri != null)
                {
                    foundOnThisPort = true;

                    string ip = response.EndPoint.Address.ToString();
                    int port = response.uri.Port;
                    string key = ip + ":" + port;

                    string name = response.roomName;
                    int connectedPlayers = response.connectedPlayers;

                    // 🔍 check if this server exists
                    var existing = discoveredServers
                        .FirstOrDefault(s => s.address == ip && s.port == port);

                    if (existing != null)
                    {
                        // 🟢 UPDATE existing entry
                        existing.roomName = name;
                        existing.playerCount = connectedPlayers;
                        existing.roomPassword = response.roomPassword;
                        existing.baseBroadcastPort = response.serverBroadcastListenPortPortValue;
                        existing.maxPlayers = response.maxPlayers;

                        Debug.Log($"🔄 Updated Host → {ip}:{port} ({name})");
                    }
                    else
                    {
                        // 🟢 ADD new entry
                        var newServer = new DiscoveredServer()
                        {
                            roomName = name,
                            playerCount = connectedPlayers,
                            address = ip,
                            port = port,
                            roomPassword = response.roomPassword,
                            baseBroadcastPort = response.serverBroadcastListenPortPortValue,
                            maxPlayers = response.maxPlayers
                        };

                        discoveredServers.Add(newServer);

                        Debug.Log($"🆕 Added Host → {ip}:{port} ({name})");
                    }

                }
            });

            if (!foundOnThisPort)
            {
                for (int i = 0; i < discoveredServers.Count; i++)
                {
                    Debug.Log("❌ currentPort" + currentPort + " discoveredServers[i].port =  " + discoveredServers[i].baseBroadcastPort);
                    if (discoveredServers[i].baseBroadcastPort == currentPort)
                    {
                        Debug.Log(currentPort + "is Not Exist in LAN");
                        discoveredServers.Remove(discoveredServers[i]);
                    }
                }
            }

            networkDiscovery.serverBroadcastListenPort = currentPort;
            networkDiscovery.StartDiscovery();
            Debug.Log($"🔎 Scanning on broadcast port {currentPort}...");
            yield return new WaitForSeconds(0.1f);
            networkDiscovery.StopDiscovery();

            silenceCounter = foundOnThisPort ? 0 : silenceCounter + 1;

            currentPort++;
        }

        Debug.Log($"⭐ Total Hosts in memory: {discoveredServers.Count}");

        if (RoomTableManager.instance != null)
        {
            RoomTableManager.instance.UpdateRoomTable();
        }

        yield return new WaitForSeconds(1f);

        isDiscovering = false;

        if (!isRoomJoined)
            CallDiscoverAllLANHosts_Unlimited();
    }


    // 📡 यह function LAN में सारे hosts ढूंढता है और result return करता है
    public void FindGames()
    {
        StartCoroutine(isRoomisExist());
    }

    public bool isRoomExist = false;

    public IEnumerator isRoomisExist()
    {
        isRoomExist = false;
        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

        listenPort = DiscoveredServerInfo.baseBroadcastPort;
        networkDiscovery.serverBroadcastListenPort = listenPort;
        noRoomExistError.text = "";

        int currentPort = 47777; // 47777 से शुरू
        int silenceCounter = 0;              // लगातार खाली ports की गिनती
        int silenceLimit = 15;               // अगर 15 लगातार ports पर कुछ नहीं मिला तो scan रोक दो

        discoveredServers.Clear();
        Debug.Log("🌐 Starting full LAN host discovery...");


        while (silenceCounter < silenceLimit && !isRoomExist)
        {
            bool foundOnThisPort = false;

            networkDiscovery.OnServerFound.RemoveAllListeners();

            networkDiscovery.OnServerFound.AddListener((response) =>
            {
                if (response.uri != null)
                {
                    int port = response.uri.Port;
                    int connectedPlayers = response.connectedPlayers;

                    foundOnThisPort = true;

                    if (port == DiscoveredServerInfo.port)
                    {
                        if (connectedPlayers < DiscoveredServerInfo.maxPlayers)
                        {
                            if (DiscoveredServerInfo.baseBroadcastPort != 0 && DiscoveredServerInfo.port != 0)
                            {
                                string password = DiscoveredServerInfo.roomPassword ?? "";
                                Debug.Log("DiscoveredServerInfo.roomPassword = " + DiscoveredServerInfo.roomPassword);
                                isRoomExist = true;
                                StopRoomFindCoroutine();

                                if (string.IsNullOrEmpty(password))
                                {
                                    JoinRoom();
                                }
                                else
                                {
                                    GameObject popup = Instantiate(GS.Instance.passwordPopupPrefab, createJoinManager.transform);
                                    popup.GetComponent<PasswordPopup>().correctPassword = DiscoveredServerInfo.roomPassword;
                                }
                            }
                        }
                        else
                        {
                            //noRoomExistError.text = "room is full.";
                        }
                    }
                }
            });

            if (!isRoomExist)
            {
                networkDiscovery.serverBroadcastListenPort = currentPort;
                networkDiscovery.StartDiscovery();


                yield return new WaitForSeconds(0.1f);

                networkDiscovery.StopDiscovery();

                if (foundOnThisPort)
                    silenceCounter = 0;
                else
                    silenceCounter++;

                currentPort++;
            }
            else
            {
                Debug.Log("room is  exist adfffff vgbhsjmk,lukyrbetvwRFTGEYHJKLUFREW --------------------");
            }

        }
        if(!isRoomExist)
        {
            Debug.Log($" ********************** room is NOT exist Scanning LAN for hosts on broadcast port {currentPort}...");
            noRoomExistError.text = "room joining is faild , Please try again.";
        }
    }
    

    public void JoinRoom()
    {
        GS.Instance.totlePlayers = DiscoveredServerInfo.maxPlayers;
        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);
        listenPort = DiscoveredServerInfo.baseBroadcastPort;
        networkDiscovery.serverBroadcastListenPort = listenPort;

        var transport = (TelepathyTransport)networkManager.transport;
        transport.port = (ushort)DiscoveredServerInfo.port;

        Debug.Log($"🔎 Searching for LAN hosts (game={transport.port}, broadcast={listenPort})...");
        networkDiscovery.StartDiscovery();
        networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
        RoomTableManager.instance.ResetTable();
    }
    
    public void StopRoomFindCoroutine()
    {
        if (discoverRoutine != null)
        {
            Debug.Log("🛑 LAN host discovery stopped.");
            isRoomJoined = true;
            isDiscovering = false;
            StopCoroutine(discoverRoutine);
            discoveredServers.Clear();
            discoverRoutine = null;
        }
    }


    public bool isConnected = false;

    void OnDiscoveredServer(ServerResponse info)
    {
        if (isConnected)
        {
            Debug.Log("⚠️ Already connected to a host, ignoring discovered server.");
            return; // Already joined
        }

        createJoinManager.clientLobby.gameObject.SetActive(true);

        string hostAddress = info.EndPoint.Address.ToString();
        Debug.Log($"✅ Found host: {hostAddress} | URI: {info.uri}");

        networkManager.networkAddress = hostAddress;

        // Try connecting automatically
        networkManager.StartClient();

        isConnected = true;
        Debug.Log("🚀 Auto-joining host...");
        int count = NetworkServer.connections.Count;
        Debug.Log($"👥 Players connected: {count}");
    }
}