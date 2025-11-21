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

public class CustomNetworkManager : NetworkManager
{
    private Dictionary<int, string> playerNames = new Dictionary<int, string>();

    public string localPlayerName = "Rajan";

    public static CustomNetworkManager Instence;



    private void Awake()
    {
        Instence = this;
    }


    // 🔹 SERVER START पर message handler register करो
    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PlayerNameMessage>(OnReceivePlayerName);
    }

    // 🔹 CLIENT START पर PlayerListMessage receive handler register करो
    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<PlayerListMessage>(OnReceivePlayerList);
    }

    // 🔹 जब client connect करे तो अपना नाम भेजो
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

        // सभी clients को updated list भेजो
        SendUpdatedPlayerListToAll();
    }

    // 🔹 जब कोई disconnect करे
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (playerNames.ContainsKey(conn.connectionId))
        {
            Debug.Log($"❌ Player left: {playerNames[conn.connectionId]}");
            playerNames.Remove(conn.connectionId);
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

    }
 

    [Server]
    public void LoadPlaySceneForAll()
    {
        Debug.Log("🔁 Loading play scene for all clients...");
        ServerChangeScene("Play");
    }

}
