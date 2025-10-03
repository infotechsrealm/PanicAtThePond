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
    internal int totalPlayers;
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

    public Transform camera;

    public FishController myFish;
    public List<FishController> allFishes = new List<FishController> ();

    public Text messageText;

    public bool fisherManIsSpawned = false;
    public bool isFisherMan = false;

    private void Awake()
    {
        instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    void Start()
    {
        totalPlayers = PhotonLauncher.Instance.maxPlayers;
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
        // Spawn Fisherman
        photonView.RPC("FisherManSpawned", RpcTarget.All, true);

        PhotonNetwork.Instantiate("FisherMan", new Vector3(0f, 10f, 0f), Quaternion.identity);

        camera.position = new Vector3(0f, 10f, -10f);
        // Worm calculation
        int fishCount = totalPlayers-1;
        fishermanWorms = fishCount * baseWormMultiplier;
        maxWorms = fishermanWorms;
        Debug.Log("Fisherman Worms: " + fishermanWorms);
    }

    public void ShowGameOver(string message)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = message;
        }
    }

    // Restart Button function
    public void RestartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(RestartAfterDisconnect());
        }
        else
        {
            // Agar master client nahi ho to simply room leave karo
            PhotonNetwork.LeaveRoom();
        }
    }

    IEnumerator RestartAfterDisconnect()
    {
        PhotonNetwork.Disconnect();
        // wait jab tak disconnect complete na ho jaye
        yield return new WaitUntil(() => PhotonNetwork.IsConnected == false);
        SceneManager.LoadScene("Dash");
    }

    // Ye callback use karo non-master clients ke liye bhi safe restart karne ke liye
    public override void OnLeftRoom()
    {
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
        if (!fisherManIsSpawned && isFisherMan)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                SpawnFisherman();
            }
            Debug.Log("👑 New Master is: " + newMasterClient.NickName + " (ID: " + newMasterClient.ActorNumber + ")");
        }
        else 
        {
            if(myFish!=null)
            {
                myFish.CallAllWinFishRPC();
            }
        }


        // 👉 Yaha apna custom logic add kar sakte ho
        // Example: UI update, extra permissions, game flow change, etc.
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

}
