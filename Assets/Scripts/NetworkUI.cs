using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    public NetworkManager networkManager;
    public Button hostButton;
    public Button clientButton;

    [Header("UI References")]
    public Text playersListText;
    public Text waitingText;
    public GameObject waitingPanel;

    [Header("Spawning (assign in Inspector)")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    internal int playersRequired = 3;
    private bool playersSpawned = false;
    private bool playSceneLoaded = false;
    private bool gameStarted = false; // नया flag: game start हो चुका है या नहीं
    public static NetworkUI instence;

    private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (instence == null)
        {
            instence = this;
        }
    }

    private void OnEnable()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
        }
    }

    private void OnDisable()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
        }
    }

    public void Update()
    {
       
    }
    public void StartHost()
    {
        // Maximum connections limit सेट करें (playersRequired + 1 host को include करने के लिए)
        networkManager.TransportManager.Transport.SetMaximumClients(playersRequired + 1);

        networkManager.ServerManager.StartConnection();
        StartCoroutine(CheckServerStarted());

        if (hostButton != null) hostButton.interactable = false;
        if (clientButton != null) clientButton.interactable = false;
        if (waitingPanel != null) waitingPanel.SetActive(true);
    }

    public void StartClient()
    {
        networkManager.ClientManager.StartConnection();

        if (hostButton != null) hostButton.interactable = false;
        if (clientButton != null) clientButton.interactable = false;
        if (waitingPanel != null) waitingPanel.SetActive(true);
    }

    private System.Collections.IEnumerator CheckServerStarted()
    {
        while (!networkManager.ServerManager.Started)
            yield return null;

        Debug.Log("✅ Server successfully started!");
        networkManager.ClientManager.StartConnection();
        StartCoroutine(CheckHostStarted());
    }

    private System.Collections.IEnumerator CheckHostStarted()
    {
        yield return null;
        if (networkManager.ServerManager.Started)
        {
            Debug.Log("Host successfully started!");
            if (waitingText != null)
                waitingText.text = $"Waiting for players... ({networkManager.ServerManager.Clients.Count}/{playersRequired})";
        }
    }

    private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // अगर game already start हो चुका है तो नए connection को reject करें
            if (gameStarted)
            {
                Debug.Log($"Game already started. Rejecting new connection (id={connection.ClientId})");
                connection.Disconnect(true);
                return;
            }

            Debug.Log($"Client connected (id={connection.ClientId}). Total connections: {networkManager.ServerManager.Clients.Count}");
            UpdatePlayerListUI();
            if (waitingText != null)
                waitingText.text = $"Waiting for players... ({networkManager.ServerManager.Clients.Count}/{playersRequired})";

            TryLoadPlayScene();
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            Debug.Log($"Client disconnected (id={connection.ClientId}).");

            // जब कोई क्लाइंट डिस्कनेक्ट हो तो उसके प्लेयर ऑब्जेक्ट को Dictionary से हटाएं और Destroy करें
            if (playerObjects.ContainsKey(connection.ClientId))
            {
                Destroy(playerObjects[connection.ClientId]);
                playerObjects.Remove(connection.ClientId);
            }

            UpdatePlayerListUI();

            // अगर game start हो चुका है तो waiting text update नहीं करें
            if (!gameStarted && waitingText != null && networkManager.ServerManager.Started)
                waitingText.text = $"Waiting for players... ({networkManager.ServerManager.Clients.Count}/{playersRequired})";
        }
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        if (args.LoadedScenes[0].name == "Play")
        {
            Debug.Log("Play scene loaded completely for all clients");
            gameStarted = true; // Game start हो गया
            TrySpawnPlayers();
        }
    }

    private void TryLoadPlayScene()
    {
        if (playSceneLoaded || gameStarted) return;

        int connectedClients = networkManager.ServerManager.Clients.Count;
        Debug.Log($"Checking scene load readiness: connectedClients={connectedClients}, required={playersRequired}");

        if (networkManager.ServerManager.Started && connectedClients >= playersRequired)
        {
            playSceneLoaded = true;
            SceneLoadData sld = new SceneLoadData("Play")
            {
                ReplaceScenes = ReplaceOption.All
            };
            networkManager.SceneManager.LoadGlobalScenes(sld);

            if (waitingPanel != null)
                waitingPanel.SetActive(false);
        }
    }

    private void TrySpawnPlayers()
    {
        if (playersSpawned) return;

        Debug.Log("Attempting to spawn players for all connections");

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned in NetworkUI.");
            return;
        }

        Debug.Log("✅ All players in Play scene. Spawning players...");

        foreach (var kv in networkManager.ServerManager.Clients)
        {
            NetworkConnection conn = kv.Value;

            // Random position और rotation generate करें
            Vector3 randomPos = GetRandomSpawnPosition();
            GameObject go = Instantiate(playerPrefab, randomPos, Quaternion.Euler(0, 0, 0));
            networkManager.ServerManager.Spawn(go, conn);

            // प्लेयर के ऑब्जेक्ट को हमारे Dictionary में Store करें
            playerObjects[conn.ClientId] = go;

            Debug.Log($"Spawned player for client {conn.ClientId} at random position {randomPos}");
        }

        playersSpawned = true;
        Debug.Log("🎮 All players spawned via ServerManager.Spawn()");
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float x = UnityEngine.Random.Range(-8f, 8);
        float y = UnityEngine.Random.Range(0, -4);
        return new Vector3(x, y);
    }

    private void UpdatePlayerListUI()
    {
        if (playersListText == null) return;

        string list = "Connected Players:\n";
        foreach (var kv in networkManager.ServerManager.Clients)
        {
            NetworkConnection conn = kv.Value;
            list += $"Player {conn.ClientId}\n";
        }
        playersListText.text = list;
    }

    public GameObject GetPlayerByConnectionId(int connectionId)
    {
        if (playerObjects.ContainsKey(connectionId))
        {
            return playerObjects[connectionId];
        }
        return null;
    }

    //for Change Host


}