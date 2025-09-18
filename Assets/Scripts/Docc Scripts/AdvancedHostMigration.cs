using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using System.Collections;

public class AdvancedHostMigration : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private bool _enableAutomaticHostMigration = true;

    private void Start()
    {
        _networkManager = NetworkUI.instence.networkManager;
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        }
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped && _enableAutomaticHostMigration)
        {
            Debug.Log("Server disconnected. Checking for host migration...");
            // Yahan aap logic add kar sakte hain ki kaun sa client naya host banega
        }
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped && _enableAutomaticHostMigration)
        {
            Debug.Log("Disconnected from server. Considering host migration...");
            // Automatic host migration ke liye logic yahan add karein
        }
    }

    // Host migration start karne ke liye main function
    public void BecomeNewHost()
    {
        if (CanBecomeHost())
        {
            StartCoroutine(HostMigrationProcess());
        }
        else
        {
            Debug.LogWarning("This client cannot become the host at this time");
        }
    }

    // Check karein ki kya ye client host ban sakta hai
    private bool CanBecomeHost()
    {
        // Yahan aap apni game-specific conditions check kar sakte hain
        // Jaise: player score, connection stability, etc.

        return _networkManager.ClientManager.Started &&
               !_networkManager.ServerManager.Started &&
               IsConnectionStable();
    }

    private bool IsConnectionStable()
    {
        // Simple check - aap isme advanced logic add kar sakte hain
        return true;
    }

    // Host migration process
    private IEnumerator HostMigrationProcess()
    {
        Debug.Log("Starting host migration process...");

        // 1. Pehle client connection stop karein
        _networkManager.ClientManager.StopConnection();
        yield return new WaitForSeconds(0.1f);

        // 2. Server start karein
        _networkManager.ServerManager.StartConnection();
        yield return new WaitForSeconds(0.5f); // Server start hone ka wait karein

        // 3. Local client ko naye server se connect karein
        _networkManager.ClientManager.StartConnection();

        Debug.Log("Host migration completed successfully");

        // 4. Game state restore karein (agar needed ho)
        RestoreGameState();
    }

    private void RestoreGameState()
    {
        // Yahan aap game state restore kar sakte hain
        // Jaise: player positions, scores, game time, etc.
        Debug.Log("Game state restored after host migration");
    }

    // UI se call karne ke liye
    public void OnBecomeHostButtonPressed()
    {
        BecomeNewHost();
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
        }
    }
}