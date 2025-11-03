using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MyNetworkManager : NetworkManager
{
    public static MyNetworkManager Instance;

    public int connectedPlayers = 0, playerLimmit = 0;

    public GameObject clientLobby, hostLobby;
    public Text playerLimmitText, passwordText;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        connectedPlayers++;
        Debug.Log($"Player connected: {connectedPlayers}");

        if (connectedPlayers >= 2)
        {
            Debug.Log("Lobby full — spawning players...");
            SpawnPlayers();
            LANConnector.Instence.enableUdpAnnounce = false;
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        connectedPlayers--;
    }

    void SpawnPlayers()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null)
            {
                Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
                GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                NetworkServer.AddPlayerForConnection(conn, player);
            }
        }
    }
}
