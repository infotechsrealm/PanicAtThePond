using Mirror;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public int connectedPlayers = 0;
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        connectedPlayers++;
        Debug.Log($"🔵 Player connected. Total players: {numPlayers}");


        if (NetworkServer.active)
        {
            int playerCount = NetworkServer.connections.Count;
            Debug.Log("🧑‍🤝‍🧑 Connected Players: " + playerCount);
        }

        if (connectedPlayers >= 2)
        {
            Debug.Log("Lobby full — spawning players...");
            SpawnPlayers();
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        connectedPlayers--;
        Debug.Log($"🔴 Player disconnected. Remaining players: {numPlayers}");
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
