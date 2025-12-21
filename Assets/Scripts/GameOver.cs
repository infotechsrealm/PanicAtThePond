using Mirror;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class GameOver : MonoBehaviourPunCallbacks
{
    public static GameOver Instance;

    public Button playAgainBtn,lobbyButton,CloseButton;
    public Text waitingForHostText; // Text component for "Waiting for host action..."

    private void Awake()
    {
        Instance = this;
        
        // Check if this player is the original host (not current master client)
        // This ensures only the original host sees buttons, even if master client changed
        bool isHost = false;
        if (GS.Instance.isLan)
        {
            isHost = GS.Instance.IsMirrorMasterClient;
        }
        else
        {
            // Use GS.Instance.isMasterClient which tracks the original host
            // This prevents fisherman from seeing buttons when they win
            isHost = GS.Instance.isMasterClient;
        }
        
        // Show buttons only for original host, show waiting text for clients
        if (isHost)
        {
            playAgainBtn.gameObject.SetActive(true);
            lobbyButton.gameObject.SetActive(true);
            if (waitingForHostText != null)
            {
                waitingForHostText.gameObject.SetActive(false);
            }
        }
        else
        {
            playAgainBtn.gameObject.SetActive(false);
            lobbyButton.gameObject.SetActive(false);
            if (waitingForHostText != null)
            {
                waitingForHostText.gameObject.SetActive(true);
            }
        }
        
        PhotonNetwork.AutomaticallySyncScene = true;
    }


    private void Start()
    {
        // Remove all existing listeners first to prevent conflicts with Unity scene setup
        playAgainBtn.onClick.RemoveAllListeners();
        lobbyButton.onClick.RemoveAllListeners();
        CloseButton.onClick.RemoveAllListeners();
        
        // Add our listeners
        playAgainBtn.onClick.AddListener(PlayAgain);
        lobbyButton.onClick.AddListener(Lobby);
        CloseButton.onClick.AddListener(Close);
        
        Debug.Log("GameOver: Button listeners set up");
        Debug.Log($"PlayAgain button: {playAgainBtn != null}");
        Debug.Log($"Lobby button: {lobbyButton != null}");
        Debug.Log($"Close button: {CloseButton != null}");
        
        // Update button visibility on Start as well (in case Awake was called before network state was ready)
        UpdateButtonVisibility();
    }
    
    // Method to update button visibility based on host status
    public void UpdateButtonVisibility()
    {
        // Check if this player is the original host (not current master client)
        // This ensures only the original host sees buttons, even if master client changed
        bool isHost = false;
        if (GS.Instance.isLan)
        {
            isHost = GS.Instance.IsMirrorMasterClient;
        }
        else
        {
            // Use GS.Instance.isMasterClient which tracks the original host
            // This prevents fisherman from seeing buttons when they win
            isHost = GS.Instance.isMasterClient;
        }
        
        // Show buttons only for original host, show waiting text for clients
        if (isHost)
        {
            playAgainBtn.gameObject.SetActive(true);
            lobbyButton.gameObject.SetActive(true);
            if (waitingForHostText != null)
            {
                waitingForHostText.gameObject.SetActive(false);
            }
        }
        else
        {
            playAgainBtn.gameObject.SetActive(false);
            lobbyButton.gameObject.SetActive(false);
            if (waitingForHostText != null)
            {
                waitingForHostText.gameObject.SetActive(true);
            }
        }
    }
    public void PlayAgain()
    {
        Debug.Log("=== PLAY AGAIN BUTTON CLICKED ===");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"isMasterClient (original): {GS.Instance.isMasterClient}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        
        // Additional safeguard: Only original host should be able to click this button
        if (!GS.Instance.isLan && !GS.Instance.isMasterClient)
        {
            Debug.LogWarning("⚠️ Play Again button clicked but not original host - This should not happen!");
            return;
        }
        
        // Load Play scene for all players
        if (GS.Instance.isLan)
        {
            Debug.Log("LAN Mode - Using ServerChangeScene to load Play");
            // For Mirror/LAN: Use ServerChangeScene to load for all players
            if (NetworkServer.active)
            {
                Debug.Log("Server is active - Loading Play scene");
                NetworkManager.singleton.ServerChangeScene("Play");
            }
            else
            {
                Debug.LogWarning("Server is not active - Cannot load Play scene");
            }
        }
        else
        {
            Debug.Log("Photon Mode - Using LoadLevel to load Play");
            // For Photon: Use LoadLevel to sync scene for all players
            // Check both original host and current master client (master client may have been restored)
            if (GS.Instance.isMasterClient && PhotonNetwork.IsMasterClient)
            {
                if (PhotonNetwork.InRoom)
                {
                    Debug.Log("✅ Loading Play scene with all players in room");
                    PhotonNetwork.LoadLevel("Play");
                }
                else
                {
                    Debug.LogError("❌ Cannot play again - not in a room!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Play Again button clicked but not master client or original host");
            }
        }
    }

    public void Lobby()
    {
        Debug.Log("=== LOBBY BUTTON CLICKED ===");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"isMasterClient (original): {GS.Instance.isMasterClient}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"CurrentRoom: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "null")}");
        Debug.Log($"PlayerCount: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount.ToString() : "N/A")}");
        
        // Additional safeguard: Only original host should be able to click this button
        if (!GS.Instance.isLan && !GS.Instance.isMasterClient)
        {
            Debug.LogWarning("⚠️ Lobby button clicked but not original host - This should not happen!");
            return;
        }
        
        // CRITICAL: Set flag to prevent RestartGame() from disconnecting
        // The Unity scene's persistent listener may trigger RestartGame() which starts disconnecting
        // We set this flag so RestartGame() knows to skip the disconnect
        GameManager.SetLobbyButtonPressed(true);
        Debug.Log("✅ Set isLobbyButtonPressed flag to prevent RestartGame() from disconnecting");
        
        // Also stop any pending coroutines
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StopAllCoroutines();
            Debug.Log("✅ Stopped all coroutines (including any pending disconnect from RestartGame)");
        }
        
        // Load Dash scene (which contains Client Lobby) for all players
        // This will return all players to the lobby while keeping them in the same room
        // DO NOT disconnect - we want to keep everyone in the same room
        bool sceneLoadInitiated = false;
        
        if (GS.Instance.isLan)
        {
            Debug.Log("LAN Mode - Using ServerChangeScene");
            // For Mirror/LAN: Use ServerChangeScene to load for all players
            // This keeps the network connection active and all players in the same session
            if (NetworkServer.active)
            {
                Debug.Log("Server is active - Loading Dash scene");
                NetworkManager.singleton.ServerChangeScene("Dash");
                sceneLoadInitiated = true;
            }
            else
            {
                Debug.LogWarning("Server is not active - Cannot load Dash scene");
            }
        }
        else
        {
            Debug.Log("Photon Mode - Using LoadLevel");
            // For Photon: Use LoadLevel to sync scene for all players
            // This keeps all players in the same room and loads Dash scene
            // PhotonNetwork.LoadLevel automatically syncs the scene for all players in the room
            
            // Check if we have room info - even if InRoom is false (might be in transition state)
            // CurrentRoom can still be valid during disconnection process
            bool hasRoomInfo = PhotonNetwork.CurrentRoom != null;
            bool inRoom = PhotonNetwork.InRoom;
            bool isConnected = PhotonNetwork.IsConnected;
            
            Debug.Log($"Room state check - InRoom: {inRoom}, HasRoomInfo: {hasRoomInfo}, IsConnected: {isConnected}");
            
            // Check both original host and current master client (master client may have been restored)
            if (GS.Instance.isMasterClient && PhotonNetwork.IsMasterClient)
            {
                // If we have room info and are connected, we can still load the scene and sync to other players
                // Even if InRoom is false due to disconnection in progress, CurrentRoom might still be valid
                if (hasRoomInfo && isConnected)
                {
                    // We have room info and are connected - load Dash scene
                    // This will sync to all players in the room
                    Debug.Log("✅ Loading Dash scene - returning to lobby with all players in room");
                    Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
                    PhotonNetwork.LoadLevel("Dash");
                    sceneLoadInitiated = true;
                }
                else if (inRoom)
                {
                    // Standard case - we're in a room
                    Debug.Log("✅ Loading Dash scene - returning to lobby with all players in room");
                    Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
                    PhotonNetwork.LoadLevel("Dash");
                    sceneLoadInitiated = true;
                }
                else if (isConnected)
                {
                    Debug.LogWarning("⚠️ Connected but no room info - Attempting to load Dash anyway");
                    // Try to load Dash - if we're connected, we might still be able to sync
                    PhotonNetwork.LoadLevel("Dash");
                    sceneLoadInitiated = true;
                }
                else
                {
                    Debug.LogError("❌ Cannot load lobby - not connected!");
                    Debug.Log("Player was disconnected. Cannot return to lobby with players.");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Lobby button clicked but not master client or original host - Only master can load scene");
            }
        }
        
        // CRITICAL: Reset the flag after scene loading is initiated
        // This prevents the flag from persisting indefinitely if RestartGame() is never called
        // We use a coroutine to reset it after a short delay to ensure RestartGame() has had time to check it
        if (sceneLoadInitiated)
        {
            StartCoroutine(ResetLobbyFlagAfterDelay());
        }
        else
        {
            // If scene loading failed, reset immediately
            GameManager.SetLobbyButtonPressed(false);
            Debug.Log("⚠️ Scene loading failed - Reset isLobbyButtonPressed flag immediately");
        }
    }
    
    private System.Collections.IEnumerator ResetLobbyFlagAfterDelay()
    {
        // Wait a short time to ensure RestartGame() has had time to check the flag
        // This prevents the persistent listener's RestartGame() call from disconnecting
        yield return new WaitForSeconds(0.1f);
        
        // Reset the flag so future RestartGame() calls work normally
        GameManager.SetLobbyButtonPressed(false);
        Debug.Log("✅ Reset isLobbyButtonPressed flag after scene load initiated");
    }

    public void Close()
    {
        if (GS.Instance.isLan)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
    }

}
