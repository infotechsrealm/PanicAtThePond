using Mirror;
using Mirror.Discovery;
using Photon.Pun;
using Photon.Realtime;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Player Setup")]
    public int totalPlayers;
    public GameObject fishermanPrefab;
    public GameObject FisherMan_Hungerbar;
    public GameObject fishPrefab;

    [Header("Worm Settings")]
    public int baseWormMultiplier = 3;

    [Header("Fish Spawn Bounds")]
    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    [Header("Runtime Info")]
    internal int fishermanWorms;
    public int maxWorms;
    public List<GameObject> fishes = new List<GameObject>();

    [Header("UI")]
    public Slider castingMeter;
    public static GameManager Instance;
    public GameObject gameOverPanel, WinScreen, WinnerScreen, ScoreScreen;
    public Text gameOverText;

    [Header("Bucket Sprites")]
    public Sprite fullBucket;
    public Sprite halfBucket;
    public Sprite emptyBucket;

    [Header("UI References")]
    public Image bucketImage;
    public Text wormCountText;
    public GameObject hungerBar;
    public GameObject fisherManObjects;
    public GameObject preloderUI;
    public GameObject coverBG;

    public FishController myFish;
    public List<FishController> allFishes = new List<FishController>();
    public Text messageText;

    internal bool fisherManIsSpawned = false;
    internal bool isFisherMan = false;
    internal bool goldWormEatByFish = false;
    internal bool isRoundEnding = false;
    public GameObject sky, water;

    internal bool isRestoringHost = false; // Flag to track if we are waiting for host authority back

    private void Awake()
    {
        Instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            GS.Instance.isMasterClient = true;
        }
        else
        {
            GS.Instance.isMasterClient = false;
        }
    }

    void Start()
    {
        if (GS.Instance.isLan)
        {
            // LAN mode: spawn immediately
            SpawnPlayer();
            Invoke(nameof(setFishermanWormCounts), 2f);
        }
        else
        {
            // Photon mode: wait for network to be ready before spawning
            // Also check if we need to reload scene (Photon doesn't support reloading same scene on clients)
            if (PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient)
            {
                // Client: Check if scene is actually loaded correctly
                // If we're in Play scene but don't have a fish, we might need to spawn
                StartCoroutine(SpawnPlayerWhenReady());
            }
            else if (PhotonNetwork.IsMasterClient)
            {
                // Host: Always spawn
                StartCoroutine(SpawnPlayerWhenReady());
            }
            else
            {
                // Not in room yet, wait a bit
                StartCoroutine(SpawnPlayerWhenReady());
            }
        }
    }

    /// <summary>
    /// Coroutine to wait for Photon to be ready before spawning players
    /// This ensures proper synchronization when scene loads via PhotonNetwork.LoadLevel
    /// </summary>
    private IEnumerator SpawnPlayerWhenReady()
    {
        Debug.Log($"[SpawnPlayerWhenReady] Starting coroutine - IsMasterClient: {PhotonNetwork.IsMasterClient}, InRoom: {PhotonNetwork.InRoom}, Scene: {SceneManager.GetActiveScene().name}");
        
        // Wait for Photon to be in a room and message queue to be running
        // This is critical when loading scenes via PhotonNetwork.LoadLevel
        int maxWaitTime = 10; // Increased wait time for scene reload
        float elapsed = 0f;
        
        // First, wait for scene to be fully loaded
        while (SceneManager.GetActiveScene().name != "Play" && elapsed < maxWaitTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (SceneManager.GetActiveScene().name != "Play")
        {
            Debug.LogError($"❌ Scene not loaded after {maxWaitTime}s. Current scene: {SceneManager.GetActiveScene().name}");
            yield break;
        }
        
        Debug.Log($"[SpawnPlayerWhenReady] Scene loaded. Waiting for Photon network...");
        elapsed = 0f;
        
        // Now wait for Photon network to be ready
        while (elapsed < maxWaitTime)
        {
            bool inRoom = PhotonNetwork.InRoom;
            bool messageQueueRunning = PhotonNetwork.IsMessageQueueRunning;
            bool isConnected = PhotonNetwork.IsConnected;
            int playerCount = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
            
            // Check all conditions
            if (inRoom && messageQueueRunning && isConnected && playerCount > 0)
            {
                // Additional delay to ensure everything is synchronized
                yield return new WaitForSeconds(0.3f);
                
                // Double-check conditions after delay
                if (PhotonNetwork.InRoom && PhotonNetwork.IsMessageQueueRunning && PhotonNetwork.CurrentRoom.PlayerCount > 0)
                {
                    Debug.Log($"✅ Photon is ready - Spawning player. PlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}, IsMasterClient: {PhotonNetwork.IsMasterClient}");
                    SpawnPlayer();
                    Invoke(nameof(setFishermanWormCounts), 2f);
                    yield break;
                }
            }
            
            // Log status every second
            if (Mathf.FloorToInt(elapsed) != Mathf.FloorToInt(elapsed - Time.deltaTime))
            {
                Debug.Log($"[SpawnPlayerWhenReady] Waiting... ({elapsed:F1}s) InRoom: {inRoom}, MessageQueue: {messageQueueRunning}, Connected: {isConnected}, Players: {playerCount}");
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Fallback: spawn anyway if we've waited too long
        Debug.LogWarning($"⚠️ Photon ready timeout after {maxWaitTime}s - Attempting spawn anyway");
        Debug.Log($"Final state - InRoom: {PhotonNetwork.InRoom}, MessageQueue: {PhotonNetwork.IsMessageQueueRunning}, Connected: {PhotonNetwork.IsConnected}, Players: {(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0)}");
        
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.PlayerCount > 0)
        {
            Debug.Log("⚠️ Spawning player despite timeout");
            SpawnPlayer();
            Invoke(nameof(setFishermanWormCounts), 2f);
        }
        else
        {
            Debug.LogError("❌ Cannot spawn player - Not in a room or no players!");
        }
    }

    public void UpdateUI(int currunt_Warms)
    {
        bucketImage.gameObject.SetActive(true);

        // Text
        wormCountText.text = currunt_Warms.ToString();

        // Percentage
        float percentage = (float)currunt_Warms / maxWorms;
        if (percentage >= 0.5f)
        {
            bucketImage.sprite = fullBucket;
        }
        else if (percentage > 0.25f)
        {
            bucketImage.sprite = halfBucket;
        }
        else
        {
            bucketImage.sprite = emptyBucket;
        }
    }

    void SpawnPlayer()
    {
        Debug.Log("GS.Instance.isLan = > " + GS.Instance.isLan);
        GameObject selectedFishPrefab = ResolveSelectedFishPrefab();

        if (GS.Instance.isLan)
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                {
                    float x = Random.Range(minBounds.x, maxBounds.x);
                    float y = Random.Range(minBounds.y, maxBounds.y);
                    Vector3 spawnPos = new Vector3(x, y, 0);
                    GameObject fish = Instantiate(selectedFishPrefab, spawnPos, Quaternion.identity);
                    fishes.Add(fish);
                    NetworkServer.AddPlayerForConnection(conn, fish);
                }
            }
        }
        else
        {
            // Spawn Fish
            float x = Random.Range(minBounds.x, maxBounds.x);
            float y = Random.Range(minBounds.y, maxBounds.y);
            Vector3 spawnPos = new Vector3(x, y, 0);
            GameObject fish = PhotonNetwork.Instantiate(selectedFishPrefab.name, spawnPos, Quaternion.identity);
            fishes.Add(fish);
            Debug.Log("Fish Spawned: " + fishes.Count);
        }
    }

    private GameObject ResolveSelectedFishPrefab()
    {
        string selectedFishPrefabName = LocalPlayManager.GetSelectedFishPrefabName();
        GameObject selectedFishPrefab = Resources.Load<GameObject>(selectedFishPrefabName);
        if (selectedFishPrefab != null)
        {
            return selectedFishPrefab;
        }

        Debug.LogWarning($"Selected fish prefab '{selectedFishPrefabName}' was not found in Resources. Falling back to default fish prefab.");
        return fishPrefab;
    }

    public void setFishermanWormCounts()
    {
        if (GS.Instance.isLan)
        {
            if (GS.Instance.IsMirrorMasterClient)
            {
                GS.Instance.totlePlayers = totalPlayers = NetworkServer.connections.Count;
                Debug.Log("totlePlayer = " + GS.Instance.totlePlayers);
            }
            else
            {
                GS.Instance.totlePlayers = totalPlayers = allFishes.Count;
                Debug.Log("totlePlayer = " + GS.Instance.totlePlayers);
            }

            int fishCount = totalPlayers - 1;
            fishermanWorms = fishCount * baseWormMultiplier;
            maxWorms = fishermanWorms;
            Debug.Log("Fisherman Worms: " + fishermanWorms);
        }
        else
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int fishCount = totalPlayers - 1;
            fishermanWorms = fishCount * baseWormMultiplier;
            maxWorms = fishermanWorms;
            Debug.Log("Fisherman Worms: " + fishermanWorms);
        }
    }

    public void LoadSpawnFisherman()
    {
        ResetFishHungerForRoleTransition();
        LoadPreloderOnOff(true);
        Invoke(nameof(SpawnFisherman), 0f);
    }

    public void ResetFishHungerForRoleTransition()
    {
        if (HungerSystem.Instance != null)
        {
            HungerSystem.Instance.ResetHungerToFull(true);
        }
    }

    public void SpawnFisherman()
    {
        if (GS.Instance.isLan)
        {
            myFish.GetComponent<FishController_Mirror>().RequestSpawnFisherman();
        }
        else
        {
            photonView.RPC(nameof(FisherManSpawned), RpcTarget.All, true);
            PhotonNetwork.Instantiate(fishermanPrefab.name, new Vector3(0f, 1.95f, 0f), Quaternion.identity);
            FisherMan_Hungerbar.SetActive(false);
        }
    }

    public void ShowGameOver(string message)
    {
        Debug.Log("Game Over: " + message);

        bool isQuickSurvivalist = GS.Instance != null && GS.Instance.currentGameMode == 0;
        bool isQuickQast = GS.Instance != null && GS.Instance.currentGameMode == 1;
        bool isDeepSeaFishing = GS.Instance != null && GS.Instance.currentGameMode == 2;
        bool isStarvationTie = (isQuickQast || isDeepSeaFishing)
            && !goldWormEatByFish
            && !string.IsNullOrEmpty(message)
            && message.Contains("Starve");

        if (isStarvationTie && GS.Instance != null)
        {
            Debug.Log("Starvation tie detected. Score screen will complete before returning to Dash with tie preloader.");
            GS.Instance.MarkTiePreloderForDash();

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (WinnerScreen != null) WinnerScreen.SetActive(false);
        }


        if (isQuickSurvivalist)
        {
                gameOverPanel.SetActive(true);
                Debug.Log("isQuicSurvivalList ShowGameOver Runingggggg");

                if (WormSpawner.Instance != null && WormSpawner.Instance.canSpawn)
                {
                    WormSpawner.Instance.canSpawn = false;
                }
            
            else
            {
                Debug.Log("Gameover Panel is null");
            }

            if (gameOverText != null)
            {
                gameOverText.text = message;
            }

            // Update button visibility when game over panel is shown
            if (GameOver.Instance != null)
            {
                GameOver.Instance.UpdateButtonVisibility();
            }
        }
        if (isQuickQast)
        {
            Debug.Log("isQuickQast SurvivalList ShowGameOver Runingggggg");

            if (WormSpawner.Instance != null && WormSpawner.Instance.canSpawn)
            {
                WormSpawner.Instance.canSpawn = false;
            }
            WinScreen.SetActive(true);
            
           // ScoreScreen.SetActive(true);
            
            CalculateEndOfRoundBonuses(message);
            ShowScoreScreenNow();
        }
        if(isDeepSeaFishing){
            Debug.Log("isQuickQast SurvivalList ShowGameOver Runingggggg");

            if (WormSpawner.Instance != null && WormSpawner.Instance.canSpawn)
            {
                WormSpawner.Instance.canSpawn = false;
            }
            WinScreen.SetActive(true);
            
            if (ScoreScreen != null) ScoreScreen.SetActive(false);
            
            CalculateEndOfRoundBonuses(message);
            ShowScoreScreenNow();
        }
        else
        {
             // For Quick Cast and Deep Sea Fishing, we don't show the Game Over screen.
             // But we should stop the worm spawner to finalize the round state.
             if (WormSpawner.Instance != null && WormSpawner.Instance.canSpawn)
             {
                 WormSpawner.Instance.canSpawn = false;
             }
        }
    }

    public void CallShowGameOverRPC(string message)
    {
        if (GS.Instance.isLan)
        {
            if (myFish != null && myFish.fishController_Mirror != null)
            {
                myFish.fishController_Mirror.CallShowGameOver_Mirror(message);
            }
            else if (FishermanController.Instance != null && FishermanController.Instance.fishermanController_Mirror != null)
            {
                FishermanController.Instance.fishermanController_Mirror.CallShowGameOver_Mirror(message);
            }
        }
        else
        {
            photonView.RPC(nameof(ShowGameOverRPC), RpcTarget.All, message);
        }
    }

    public void CallFishermanWinAnimationRPC()
    {
        if (GS.Instance != null && GS.Instance.isLan)
        {
            PlayFishermanWinAnimationRPC();
            return;
        }

        photonView.RPC(nameof(PlayFishermanWinAnimationRPC), RpcTarget.All);
    }

    [PunRPC]
    public void PlayFishermanWinAnimationRPC()
    {
        if (FishermanController.Instance != null)
        {
            FishermanController.Instance.PlayWinAnimation();
        }
    }

    [PunRPC]
    public void ShowGameOverRPC(string message)
    {
        ShowGameOver(message);
    }

    public void TriggerRoundEnd(string message)
    {
         if (GS.Instance == null) return;
         if (GS.Instance.currentGameMode == 0) return; // Quick survivalist uses normal game over

         if (GS.Instance.isLan)
         {
             if (myFish != null && myFish.fishController_Mirror != null)
             {
                 myFish.fishController_Mirror.CallTriggerRoundEnd_Mirror(message);
             }
             else if (FishermanController.Instance != null && FishermanController.Instance.fishermanController_Mirror != null)
             {
                 FishermanController.Instance.fishermanController_Mirror.CallTriggerRoundEnd_Mirror(message);
             }
         }
         else
         {
             photonView.RPC(nameof(EndRoundRPC), RpcTarget.All, message);
         }
    }

    [PunRPC]
    public void EndRoundRPC(string message)
    {
         if (GS.Instance == null || GS.Instance.currentGameMode == 0) return;
         if (isRoundEnding) return;
         isRoundEnding = true;
         
         // Shut down regular Game Over panel if it's active so it doesn't overlap
         if (gameOverPanel != null) gameOverPanel.SetActive(false);
         
         CalculateEndOfRoundBonuses(message);
         ShowScoreScreenNow();
    }

    private IEnumerator ShowScoreScreenDelayed()
    {
        yield return null;
        ShowScoreScreenNow();
    }

    private void ShowScoreScreenNow()
    {
        if (ScoreManager.Instance != null && GS.Instance != null)
        {
            ScoreManager.Instance.ShowScoreScreen(GS.Instance.playerScores);
        }
    }

    public void HandleEndOfRoundTransition()
    {
        if (GS.Instance == null) return;

        bool isTieReturn = (GS.Instance.currentGameMode == 1 || GS.Instance.currentGameMode == 2)
            && IsStarvationTieReturnPending();

        if (isTieReturn)
        {
            GS.Instance.ReturnToLobbyWithPendingTiePreloder();
            return;
        }
        
        if (GS.Instance.currentGameMode == 1) // Quick Cast (1 round)
        {
            DetermineWinnerAndShowWinnerScreen();
        }
        else if (GS.Instance.currentGameMode == 2) // Deep Sea Fishing (5 rounds)
        {
            if (GS.Instance.currentRound < 5)
            {
                GS.Instance.currentRound++;
                // Resets round and reloads the Play scene
                ProcessRestart(); 
            }
            else
            {
                DetermineWinnerAndShowWinnerScreen();
            }
        }
    }

    public void DetermineWinnerAndShowWinnerScreen() 
    {
        string winnerName = "No One";
        int highestScore = -1;
        
        foreach(var kvp in GS.Instance.playerScores)
        {
            if(kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
                winnerName = kvp.Key;
            }
        }
        
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ShowWinnerScreen(winnerName, highestScore);
        }
        
        // Wait then go to Lobby (Removed automatic transition so Host can choose via WinnerScreen buttons)
        // StartCoroutine(ReturnToLobbyDelayed());
    }

    private bool IsStarvationTieReturnPending()
    {
        return GS.Instance != null && GS.Instance.ShouldShowTiePreloderOnDash;
    }
    
    private IEnumerator ReturnToLobbyDelayed()
    {
         yield return new WaitForSeconds(5f);
         if (GameOver.Instance != null)
         {
             GameOver.Instance.Lobby();
         }
    }

    public void CalculateEndOfRoundBonuses(string message)
    {
        if (GS.Instance == null) return;

        ScoreSystemSettings settings = GS.Instance.scoreSystemSettings;
        int fishermanWinPoints = settings != null ? settings.GetFishermanWinPoints() : 15;
        int fishermanBucketWormPoints = settings != null ? settings.GetFishermanBucketWormPoints() : 1;
        int fishWinPoints = settings != null ? settings.GetFishWinPoints() : 10;
        int fishSurvivePoints = settings != null ? settings.GetFishSurvivePoints() : 5;

        bool fishermanWon = message.Contains("Fisherman Win");
        bool fishesWon = message.Contains("Fishes Win") || message.Contains("You win");

        string myName = "Player";
        if (GS.Instance.isLan) {
            myName = GS.Instance.nickName;
        }
        else if (PhotonNetwork.InRoom) {
            myName = PhotonNetwork.LocalPlayer.NickName;
        }

        bool isFullLobby = totalPlayers >= 6; 
        int totalOriginalFish = totalPlayers - 1;
        int aliveFishCount = 0;

        foreach (var fish in allFishes)
        {
             if (fish != null && !fish.isDead) aliveFishCount++;
        }

        if (fishermanWon)
        {
            if (isFisherMan) 
            {
                if (GS.Instance.currentGameMode == 0)
                {
                    gameOverPanel.SetActive(true);
                }
                AddPlayerScore(myName, fishermanWinPoints);
                if (fishermanWorms > 0)
                {
                    AddPlayerScore(myName, fishermanWorms * fishermanBucketWormPoints);
                }
                
                
                if (isFullLobby && FishermanController.Instance != null && FishermanController.Instance.catchadFish >= 6) UnlockAchievement("FISH_SLAYER");
                if (isFullLobby && GS.Instance.currentRoundWormsUsed <= 6) UnlockAchievement("EARTH_PRAISER");
            }
        }
        else if (fishesWon)
        {
            if (!isFisherMan && myFish != null) 
            {
                AddPlayerScore(myName, fishWinPoints);

                if (!myFish.isDead) 
                {
                    AddPlayerScore(myName, fishSurvivePoints);
                    
                    if (isFullLobby && aliveFishCount == totalOriginalFish) UnlockAchievement("WE_COME_IN_SWARMS");
                    if (GS.Instance.currentGameMode == 2 && GS.Instance.hooksEscaped.ContainsKey(myName) && GS.Instance.hooksEscaped[myName] >= 15) UnlockAchievement("SURVIVOR");
                    if (GS.Instance.currentGameMode == 1 && aliveFishCount == 1) UnlockAchievement("SOLO_ARTIST");
                }
            }
        }
    }

    [PunRPC]
    public void AddPlayerScoreRPC(string playerName, int amount)
    {
        if (GS.Instance == null) return;
        if (!GS.Instance.playerScores.ContainsKey(playerName))
            GS.Instance.playerScores[playerName] = 0;
            
        GS.Instance.playerScores[playerName] += amount;
    }

    public void AddPlayerScore(string playerName, int amount)
    {
        if (GS.Instance == null) return;

        if (GS.Instance.isLan)
        {
             if (!isFisherMan && myFish != null && myFish.fishController_Mirror != null)
             {
                  myFish.fishController_Mirror.CallAddScore_Mirror(playerName, amount);
             }
             else if (isFisherMan && FishermanController.Instance != null && FishermanController.Instance.fishermanController_Mirror != null)
             {
                  FishermanController.Instance.fishermanController_Mirror.CallAddScore_Mirror(playerName, amount);
             }
        }
        else
        {
             photonView.RPC(nameof(AddPlayerScoreRPC), RpcTarget.All, playerName, amount);
        }
    }

    public void UnlockAchievement(string achievementId)
    {
         PlayerPrefs.SetInt("Achievement_" + achievementId, 1);
         PlayerPrefs.Save();

         if (SteamManager.Initialized)
         {
             SteamUserStats.SetAchievement(achievementId);
             SteamUserStats.StoreStats();
             Debug.Log("Unlocked Steam Achievement: " + achievementId);
         }
    }

    /// <summary>
    /// ResetGameState_RPC - Called via RPC on all clients to reset game state
    /// IMPORTANT: This method is called on GameManager, not GameOver!
    /// </summary>
    [PunRPC]
    public void ResetGameState_RPC()
    {
        Debug.Log("=== RESETTING GAME STATE (RPC on GameManager) ===");
        ResetGameStateLocal();
    }

    /// <summary>
    /// ResetGameStateLocal - Core reset logic executed locally and via RPC
    /// </summary>
    public void ResetGameStateLocal()
    {
        Debug.Log("=== RESETTING GAME STATE ===");

        // 1. Hide game over panel and reset UI
        if (Instance != null && Instance.gameOverPanel != null)
        {
            Instance.gameOverPanel.SetActive(false);
            Debug.Log("✅ Game over panel hidden");
        }

        // 2. Reset GameManager singleton state
        if (Instance != null)
        {
            // Clear all spawned objects lists
            Instance.fishes.Clear();
            Instance.allFishes.Clear();

            // Reset game state flags
            Instance.fisherManIsSpawned = false;
            Instance.isFisherMan = false;
            Instance.goldWormEatByFish = false;
            Instance.fishermanWorms = 0;
            Instance.isRoundEnding = false;

            Debug.Log("✅ GameManager state reset");
        }
        
        if (GS.Instance != null)
        {
            GS.Instance.currentRoundWormsUsed = 0;
            GS.Instance.wormsEatenThisRound.Clear();
        }

        // 3. Reset ScoreManager/PlayFab coins lock
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetCoinSaveFlag();
        }

        // 3. Reset FishController singleton
        if (FishController.Instance != null)
        {
            FishController.Instance.isDead = false;
            FishController.Instance.canMove = true;
            FishController.Instance.catchadeFish = false;
            FishController.Instance.isFisherMan = false;
            FishController.Instance.hunger = 100;
            Debug.Log("✅ FishController state reset");
        }

        // 4. Reset FishermanController singleton
        if (FishermanController.Instance != null)
        {
            FishermanController.Instance.catchadFish = 0;
            FishermanController.Instance.isCanCast = true;
            Debug.Log("✅ FishermanController state reset");
        }

        // 5. Reset HungerSystem
        if (HungerSystem.Instance != null)
        {
            HungerSystem.Instance.canDecrease = true;
            if (HungerSystem.Instance.hungerBar != null)
            {
                HungerSystem.Instance.hungerBar.value = 100f;
            }
            Debug.Log("✅ HungerSystem reset");
        }

        // 6. Reset MashPhaseManager
        if (MashPhaseManager.Instance != null)
        {
            MashPhaseManager.Instance.active = false;
            if (MashPhaseManager.Instance.mashPanel != null)
            {
                MashPhaseManager.Instance.mashPanel.SetActive(false);
            }
            Debug.Log("✅ MashPhaseManager reset");
        }

        // 7. Reset spawners
        if (WormSpawner.Instance != null)
        {
            WormSpawner.Instance.canSpawn = true;
            Debug.Log("✅ WormSpawner reset");
        }

        if (JunkSpawner.Instance != null)
        {
            JunkSpawner.Instance.canSpawn = true;
            Debug.Log("✅ JunkSpawner reset");
        }

        // 8. Destroy all networked objects (fish, worms, junk, fisherman)
        if (GS.Instance.isLan)
        {
            Debug.Log("✅ Mirror will clean up networked objects on scene change");
        }
        else
        {
            // For Photon: Only destroy objects we own, or let scene load handle cleanup
            // When PhotonNetwork.LoadLevel is called, it will automatically clean up all networked objects
            // But we'll try to clean up what we can to avoid errors
            
            FishController[] allFish = UnityEngine.Object.FindObjectsByType<FishController>(FindObjectsSortMode.None);
            int destroyedCount = 0;
            foreach (FishController fish in allFish)
            {
                if (fish != null && fish.gameObject != null)
                {
                    PhotonView pv = fish.GetComponent<PhotonView>();
                    // Only destroy if we own it or if it's null (already destroyed)
                    if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
                    {
                        try
                        {
                            PhotonNetwork.Destroy(fish.gameObject);
                            destroyedCount++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not destroy fish: {e.Message}");
                        }
                    }
                }
            }
            Debug.Log($"✅ Destroyed {destroyedCount} fish objects (scene load will clean up the rest)");

            WormManager[] allWorms = UnityEngine.Object.FindObjectsByType<WormManager>(FindObjectsSortMode.None);
            int destroyedWorms = 0;
            foreach (WormManager worm in allWorms)
            {
                if (worm != null && worm.gameObject != null)
                {
                    PhotonView pv = worm.GetComponent<PhotonView>();
                    // Only destroy if we own it or if master client
                    if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
                    {
                        try
                        {
                            PhotonNetwork.Destroy(worm.gameObject);
                            destroyedWorms++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not destroy worm: {e.Message}");
                        }
                    }
                }
            }
            Debug.Log($"✅ Destroyed {destroyedWorms} worm objects (scene load will clean up the rest)");

            JunkManager[] allJunk = UnityEngine.Object.FindObjectsByType<JunkManager>(FindObjectsSortMode.None);
            int destroyedJunk = 0;
            foreach (JunkManager junk in allJunk)
            {
                if (junk != null && junk.gameObject != null)
                {
                    PhotonView pv = junk.GetComponent<PhotonView>();
                    // Only destroy if we own it or if master client
                    if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
                    {
                        try
                        {
                            PhotonNetwork.Destroy(junk.gameObject);
                            destroyedJunk++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not destroy junk: {e.Message}");
                        }
                    }
                }
            }
            Debug.Log($"✅ Destroyed {destroyedJunk} junk objects (scene load will clean up the rest)");

            FishermanController[] allFishermen = UnityEngine.Object.FindObjectsByType<FishermanController>(FindObjectsSortMode.None);
            int destroyedFishermen = 0;
            foreach (FishermanController fisherman in allFishermen)
            {
                if (fisherman != null && fisherman.gameObject != null)
                {
                    PhotonView pv = fisherman.GetComponent<PhotonView>();
                    // Only destroy if we own it or if master client
                    if (pv != null && (pv.IsMine || PhotonNetwork.IsMasterClient))
                    {
                        try
                        {
                            PhotonNetwork.Destroy(fisherman.gameObject);
                            destroyedFishermen++;
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Could not destroy fisherman: {e.Message}");
                        }
                    }
                }
            }
            Debug.Log($"✅ Destroyed {destroyedFishermen} fisherman objects (scene load will clean up the rest)");
        }

        Debug.Log("=== GAME STATE RESET COMPLETE ===");
    }

    private static bool isLobbyButtonPressed = false;

    public static void SetLobbyButtonPressed(bool value)
    {
        isLobbyButtonPressed = value;
    }

    public void RestartGame()
    {
        Debug.Log("=== RestartGame() CALLED ===");
        Debug.Log($"Stack trace: {System.Environment.StackTrace}");
        Debug.Log($"isLobbyButtonPressed flag: {isLobbyButtonPressed}");

        if (isLobbyButtonPressed)
        {
            Debug.LogWarning("⚠️ RestartGame() called but Lobby button was pressed - Skipping disconnect!");
            Debug.LogWarning("⚠️ Lobby button should handle scene loading without disconnecting");
            isLobbyButtonPressed = false;
            return;
        }

        if (GameOver.Instance != null && SceneManager.GetActiveScene().name == "Play")
        {
            string stackTrace = System.Environment.StackTrace;
            if (stackTrace.Contains("Button.Press") || stackTrace.Contains("Button.OnPointerClick"))
            {
                Debug.LogWarning("⚠️ RestartGame() called from button click - This might be the Lobby button!");
                Debug.LogWarning("⚠️ Lobby button should call GameOver.Lobby() instead of RestartGame()");
                Debug.LogWarning("⚠️ Skipping disconnect - Lobby button should handle this differently");
                return;
            }
        }

        StartCoroutine(RestartAfterDisconnect());
    }

    IEnumerator RestartAfterDisconnect()
    {
        Debug.Log("=== RestartAfterDisconnect() STARTED ===");
        Debug.Log($"isLan: {GS.Instance.isLan}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");

        if (GS.Instance.isLan)
        {
            Debug.Log("LAN Mode - ForceDisconnect()");
            ForceDisconnect();
        }
        else
        {
            Debug.Log("Photon Mode - Disconnecting...");
            PhotonNetwork.Disconnect();
            yield return new WaitUntil(() => PhotonNetwork.IsConnected == false);
            Debug.Log("Disconnected from Photon");
        }

        Debug.Log("Loading Dash scene...");
        SceneManager.LoadScene("Dash");
    }

    public void ForceDisconnect()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
            Debug.Log("Stopped Host");
        }
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
            Debug.Log("Stopped Server");
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
            Debug.Log("Stopped Client");
        }
    }

    public IEnumerator RestartAfterLeftRoom()
    {
        PhotonNetwork.LeaveRoom();
        yield return new WaitUntil(() => PhotonNetwork.InRoom == false);
        SceneManager.LoadScene("Dash");
    }

    public void LoadGetIdAndChangeHost()
    {
        LoadPreloderOnOff(true);
        Invoke(nameof(GetIdAndChangeHost), 4f);
    }

    public void GetIdAndChangeHost()
    {
        int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("✅ My Client ID = " + myId);
        photonView.RPC(nameof(ChangeHostById), RpcTarget.MasterClient, myId);
    }

    [PunRPC]
    public void RequestHostBack(int originalHostId)
    {
        Debug.Log($"RequestHostBack called for ID: {originalHostId}");
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }

        Player targetPlayer = null;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == originalHostId)
            {
                targetPlayer = p;
                break;
            }
        }

        if (targetPlayer != null)
        {
            Debug.Log($"Reverting Master Client to original host: {targetPlayer.NickName} (ID: {originalHostId})");
            PhotonNetwork.SetMasterClient(targetPlayer);
        }
        else
        {
            Debug.LogError($"Could not find original host with ID: {originalHostId}");
        }
    }

    public void ProcessRestart()
    {
        Debug.Log("=== ProcessRestart() CALLED ===");

        if (GS.Instance.isLan)
        {
            if (GS.Instance.IsMirrorMasterClient)
            {
                // Also reset locally on host
                ResetGameStateLocal();
                Debug.Log("✅ Loading Play scene with all players in room via Mirror");
                
                // Force all clients to load the new scene
                CustomNetworkManager.Instence.LoadPlaySceneForAll();
            }
        }
        else if (PhotonNetwork.InRoom)
        {
            // Call RPC on GameManager's PhotonView to reset all clients
            photonView.RPC(nameof(ResetGameState_RPC), RpcTarget.All);

            // Also reset locally on host
            ResetGameStateLocal();

            Debug.Log("✅ Loading Play scene with all players in room");

            // Wait for RPC to be sent and processed before loading scene
            PhotonNetwork.SendAllOutgoingCommands();

            // Start coroutine to load scene
            StartCoroutine(LoadPlaySceneAfterDelayCoroutine());
        }
        else
        {
            Debug.LogError("❌ Cannot play again - not in a room!");
        }
    }

    [PunRPC]
    public void ChangeHostById(int clientId)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("❌ Only current MasterClient can change host!");
            return;
        }

        Player targetPlayer = null;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.ActorNumber == clientId)
            {
                targetPlayer = p;
                break;
            }
        }

        if (targetPlayer != null)
        {
            PhotonNetwork.SetMasterClient(targetPlayer);
            Debug.Log("✅ Host changed to Player with ID: " + clientId);
        }
        else
        {
            Debug.LogWarning("❌ Client ID not found: " + clientId);
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("👑 New Master is: " + newMasterClient.NickName + " (ID: " + newMasterClient.ActorNumber + ")");
        
        if (PhotonNetwork.IsMasterClient)
        {
            // This client is now the master client
            Debug.Log("I am now the Master Client!");

            // Check if we were waiting for host restoration
            if (isRestoringHost)
            {
                Debug.Log("✅ Host authority restored! Proceeding with restart.");
                isRestoringHost = false;
                ProcessRestart();
                return;
            }

            if (GameOver.Instance != null)
            {
                GameOver.Instance.UpdateButtonVisibility();
            }

            if (goldWormEatByFish)
            {
                if (!fisherManIsSpawned)
                {
                    SpawnFisherman();
                }
            }
        }
        else
        {
            // This client is no longer the master client (host migration occurred)
            // This is normal when a client becomes the fisherman - do NOT trigger game over
            // The original host should continue playing as a fish
            Debug.Log("⚠️ No longer master client - Host migration occurred. Continuing to play as fish.");
            
            // Update button visibility if game over panel exists
            if (GameOver.Instance != null)
            {
                GameOver.Instance.UpdateButtonVisibility();
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("OnPlayerEnteredRoom Called");
        UpdateTablesUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log("❌ Player Left Room: " + otherPlayer.NickName + " | ID: " + otherPlayer.ActorNumber + " | current Player = " + PhotonNetwork.CurrentRoom.PlayerCount);

        if (PhotonNetwork.IsMasterClient)
        {
            int curruntPlayer = PhotonNetwork.CurrentRoom.PlayerCount;
            if (curruntPlayer <= 1)
            {
                if (fisherManIsSpawned && isFisherMan)
                {
                    FishermanController.Instance.CheckWorms();
                }
                else
                {
                    if (myFish != null)
                    {
                        Debug.Log(" OnPlayerLeftRoom CallAllWinFishRPC called");
                        myFish.WinFish();
                    }
                }
            }
        }

        UpdateTablesUI();
    }

    public void UpdateTablesUI()
    {
        if (PlayerTableManager.Instance != null)
        {
            PlayerTableManager.Instance.UpdatePlayerTable();
        }
    }

    public void LoadPreloderOnOff(bool res)
    {
        if (GS.Instance.isLan)
        {
            PreloderOnOff(res);
        }
        else
        {
            photonView.RPC(nameof(PreloderOnOff), RpcTarget.All, res);
        }
    }

    [PunRPC]
    public void PreloderOnOff(bool res)
    {
        preloderUI.SetActive(res);
    }

    public void CallFisherManSpawnedRPC(bool res)
    {
        photonView.RPC(nameof(FisherManSpawned), RpcTarget.All, res);
    }

    [PunRPC]
    public void FisherManSpawned(bool res)
    {
        fisherManIsSpawned = res;
    }

    public void CallCoverBGDisableRPC()
    {
        photonView.RPC(nameof(CoverBGDisable), RpcTarget.All);
    }

    [PunRPC]
    public void CoverBGDisable()
    {
        coverBG.SetActive(false);
    }

    public void CallLessPlayerCountRPC()
    {
        if (!GS.Instance.isLan)
        {
            photonView.RPC(nameof(LessPlayerCount), RpcTarget.MasterClient);
            PhotonNetwork.SendAllOutgoingCommands();
        }
    }

    [PunRPC]
    public void LessPlayerCount()
    {
        totalPlayers--;

        if (goldWormEatByFish) return; // Prevent game over trigger during host migration

        if (PhotonNetwork.IsMasterClient)
        {
            if (FishermanController.Instance != null)
                FishermanController.Instance.CheckWorms();
        }
    }

    public void LessPlayerCount_Mirror()
    {
        totalPlayers--;

        if (goldWormEatByFish) return; // Prevent game over trigger during host migration

        if (GS.Instance.isLan)
        {
            if (myFish.isFisherMan)
            {
                FishermanController.Instance.CheckWorms();
            }
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                FishermanController.Instance.CheckWorms();
            }
        }
    }

    public void WinFish_Mirror()
    {
        if (GS.Instance.isLan)
        {
            for (int i = 0; i < allFishes.Count; i++)
            {
                allFishes[i].CallWinFishRPC();
            }
        }
        else
        {
            // Broadcast to ALL clients (including master) that fishes have won
            // This is more reliable than iterating through the list which might be desynced
            photonView.RPC(nameof(ReceiveFishWinRPC), RpcTarget.All);
        }
    }

    [PunRPC]
    public void ReceiveFishWinRPC()
    {
        Debug.Log("ReceiveFishWinRPC called");
        
        // If I am a fish and not the fisherman, I trigger my win state
        if (myFish != null && !isFisherMan)
        {
            Debug.Log("Triggering local win for fish");
            myFish.WinFish_mirror();
        }
        else if (isFisherMan) 
        {
            // If I am the fisherman, this confirms the loss (though it might be redundant if already handled)
             Debug.Log("ReceiveFishWinRPC received by Fisherman - Logic handled in FishermanController");
        }
    }

    /// <summary>
    /// LoadPlaySceneAfterDelayCoroutine - Coroutine to wait for RPC processing before loading scene
    /// This is called from GameOver to avoid issues when GameOver GameObject becomes inactive
    /// </summary>
    public IEnumerator LoadPlaySceneAfterDelayCoroutine()
    {
        // Wait a bit longer to ensure RPC is fully processed on all clients
        yield return new WaitForSeconds(0.2f);
        
        // Double-check we're still in room before loading
        if (PhotonNetwork.InRoom && PhotonNetwork.IsConnected)
        {
            Debug.Log("Loading Play scene...");
            
            // Send RPC to all clients (including master) to ensure they reload the scene
            // Photon doesn't support reloading same scene on clients, so we need to force it via RPC
            if (photonView != null)
            {
                // Use RPC to ensure ALL clients (including master) reload the scene
                // This works around Photon's limitation with reloading the same scene
                photonView.RPC(nameof(ReloadScene_RPC), RpcTarget.All);
            }
            else
            {
                Debug.LogError("❌ Cannot send ReloadScene RPC - PhotonView is null!");
                // Fallback: just load on master
                PhotonNetwork.LoadLevel("Play");
            }
        }
        else
        {
            Debug.LogError("❌ Cannot load Play scene - Not in room or disconnected!");
        }
    }
    
    /// <summary>
    /// RPC to force all clients (including master) to reload the Play scene
    /// This is needed because Photon doesn't support reloading the same scene on clients
    /// By using RPC, we ensure all clients reload, not just the master
    /// </summary>
    [PunRPC]
    private void ReloadScene_RPC()
    {
        Debug.Log($"ReloadScene_RPC received - Reloading Play scene. IsMasterClient: {PhotonNetwork.IsMasterClient}");
        if (PhotonNetwork.InRoom)
        {
            // Force reload by loading the scene again
            // This ensures all clients reload, not just the master
            PhotonNetwork.LoadLevel("Play");
        }
        else
        {
            Debug.LogError("❌ Cannot reload scene - Not in room!");
        }
    }
}
