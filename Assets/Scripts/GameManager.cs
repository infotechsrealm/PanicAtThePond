using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Player Setup")]
    public int totalPlayers = 7;
    public FishermanController fishermanPrefab;
    public GameObject fishPrefab;
    public Transform camera;

    [Header("Worm Settings")]
    public int baseWormMultiplier = 3;

    [Header("Fish Spawn Bounds")]
    public Vector2 minBounds = new Vector2(-8f, -4f);
    public Vector2 maxBounds = new Vector2(8f, 4f);

    [Header("Runtime Info")]
    public int fishermanWorms;
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

    [Header("UI")]
    public GameObject mashPanel;
    public Slider mashSlider;   // 0 = escape (fish), 1 = capture (fisherman)
    public Text mashText;

    public List<GameObject> DisabledObjects = new List<GameObject>();

    public List<FishController> AllFishPlayers = new List<FishController>();

    public FishController myFish;
    private void Awake()    
    {
        Instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    void Start()
    {
        // SetupGame();

    }
    public void EnableAll()
    {

        for (int i = 0; i < DisabledObjects.Count; i++)
        {
            if (DisabledObjects[i] != null && !DisabledObjects[i].activeSelf)
            {
                DisabledObjects[i].SetActive(true);
                Debug.Log("Is not enable");
            }
            else
            {
                Debug.Log("Is  enable");
            }
        }
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

    public FishermanController fisherman;

    public void LoadMakeFisherMan()
    {
        Debug.Log("LoadMakeFisherMan");
        Invoke(nameof(MakeFisherMan),1f);
    }

    internal void MakeFisherMan()
    {
        if (fisherman != null)
        {
            camera.transform.position = new Vector3(0f, 10f, -10f);
            fisherman.isfisherMan = true;   
            HungerSystem.instance.gameObject.SetActive(false);
            castingMeter.gameObject.SetActive(true);
            bucketImage.gameObject.SetActive(true);
            // Worm calculation
            int fishCount = totalPlayers - 1;
            fishermanWorms = fishCount * baseWormMultiplier;
            maxWorms = fishermanWorms;
            Debug.Log("Fisherman Worms: " + fishermanWorms);

            // Assign castingMeter to FishermanController
            FishermanController fc = fisherman;
            if (fc != null)
            {
                fc.castingMeter = castingMeter;
                fc.worms = fishermanWorms;
                fc.worms = 500;
                UpdateUI(fc.worms);
            }
            else
            {
                Debug.LogWarning("FishermanController not found on FishermanPrefab!");
            }
        }
    }

    public void ShowGameOverMessage(string message)
    {
        gameOverText.text = message;
    }

    // Restart Button function
    public void RestartGame()
    {
        SceneManager.LoadScene("Dash");
    }

    public void AssignFisherman(FishermanController fc)
    {
        Debug.Log("Fisherman assigned on client!");
        fisherman = fc;
    }

    //For Prepare ChangeHost

}
