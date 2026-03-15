using Mirror;
using System.Collections.Generic;
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

public struct GameModeMessage : NetworkMessage
{
    public int gameMode;
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

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PlayerNameMessage>(OnReceivePlayerName);
        if (NetworkClient.active)
        {
            NetworkClient.RegisterHandler<VisibilityMessage>(OnClientReceive);
            NetworkClient.RegisterHandler<GameModeMessage>(OnReceiveGameMode_Client);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<PlayerListMessage>(OnReceivePlayerList);
        if (NetworkClient.active)
        {
            NetworkClient.RegisterHandler<VisibilityMessage>(OnClientReceive);
            NetworkClient.RegisterHandler<GameModeMessage>(OnReceiveGameMode_Client);
        }
    }

    // ----------- CLIENT RECEIVE -----------
    private void OnClientReceive(VisibilityMessage msg)
    {

        SetVisibility(msg.reflectiveWater, msg.deepWaters, msg.murkyWaters, msg.clearWaters);

    }

    public void SetVisibility(bool reflectiveWater, bool deepWaters, bool murkyWaters, bool clearWaters)
    {
        GS gsObj = GS.Instance;

        gsObj.ClearWaters = clearWaters;
        gsObj.MurkyWaters = murkyWaters;
        gsObj.DeepWaters = deepWaters;
        gsObj.ReflectiveWater = reflectiveWater;

        gsObj.dropDownChangeAvalable = true;
    }
   
    private void OnReceiveGameMode_Client(GameModeMessage msg)
    {
        GS.Instance.currentGameMode = msg.gameMode;
        if (GameModeDropdownHandler.Instance != null && GameModeDropdownHandler.Instance.gameModeDropdown != null)
        {
            GameModeDropdownHandler.Instance.gameModeDropdown.value = msg.gameMode;
        }
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
        
        // As a client, fetch the latest UI state from the connected host
        if (!GS.Instance.IsMirrorMasterClient)
        {
            if (GameModeDropdownHandler.Instance != null && GameModeDropdownHandler.Instance.gameModeDropdown != null)
            {
                GameModeDropdownHandler.Instance.gameModeDropdown.interactable = false; // Disable local modification for clients
            }
        }
    }

    void OnReceivePlayerName(NetworkConnectionToClient conn, PlayerNameMessage msg)
    {
        playerNames[conn.connectionId] = msg.playerName;
        Debug.Log($"🧩 Player joined: {msg.playerName} ({conn.address})");

        CallBroadcastVisibility();
        // सभी clients को updated list भेजो
        SendUpdatedPlayerListToAll();

        // Send current game mode and visibility to the newly joined client specifically
        if (GS.Instance.isLan && GS.Instance.IsMirrorMasterClient)
        {
            VisibilityMessage visMsg = new VisibilityMessage
            {
                reflectiveWater = GS.Instance.ReflectiveWater,
                deepWaters = GS.Instance.DeepWaters,
                murkyWaters = GS.Instance.MurkyWaters,
                clearWaters = GS.Instance.ClearWaters
            };
            conn.Send(visMsg);

            GameModeMessage modeMsg = new GameModeMessage { gameMode = GS.Instance.currentGameMode };
            conn.Send(modeMsg);
        }
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

    public void CallBroadcastGameMode()
    {
        if (GS.Instance.isLan && GS.Instance.IsMirrorMasterClient)
        {
            BroadcastGameMode(GS.Instance.currentGameMode);
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

    public void BroadcastGameMode(int mode)
    {
        if (!NetworkServer.active) return;
        GameModeMessage msg = new GameModeMessage { gameMode = mode };
        NetworkServer.SendToAll(msg);
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
    public void SendUpdatedPlayerListToAll()
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
       // Debug.Log("📜 --- Client: Connected Players ---");
        if (msg.allPlayerNames.Count == 0)
        {
            Debug.Log("⚠️ कोई भी player connected नहीं है।");
        }
        else
        {
            if (PlayerTableManager.Instance != null)
            {
                PlayerTableManager.Instance.players = msg.allPlayerNames;
                PlayerTableManager.Instance.UpdatePlayerTable();
            }
            
            if (GS.Instance != null)
            {
                foreach (var pName in msg.allPlayerNames)
                {
                    if (!GS.Instance.playerScores.ContainsKey(pName))
                    {
                        GS.Instance.playerScores[pName] = 0;
                    }
                }
            }
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        playerNames.Clear();
        SendUpdatedPlayerListToAll();
        Debug.Log("🛑 Server stopped, clearing all player names.");
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
