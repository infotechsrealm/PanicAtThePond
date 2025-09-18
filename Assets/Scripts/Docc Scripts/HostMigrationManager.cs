using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using System.Collections;

public class HostMigrationManager : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;

    private void Start()
    {
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        }
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Server disconnected, try to migrate host
            TryHostMigration();
        }
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Lost connection to server
            Debug.Log("Disconnected from server. Attempting reconnection...");
            StartCoroutine(AttemptReconnect());
        }
    }

    private void TryHostMigration()
    {
        // Check if we can become the new host
        Debug.Log("Attempting host migration...");

        // For now, just stop the network completely
        _networkManager.ServerManager.StopConnection(true);
        _networkManager.ClientManager.StopConnection();

        // Here you would typically:
        // 1. Check if this client should become the new host
        // 2. Start a new server on this client
        // 3. Have other clients reconnect to this new server
    }

    private IEnumerator AttemptReconnect()
    {
        yield return new WaitForSeconds(2f);

        // Try to reconnect to the server
        if (!_networkManager.ClientManager.Started)
        {
            _networkManager.ClientManager.StartConnection();
        }
    }

    // Host migration ke liye main function
    public void MigrateToNewHost()
    {
        // Pehle check karein ki kya ye client hai aur server nahi
        if (_networkManager.ClientManager.Started && !_networkManager.ServerManager.Started)
        {
            Debug.Log("This client will become the new host...");
            StartCoroutine(HostMigrationProcess());
        }
    }

    // Host migration process
    private IEnumerator HostMigrationProcess()
    {
        // 1. Pehle client connection stop karein
        _networkManager.ClientManager.StopConnection();
        yield return new WaitForSeconds(0.1f);

        // 2. Server start karein
        _networkManager.ServerManager.StartConnection();
        yield return new WaitForSeconds(0.5f); // Server start hone ka wait karein

        // 3. Local client ko naye server se connect karein
        _networkManager.ClientManager.StartConnection();

        Debug.Log("Host migration completed successfully");
    }

    // UI Button ke liye public method
    public void OnBecomeHostButtonClicked()
    {
        MigrateToNewHost();
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