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

    public static GameManager instance;

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
    public List<FishController> allFishes = new List<FishController> ();

    public Text messageText;

    internal bool fisherManIsSpawned = false;
    internal bool isFisherMan = false;
    internal bool goldWormEatByFish = false;

    public GameObject sky, water;

    private void Awake()
    {
        instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if(PhotonNetwork.IsMasterClient)
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
        PhotonNetwork.AutomaticallySyncScene = true;
        totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        SpawnPlayer();
    } 
    public void UpdateUI(int currunt_Warms)
    {
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
        // Spawn Fish
        float x = Random.Range(minBounds.x, maxBounds.x);
        float y = Random.Range(minBounds.y, maxBounds.y);
        Vector3 spawnPos = new Vector3(x, y, 0);

        GameObject fish = PhotonNetwork.Instantiate(fishPrefab.name, spawnPos, Quaternion.identity);
        fishes.Add(fish);

        Debug.Log("Fish Spawned: " + fishes.Count);
    }

    public void LoadSpawnFisherman()
    {
      
        LoadPreloderOnOff(true);
        Invoke(nameof(SpawnFisherman),4f);
    }
   

    public void SpawnFisherman()
    {
        photonView.RPC(nameof(FisherManSpawned), RpcTarget.All, true);
        PhotonNetwork.Instantiate("FisherMan", new Vector3(0f, 3.15f, 0f), Quaternion.identity);
        int fishCount = totalPlayers - 1;
        fishermanWorms = fishCount * baseWormMultiplier;
        maxWorms = fishermanWorms;
        Debug.Log("Fisherman Worms: " + fishermanWorms);
    }

    public void ShowGameOver(string message)
    {
        if (gameOverPanel != null)
        {
            if (preloderUI.activeSelf)
            {
                preloderUI.SetActive(false);
            }
            gameOverPanel.SetActive(true);
            if (WormSpawner.instance.canSpawn)
            {
                WormSpawner.instance.canSpawn = false;
            }
        }

        if (gameOverText != null)
        {
            gameOverText.text = message;
        }
    }

    // Restart Button function
    public void RestartGame()
    {
        StartCoroutine(RestartAfterDisconnect());
    }

    IEnumerator RestartAfterDisconnect()
    {
        PhotonNetwork.Disconnect();
        // wait jab tak disconnect complete na ho jaye
        yield return new WaitUntil(() => PhotonNetwork.IsConnected == false);
        SceneManager.LoadScene("Dash");
    }

    public IEnumerator RestartAfterLeftRoom()
    {
        PhotonNetwork.LeaveRoom();
        // Wait until left room completely
        yield return new WaitUntil(() => PhotonNetwork.InRoom == false);
        SceneManager.LoadScene("Dash");
    }

    public void GetIdAndChangeHost()
    {
        int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("✅ My Client ID = " + myId);

        photonView.RPC(nameof(ChangeHostById), RpcTarget.MasterClient, myId);
    }

    public void LoadGetIdAndChangeHost()
    {
        LoadPreloderOnOff(true);

        Invoke(nameof(GetIdAndChangeHost), 4f);
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
        if (PhotonNetwork.IsMasterClient )
        {
                if (GameOver.Instance != null)
                {
                    GameOver.Instance.playAgainBtn.SetActive(true);
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
                    FishermanController.Instence.CheckWorms();
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
        if (PlayerTableManager.instance != null)
        {
            PlayerTableManager.instance.UpdatePlayerTableUI();
        }
    }

    public void LoadPreloderOnOff(bool res)
    {
        photonView.RPC(nameof(PreloderOnOff), RpcTarget.All, res);
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
        photonView.RPC(nameof(LessPlayerCount), RpcTarget.MasterClient);
        PhotonNetwork.SendAllOutgoingCommands(); // send it now
    }
    //When Fish is Die  and Exit frome game 
    [PunRPC]
    public void LessPlayerCount()
    {
        totalPlayers--;
        if(PhotonNetwork.IsMasterClient)
        {
            FishermanController.Instence.CheckWorms();
        }
    }
}
