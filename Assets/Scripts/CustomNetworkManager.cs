using Mirror;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public struct PlayerNameMessage : NetworkMessage
{
    public string playerName;
}

public struct PlayerListMessage : NetworkMessage
{
    public List<string> allPlayerNames;
}

public struct VisibilityMessage : NetworkMessage
{
    public bool reflectiveWater;
    public bool deepWaters;
    public bool murkyWaters;
    public bool clearWaters;
}

public class CustomNetworkManager : NetworkManager
{
    private Dictionary<int, string> playerNames = new Dictionary<int, string>();

    public string localPlayerName = "Rajan";

    public static CustomNetworkManager Instence;

    public DropdownHandler DropdownHandler;

    private void Awake()
    {
        Instence = this;
    }

    // 🔹 SERVER START पर message handler register करो
    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PlayerNameMessage>(OnReceivePlayerName);
        if (NetworkClient.active)
        {
            NetworkClient.RegisterHandler<VisibilityMessage>(OnClientReceive);
        }
    }

    // 🔹 CLIENT START पर PlayerListMessage receive handler register करो
    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<PlayerListMessage>(OnReceivePlayerList);
        if (NetworkClient.active)
        {
            NetworkClient.RegisterHandler<VisibilityMessage>(OnClientReceive);
        }
    }

    // ----------- CLIENT RECEIVE -----------
    private void OnClientReceive(VisibilityMessage msg)
    {
        Debug.Log("[Client] Visibility data received!");

        SetVisibility(msg.reflectiveWater, msg.deepWaters, msg.murkyWaters, msg.clearWaters);

    }

    void CallUIRefreshScript()
    {
        var handler = FindAnyObjectByType<GS>();
        if (handler != null)
        {
            handler.rerfeshDropDown();   // No freeze, safe!
        }
    }


    public void SetVisibility(bool reflectiveWater, bool deepWaters, bool murkyWaters, bool clearWaters)
    {
        GS gsObj = GS.Instance;

        gsObj.ClearWaters = clearWaters;
        gsObj.MurkyWaters = murkyWaters;
        gsObj.DeepWaters = deepWaters;
        gsObj.ReflectiveWater = reflectiveWater;

        Debug.Log($"[GS] Visibility updated: All={reflectiveWater}, Deep={deepWaters}, Murky={murkyWaters}, Clear={clearWaters}");
        UnityThread.MainThread.Post(_ =>
        {
            // Yaha UI safe chalega
            CallUIRefreshScript();
        }, null);
    }
   
    // 🔹 जब client connect करे तो अपना नाम भेज`
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        NetworkClient.Send(new PlayerNameMessage
        {
            playerName = GS.Instance.nickName,
        });

        LANDiscoveryMenu.Instance.StopRoomFindCoroutine();
        Debug.Log($"✅ Connected to server, name sent: {GS.Instance.nickName}");
    }

    // 🔹 Server पर जब किसी का नाम आए
    void OnReceivePlayerName(NetworkConnectionToClient conn, PlayerNameMessage msg)
    {
        playerNames[conn.connectionId] = msg.playerName;
        Debug.Log($"🧩 Player joined: {msg.playerName} ({conn.address})");

        CallBroadcastVisibility();
        // सभी clients को updated list भेजो
        SendUpdatedPlayerListToAll();
    }

    public void CallBroadcastVisibility()
    {
        if (GS.Instance.isLan)
        {
            if (GS.Instance.IsMirrorMasterClient)
            {
                BroadcastVisibility(GS.Instance.ReflectiveWater, GS.Instance.DeepWaters, GS.Instance.MurkyWaters, GS.Instance.ClearWaters);
            }
        }
    }

    // ----------- SERVER SEND (CALL THIS) -----------
    public void BroadcastVisibility(bool reflective, bool deep, bool murky, bool clear)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("BroadcastVisibility called but server is not active!");
            return;
        }

        VisibilityMessage msg = new VisibilityMessage
        {
            reflectiveWater = reflective,
            deepWaters = deep,
            murkyWaters = murky,
            clearWaters = clear
        };

        // Send to ALL clients
        NetworkServer.SendToAll(msg);

        Debug.Log("[Server] Visibility broadcasted to all clients.");

    }

    // 🔹 जब कोई disconnect करे
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {

        Debug.Log("& Client disconnected or connection lost.");

        if (playerNames.ContainsKey(conn.connectionId))
        {
            Debug.Log($"❌ Player left: {playerNames[conn.connectionId]}" +"is dead =" + conn.isDead);
            playerNames.Remove(conn.connectionId);
        }


        if (FishController.Instance != null)
        {
            FishController myFish = FishController.Instance;
            FishController oppFish = conn.identity.GetComponent<FishController>();

            if (myFish.isFisherMan)
            {

                Debug.Log("oppFish => " + oppFish.isDead);

                if (!oppFish.isDead)
                {
                    GameManager.Instance.LessPlayerCount_Mirror();
                }
            }
            else
            {
                if (!oppFish.isDead)
                {
                    myFish.fishController_Mirror.CallLessPlayerCount_Mirror();
                }
            }
        }

        if (FishController.Instance != null)
        {
            FishController myFish = FishController.Instance;
            if (myFish != null)
            {
                int curruntPlayer = NetworkServer.connections.Count;
                Debug.Log("curruntPlayer ===>" + curruntPlayer);
                if (curruntPlayer <= 1)
                {
                    if (myFish.isFisherMan)
                    {
                        FishermanController.Instance.CheckWorms();
                    }
                    else
                    {
                        myFish.WinFish_mirror();
                    }
                }
            }
        }

        base.OnServerDisconnect(conn);

        // सभी को अपडेट भेजो
        SendUpdatedPlayerListToAll();
    }

    // 🔹 Server सभी connected clients को नामों की list भेजे
    void SendUpdatedPlayerListToAll()
    {
        var allNames = new List<string>(playerNames.Values);

        var msg = new PlayerListMessage
        {
            allPlayerNames = allNames
        };

        NetworkServer.SendToAll(msg);

        // Server पर print भी करो
        PrintAllPlayers_Server();
    }

   

    // 🔹 Server side पर players print
    void PrintAllPlayers_Server()
    {
        Debug.Log("📜 --- Server: Connected Players ---");
        if (playerNames.Count == 0)
        {
            Debug.Log("⚠️ कोई भी player connected नहीं है।");
            return;
        }

        foreach (var kvp in playerNames)
        {
            int id = kvp.Key;
            string name = kvp.Value;

            if (NetworkServer.connections.TryGetValue(id, out NetworkConnectionToClient conn))
                Debug.Log($"👤 {name} → {conn.address}");
            else
                Debug.Log($"👤 {name} → (missing connection)");
        }

    }

    // 🔹 Client side पर जब list मिले
    void OnReceivePlayerList(PlayerListMessage msg)
    {
        Debug.Log("📜 --- Client: Connected Players ---");
        if (msg.allPlayerNames.Count == 0)
        {
            Debug.Log("⚠️ कोई भी player connected नहीं है।");
        }
        else
        {
            foreach (var name in msg.allPlayerNames)
            {
                Debug.Log($"👤 {name}");
            }

            if (PlayerTableManager.Instance != null)
            {
                PlayerTableManager.Instance.players = msg.allPlayerNames;
                PlayerTableManager.Instance.UpdatePlayerTable();
            }
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        Debug.Log("🚨 Host disconnected or connection lost.");

        if (LANDiscoveryMenu.Instance != null)
        {
            if (CreateJoinManager.Instance.isJoining)
            {
                if (PlayerTableManager.Instance != null)
                {
                    PlayerTableManager.Instance.players.Clear();
                    PlayerTableManager.Instance.UpdatePlayerTable();
                }

                LANDiscoveryMenu.Instance.isConnected = false;

                LANDiscoveryMenu.Instance.CallDiscoverAllLANHosts_Unlimited();
                CreateJoinManager.Instance.clientLobby.gameObject.SetActive(false);
            }
        }


        if (FishController.Instance != null)
        {
            GameManager.Instance.ShowGameOver("GameOver!");
        }

    }
 

    [Server]
    public void LoadPlaySceneForAll()
    {
        Debug.Log("🔁 Loading play scene for all clients...");
        ServerChangeScene("Play");
    }
}
