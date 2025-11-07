using Mirror;
using System.Linq;
using System.Net;
using UnityEngine;

public class CustomNetworkManager : NetworkManager
{
    public int connectedPlayers = 0;

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"🟢 Client connected from: {conn.address}");
        PrintAllConnections();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        string serverIP = networkAddress;
        string localIP = GetLocalIPAddress();
        Debug.Log($"🧩 Client connected to server: {serverIP}");
        Debug.Log($"💻 My local IP: {localIP}");
    }

    void PrintAllConnections()
    {
        foreach (var kvp in NetworkServer.connections)
        {
            Debug.Log($"🌐 Connected client: {kvp.Value.address}");
        }

        Debug.Log($"🖥️ Server local IP: {GetLocalIPAddress()}");
    }

    string GetLocalIPAddress()
    {
        return Dns.GetHostEntry(Dns.GetHostName())
            .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?
            .ToString();
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
