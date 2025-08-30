﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Player Setup")]
    public int totalPlayers = 7;
    public GameObject fishermanPrefab;
    public GameObject fishPrefab;

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

    private void Awake()
    {
        instance = this;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    void Start()
    {
       // SetupGame();
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

    void SetupGame()
    {
        // Spawn Fish
        for (int i = 0; i < 7; i++)
        {
            float x = Random.Range(minBounds.x, maxBounds.x);
            float y = Random.Range(minBounds.y, maxBounds.y);
            Vector3 spawnPos = new Vector3(x, y, 0);

            GameObject fish = Instantiate(fishPrefab, spawnPos, Quaternion.identity);
            fish.name = "Fish_" + (i + 1);
            fishes.Add(fish);
        }

        Debug.Log("Fish Spawned: " + fishes.Count);
    }

    public void SpawnFisherman()
    {
        // Spawn Fisherman
        GameObject fisherman = Instantiate(fishermanPrefab, new Vector3(0f, 1.75f, 0f), Quaternion.identity);
        fisherman.name = "Fisherman";

        // Worm calculation
        int fishCount = totalPlayers - 1;
        fishermanWorms = fishCount * baseWormMultiplier;
        maxWorms = fishermanWorms;
        Debug.Log("Fisherman Worms: " + fishermanWorms);

        // Assign castingMeter to FishermanController
        FishermanController fc = fisherman.GetComponent<FishermanController>();
        if (fc != null)
        {
            fc.castingMeter = castingMeter;
            fc.worms = fishermanWorms;
            UpdateUI(fc.worms);
        }
        else
        {
            Debug.LogWarning("FishermanController not found on FishermanPrefab!");
        }
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
