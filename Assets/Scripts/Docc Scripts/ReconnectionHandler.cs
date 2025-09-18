using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Managing.Client;
using FishNet.Transporting;

public class ReconnectionHandler : MonoBehaviour
{
    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private GameObject _reconnectPanel;
    [SerializeField] private Button _reconnectButton;
    [SerializeField] private Text _statusText;

    private int _reconnectAttempts = 0;
    private const int MAX_RECONNECT_ATTEMPTS = 5;

    private void Start()
    {
        if (_networkManager != null)
        {
            _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
        }

        if (_reconnectButton != null)
        {
            _reconnectButton.onClick.AddListener(AttemptReconnect);
        }

        if (_reconnectPanel != null)
        {
            _reconnectPanel.SetActive(false);
        }
    }

    private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            // Show reconnect UI
            ShowReconnectUI();
        }
        else if (args.ConnectionState == LocalConnectionState.Started)
        {
            // Hide reconnect UI on successful connection
            HideReconnectUI();
            _reconnectAttempts = 0;
        }
    }

    private void ShowReconnectUI()
    {
        if (_reconnectPanel != null)
        {
            _reconnectPanel.SetActive(true);
        }

        if (_statusText != null)
        {
            _statusText.text = "Disconnected from server. Attempting to reconnect...";
        }

        // Start automatic reconnection attempts
        StartCoroutine(AutoReconnect());
    }

    private void HideReconnectUI()
    {
        if (_reconnectPanel != null)
        {
            _reconnectPanel.SetActive(false);
        }
    }

    private IEnumerator AutoReconnect()
    {
        // FishNet v4 compatible connection state check
        bool isConnected = _networkManager.ClientManager.Started;

        while (_reconnectAttempts < MAX_RECONNECT_ATTEMPTS && !isConnected)
        {
            yield return new WaitForSeconds(Mathf.Pow(2, _reconnectAttempts)); // Exponential backoff

            AttemptReconnect();
            _reconnectAttempts++;

            if (_statusText != null)
            {
                _statusText.text = $"Attempting to reconnect ({_reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS})...";
            }

            // Update connection status
            isConnected = _networkManager.ClientManager.Started;
        }

        if (_reconnectAttempts >= MAX_RECONNECT_ATTEMPTS && !isConnected)
        {
            if (_statusText != null)
            {
                _statusText.text = "Failed to reconnect. Please check your connection and try again.";
            }
        }
    }

    public void AttemptReconnect()
    {
        // FishNet v4 compatible connection state check
        if (_networkManager != null && !_networkManager.ClientManager.Started)
        {
            _networkManager.ClientManager.StartConnection();
        }
    }

    private void OnDestroy()
    {
        if (_networkManager != null)
        {
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
        }

        if (_reconnectButton != null)
        {
            _reconnectButton.onClick.RemoveListener(AttemptReconnect);
        }
    }
}