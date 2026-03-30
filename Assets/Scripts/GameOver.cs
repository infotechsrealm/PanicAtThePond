using Mirror;
using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviourPunCallbacks
{
    public static GameOver Instance;

    public Button playAgainBtn, lobbyButton, CloseButton;
    public Text waitingForHostText;

    // CRITICAL: PhotonView reference for RPC calls
    new private PhotonView photonView;

    private void Awake()
    {
        Instance = this;

        // Initialize PhotonView with 3 fallback options
        // Option 1: Get PhotonView from this GameObject
        photonView = GetComponent<PhotonView>();

        // Option 2: If not on this GameObject, find it in the scene
        if (photonView == null)
        {
            photonView = UnityEngine.Object.FindFirstObjectByType<PhotonView>();
        }

        // Option 3: If still null, create a new PhotonView
        if (photonView == null)
        {
            Debug.LogError("âŒ NO PhotonView FOUND! Adding PhotonView to GameOver GameObject");
            photonView = gameObject.AddComponent<PhotonView>();
        }

        // Check if this player is the original host
        bool isHost = false;

        if (GS.Instance.isLan)
        {
            isHost = GS.Instance.IsMirrorMasterClient;
        }
        else
        {
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
        // Remove all existing listeners first
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
        Debug.Log($"PhotonView: {photonView != null}");

        UpdateButtonVisibility();
    }

    public void UpdateButtonVisibility()
    {
        bool isHost = false;

        if (GS.Instance.isLan)
        {
            isHost = GS.Instance.IsMirrorMasterClient;
        }
        else
        {
            isHost = GS.Instance.isMasterClient;
        }

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

    /// <summary>
    /// PlayAgain - Called when host clicks "Play Again" button
    /// Resets all game state and reloads the Play scene
    /// </summary>
    public void PlayAgain()
    {
        Debug.Log("=== PLAY AGAIN BUTTON CLICKED ===");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"isMasterClient (original): {GS.Instance.isMasterClient}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"PhotonView is null: {photonView == null}");

        // Additional safeguard: Only original host should be able to click this button
        if (!GS.Instance.isLan && !GS.Instance.isMasterClient)
        {
            Debug.LogWarning("âš ï¸ Play Again button clicked but not original host - This should not happen!");
            return;
        }

        if (GS.Instance.isLan)
        {
            Debug.Log("LAN Mode - Resetting game state and loading Play scene");

            if (NetworkServer.active)
            {
                ResetGameState();
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
            Debug.Log("Photon Mode - Resetting game state and loading Play scene");

            // CRITICAL FIX: Check if GameManager's photonView exists before using it
            if (GameManager.Instance == null || GameManager.Instance.photonView == null)
            {
                Debug.LogError("❌ CRITICAL: GameManager or its PhotonView is NULL! Cannot send RPC!");
                return;
            }

            // Only original host can restart
            if (GS.Instance.isMasterClient)
            {
                if (PhotonNetwork.InRoom)
                {
                    // Check if we still have Master Client authority
                    if (PhotonNetwork.IsMasterClient)
                    {
                        // We have logic to restart directly
                        GameManager.Instance.ProcessRestart();
                    }
                    else
                    {
                        // We lost authority (likely due to role swap), so we must request it back first
                        Debug.LogWarning("⚠️ Original Host lost Master Client authority! Requesting it back...");
                        
                        GameManager.Instance.isRestoringHost = true;
                        GameManager.Instance.photonView.RPC(nameof(GameManager.RequestHostBack), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
                        
                        // Note: The actual restart will happen in OnMasterClientSwitched once we get authority back
                    }
                }
                else
                {
                    Debug.LogError("❌ Cannot play again - not in a room!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Play Again button clicked but not original host");
            }
        }
    }

    /// <summary>
    /// LoadPlayScene - Helper method for Invoke fallback
    /// </summary>
    private void LoadPlayScene()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsConnected)
        {
            Debug.Log("Loading Play scene...");
            PhotonNetwork.LoadLevel("Play");
        }
        else
        {
            Debug.LogError("❌ Cannot load Play scene - Not in room or disconnected!");
        }
    }

    /// <summary>
    /// ResetGameState - Local reset (mirrors GameManager logic for LAN mode)
    /// </summary>
    private void ResetGameState()
    {
        Debug.Log("=== RESETTING GAME STATE (GameOver) ===");

        // Hide game over panel
        if (GameManager.Instance != null && GameManager.Instance.gameOverPanel != null)
        {
            GameManager.Instance.gameOverPanel.SetActive(false);
            Debug.Log("âœ… Game over panel hidden");
        }

        // Reset GameManager state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.fishes.Clear();
            GameManager.Instance.allFishes.Clear();
            GameManager.Instance.fisherManIsSpawned = false;
            GameManager.Instance.isFisherMan = false;
            GameManager.Instance.goldWormEatByFish = false;
            GameManager.Instance.fishermanWorms = 0;
            Debug.Log("âœ… GameManager state reset");
        }

        // Reset FishController
        if (FishController.Instance != null)
        {
            FishController.Instance.isDead = false;
            FishController.Instance.canMove = true;
            FishController.Instance.catchadeFish = false;
            FishController.Instance.isFisherMan = false;
            FishController.Instance.hunger = 100;
            Debug.Log("âœ… FishController state reset");
        }

        // Reset FishermanController
        if (FishermanController.Instance != null)
        {
            FishermanController.Instance.catchadFish = 0;
            FishermanController.Instance.isCanCast = true;
            Debug.Log("âœ… FishermanController state reset");
        }

        // Reset HungerSystem
        if (HungerSystem.Instance != null)
        {
            HungerSystem.Instance.canDecrease = true;
            if (HungerSystem.Instance.hungerBar != null)
            {
                HungerSystem.Instance.hungerBar.value = 100f;
            }
            Debug.Log("âœ… HungerSystem reset");
        }

        // Reset MashPhaseManager
        if (MashPhaseManager.Instance != null)
        {
            MashPhaseManager.Instance.active = false;
            if (MashPhaseManager.Instance.mashPanel != null)
            {
                MashPhaseManager.Instance.mashPanel.SetActive(false);
            }
            Debug.Log("âœ… MashPhaseManager reset");
        }

        // Reset spawners
        if (WormSpawner.Instance != null)
        {
            WormSpawner.Instance.canSpawn = true;
            Debug.Log("âœ… WormSpawner reset");
        }

        if (JunkSpawner.Instance != null)
        {
            JunkSpawner.Instance.canSpawn = true;
            Debug.Log("âœ… JunkSpawner reset");
        }

        Debug.Log("=== GAME STATE RESET COMPLETE ===");
    }

    /// <summary>
    /// Lobby - Called when host clicks "Lobby" button
    /// Returns all players to the lobby/dash scene
    /// </summary>
    public void Lobby()
    {
        Debug.Log("=== LOBBY BUTTON CLICKED ===");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"isMasterClient (original): {GS.Instance.isMasterClient}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");

        if (!GS.Instance.isLan && !GS.Instance.isMasterClient)
        {
            Debug.LogWarning("âš ï¸ Lobby button clicked but not original host - This should not happen!");
            return;
        }

        if (GS.Instance.isLan)
        {
            Debug.Log("LAN Mode - Using ServerChangeScene");

            if (NetworkServer.active)
            {
                Debug.Log("Server is active - Loading Dash scene");
                NetworkManager.singleton.ServerChangeScene("Dash");
            }
            else
            {
                Debug.LogWarning("Server is not active - Cannot load Dash scene");
            }
        }
        else
        {
            Debug.Log("Photon Mode - Using LoadLevel");

            bool hasRoomInfo = PhotonNetwork.CurrentRoom != null;
            bool inRoom = PhotonNetwork.InRoom;
            bool isConnected = PhotonNetwork.IsConnected;

            Debug.Log($"Room state check - InRoom: {inRoom}, HasRoomInfo: {hasRoomInfo}, IsConnected: {isConnected}");

            if (GS.Instance.isMasterClient && PhotonNetwork.IsMasterClient)
            {
                if (hasRoomInfo && isConnected)
                {
                    Debug.Log("âœ… Loading Dash scene - returning to lobby with all players in room");
                    Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
                    PhotonNetwork.LoadLevel("Dash");
                }
                else if (inRoom)
                {
                    Debug.Log("âœ… Loading Dash scene - returning to lobby with all players in room");
                    Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
                    PhotonNetwork.LoadLevel("Dash");
                }
                else if (isConnected)
                {
                    Debug.LogWarning("âš ï¸ Connected but no room info - Attempting to load Dash anyway");
                    PhotonNetwork.LoadLevel("Dash");
                }
                else
                {
                    Debug.LogError("âŒ Cannot load lobby - not connected!");
                }
            }
            else
            {
                Debug.LogWarning("âš ï¸ Lobby button clicked but not master client or original host");
            }
        }
    }

    /// <summary>
    /// Close - Called when close button is clicked
    /// Leaves the room and returns to main menu
    /// </summary>
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