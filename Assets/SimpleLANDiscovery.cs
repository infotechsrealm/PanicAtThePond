using Mirror.Discovery;
using System.Collections.Generic;
using UnityEngine;

public class SimpleLANDiscovery : MonoBehaviour
{
    public NetworkDiscovery networkDiscovery;

    // List of discovered servers
    private List<ServerResponse> discoveredServers = new List<ServerResponse>();

    void Start()
    {
        // Subscribe to server found event
        networkDiscovery.OnServerFound.AddListener(OnServerFound);
    }

    void OnDestroy()
    {
        networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
    }

    void OnServerFound(ServerResponse info)
    {
        // Avoid duplicates
        if (!discoveredServers.Exists(s => s.uri == info.uri))
        {
            discoveredServers.Add(info);
            Debug.Log($"Found server at {info.EndPoint.Address}:{info.uri.Port}");
        }
    }

    public void StartDiscovery()
    {
        discoveredServers.Clear();
        networkDiscovery.StartDiscovery();
        Debug.Log("Started LAN discovery...");
    }

    public void StopDiscovery()
    {
        networkDiscovery.StopDiscovery();
        Debug.Log("Stopped LAN discovery.");
    }

    void OnGUI()
    {
        int y = 10;
        foreach (var server in discoveredServers)
        {
            GUI.Label(new Rect(10, y, 400, 20), $"Server: {server.EndPoint.Address}:{server.uri.Port}");
            y += 25;
        }

        if (GUI.Button(new Rect(10, y, 150, 30), "Start Discovery"))
        {
            StartDiscovery();
        }
        y += 40;
        if (GUI.Button(new Rect(10, y, 150, 30), "Stop Discovery"))
        {
            StopDiscovery();
        }
    }
}