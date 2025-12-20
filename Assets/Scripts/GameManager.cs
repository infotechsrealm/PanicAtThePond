using Mirror;
using Mirror.Discovery;
using Photon.Pun;
using Photon.Realtime;
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
    public Slider castingMeter; // Assign this in Inspector

    public static GameManager Instance;

    public GameObject gameOverPanel;
    public Text gameOverText;

    [Header("Bucket Sprites")]
    public Sprite fullBucket;
    public Sprite halfBucket;
    public Sprite emptyBucket;

    [Header("UI References")]
    public Image bucketImage;   // assign bucket Image (UI Image)
    public Text wormCountText;

    public GameObject hungerBar;
    public GameObject fisherManObjects;
    public GameObject preloderUI;
    public GameObject coverBG;

    // public Transform camera;

    public FishController myFish;
    public List<FishController> allFishes = new List<FishController>();

    public Text messageText;

    internal bool fisherManIsSpawned = false;
    internal bool isFisherMan = false;
    internal bool goldWormEatByFish = false;

    public GameObject sky, water;


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
       

        SpawnPlayer();
        Invoke(nameof(setFishermanWormCounts), 2f);
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
        if (GS.Instance.isLan)
        {
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity == null)
                {
                    float x = Random.Range(minBounds.x, maxBounds.x);
                    float y = Random.Range(minBounds.y, maxBounds.y);
                    Vector3 spawnPos = new Vector3(x, y, 0);

                    GameObject fish = Instantiate(fishPrefab, spawnPos, Quaternion.identity);
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

            GameObject fish = PhotonNetwork.Instantiate(fishPrefab.name, spawnPos, Quaternion.identity);
            fishes.Add(fish);

            Debug.Log("Fish Spawned: " + fishes.Count);
        }
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
        LoadPreloderOnOff(true);
        Invoke(nameof(SpawnFisherman), 0f);
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
        }
    }

    public void ShowGameOver(string message)
    {
        Debug.Log("Game Over: " + message);
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (WormSpawner.Instance.canSpawn)
            {
                WormSpawner.Instance.canSpawn = false;
            }
        }
        else
        {
            Debug.Log("Gameover Panerl is null");
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

    // Static flag to prevent RestartGame() from disconnecting when Lobby button is pressed
    private static bool isLobbyButtonPressed = false;
    
    // Public method to set the flag (called from GameOver.Lobby())
    public static void SetLobbyButtonPressed(bool value)
    {
        isLobbyButtonPressed = value;
    }
    
    // Restart Button function
    // NOTE: This should NOT be called when Lobby button is pressed
    // The Lobby button should call GameOver.Lobby() instead
    public void RestartGame()
    {
        Debug.Log("=== RestartGame() CALLED ===");
        Debug.Log($"Stack trace: {System.Environment.StackTrace}");
        Debug.Log($"isLobbyButtonPressed flag: {isLobbyButtonPressed}");
        
        // Check if this is being called from the Lobby button (which should not disconnect)
        // The Lobby() function sets a flag before calling, so we can detect it here
        if (isLobbyButtonPressed)
        {
            Debug.LogWarning("⚠️ RestartGame() called but Lobby button was pressed - Skipping disconnect!");
            Debug.LogWarning("⚠️ Lobby button should handle scene loading without disconnecting");
            isLobbyButtonPressed = false; // Reset flag
            return; // Don't disconnect - let the Lobby button handle it
        }
        
        // Check if this is being called from the Lobby button via stack trace
        // If GameOver.Instance exists and we're in Play scene, this might be from Lobby button
        if (GameOver.Instance != null && SceneManager.GetActiveScene().name == "Play")
        {
            // Check the stack trace to see if it's from a button click
            string stackTrace = System.Environment.StackTrace;
            if (stackTrace.Contains("Button.Press") || stackTrace.Contains("Button.OnPointerClick"))
            {
                Debug.LogWarning("⚠️ RestartGame() called from button click - This might be the Lobby button!");
                Debug.LogWarning("⚠️ Lobby button should call GameOver.Lobby() instead of RestartGame()");
                Debug.LogWarning("⚠️ Skipping disconnect - Lobby button should handle this differently");
                // Don't disconnect - let the Lobby button handle it
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
            // wait jab tak disconnect complete na ho jaye
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
            // HOST (server + client)
            NetworkManager.singleton.StopHost();
            Debug.Log("Stopped Host");
        }
        else if (NetworkServer.active)
        {
            // सिर्फ SERVER
            NetworkManager.singleton.StopServer();
            Debug.Log("Stopped Server");
        }
        else if (NetworkClient.isConnected)
        {
            // सिर्फ CLIENT
            NetworkManager.singleton.StopClient();
            Debug.Log("Stopped Client");
        }
    }

    public IEnumerator RestartAfterLeftRoom()
    {
        PhotonNetwork.LeaveRoom();
        // Wait until left room completely
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
    public void ChangeHostById(int clientId)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("❌ Sirf current MasterClient hi host change kar sakta hai!");
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
        if (PhotonNetwork.IsMasterClient)
        {
            if (GameOver.Instance != null)
            {
                // Update button visibility properly (will show buttons for new master client)
                GameOver.Instance.UpdateButtonVisibility();
            }

            if (goldWormEatByFish)
            {
                if (!fisherManIsSpawned)
                {
                    SpawnFisherman();
                    Debug.Log("👑 New Master is: " + newMasterClient.NickName + " (ID: " + newMasterClient.ActorNumber + ")");
                }
            }
            else
            {
                if (myFish != null)
                {
                    for (int i = 0; i < allFishes.Count; i++)
                    {
                        if (allFishes[i] != null)
                        {
                            allFishes[i].CallAllWinFishRPC();
                        }
                    }
                }
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
        Debug.Log("❌ Player Left Room: " + otherPlayer.NickName + " | ID: " + otherPlayer.ActorNumber + " | currun Player = " + PhotonNetwork.CurrentRoom.PlayerCount);
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
            PhotonNetwork.SendAllOutgoingCommands(); // send it now
        }
    }


    //When Fish is Die  and Exit frome game 
    [PunRPC]
    public void LessPlayerCount()
    {
        totalPlayers--;
        if (PhotonNetwork.IsMasterClient)
        {
            if(FishermanController.Instance != null)
                FishermanController.Instance.CheckWorms();
        }
    }

    public void LessPlayerCount_Mirror()
    {
        totalPlayers--;

        if (GS.Instance.isLan)
        {
            if(myFish.isFisherMan)
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
        for (int i = 0; i < allFishes.Count; i++)
        {
            allFishes[i].CallWinFishRPC();
        }
    }

}
