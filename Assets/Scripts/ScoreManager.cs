using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Photon.Pun;
using TMPro;
// using PlayFab;
// using PlayFab.ClientModels; // Uncomment when PlayFab SDK is imported

public class ScoreManager : MonoBehaviourPunCallbacks
{
    public static ScoreManager Instance;

    [Header("UI References")]
    public GameObject winScreensContainer;
    public GameObject scoreScreen;
    public GameObject winnerScreen;

    [Header("Player Chests (ScoreScreen)")]
    public GameObject[] playerWrappers; // Assign Wrapper1 to Wrapper7 here

    [Header("Winner UI (WinnerScreen)")]
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI winnerScoreText;

    [Header("Animation Settings")]
    public float animationDuration = 2f;
    public float maxHeight = 200f; // Adjust this in inspector
    public float maxPointsReference = 100f; // Score that represents maxHeight

    private void Awake()
    {
        Instance = this;
        HideAllScreens();
    }

    public void HideAllScreens()
    {
        if (winScreensContainer != null) winScreensContainer.SetActive(false);
        if (scoreScreen != null) scoreScreen.SetActive(false);
        if (winnerScreen != null) winnerScreen.SetActive(false);
    }

    public void ShowScoreScreen(Dictionary<string, int> currentScores)
    {
        HideAllScreens();
        
        if (winScreensContainer != null) winScreensContainer.SetActive(true);
        if (scoreScreen != null) scoreScreen.SetActive(true);

        StartCoroutine(AnimateChests(currentScores));
    }

    private IEnumerator AnimateChests(Dictionary<string, int> scores)
    {
        int index = 0;
        float[] targetHeights = new float[playerWrappers.Length];

        int highestScore = 1; // Default to 1 to avoid division by zero
        foreach (var kvp in scores)
        {
            if (kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
            }
        }

        foreach (var kvp in scores)
        {
            if (index >= playerWrappers.Length) break;

            string pName = kvp.Key;
            int score = kvp.Value;

            float targetH = (score / (float)highestScore) * maxHeight;
            targetHeights[index] = targetH;
            
            GameObject wrapper = playerWrappers[index];
            if (wrapper != null)
            {
                if (wrapper.transform.childCount > 0)
                {
                    RectTransform chestNode = wrapper.transform.GetChild(0).GetComponent<RectTransform>();
                    if (chestNode != null)
                    {
                        chestNode.anchoredPosition = new Vector2(chestNode.anchoredPosition.x, 0);
                    }
                }
                
                TextMeshProUGUI[] texts = wrapper.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach(var t in texts)
                {
                    if (t.gameObject.name.Contains("Name")) t.text = pName;
                    if (t.gameObject.name.Contains("Score")) t.text = "0";
                }

                wrapper.SetActive(true);
            }

            index++;
        }

        for (int i = index; i < playerWrappers.Length; i++)
        {
            if (playerWrappers[i] != null) playerWrappers[i].SetActive(false);
        }

        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            index = 0;
            foreach (var kvp in scores)
            {
                if (index >= playerWrappers.Length) break;

                int score = kvp.Value;
                int currentShownScore = Mathf.RoundToInt(Mathf.Lerp(0, score, t));
                float currentH = Mathf.Lerp(0, targetHeights[index], t);

                GameObject wrapper = playerWrappers[index];
                if (wrapper != null)
                {
                    if (wrapper.transform.childCount > 0)
                    {
                        RectTransform chestNode = wrapper.transform.GetChild(0).GetComponent<RectTransform>();
                        if (chestNode != null)
                        {
                            chestNode.anchoredPosition = new Vector2(chestNode.anchoredPosition.x, currentH);
                        }
                    }

                    TextMeshProUGUI[] texts = wrapper.GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach(var tx in texts)
                    {
                        if (tx.gameObject.name.Contains("Score")) tx.text = currentShownScore.ToString();
                    }
                }

                index++;
            }
            yield return null;
        }

        // Force final values ensuring counter hits target precisely
        index = 0;
        foreach (var kvp in scores)
        {
            if (index >= playerWrappers.Length) break;
            
            int score = kvp.Value;
            float targetH = targetHeights[index];

            GameObject wrapper = playerWrappers[index];
            if (wrapper != null)
            {
                if (wrapper.transform.childCount > 0)
                {
                    RectTransform chestNode = wrapper.transform.GetChild(0).GetComponent<RectTransform>();
                    if (chestNode != null)
                    {
                        chestNode.anchoredPosition = new Vector2(chestNode.anchoredPosition.x, targetH);
                    }
                }
                TextMeshProUGUI[] texts = wrapper.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach(var tx in texts)
                {
                    if (tx.gameObject.name.Contains("Score")) tx.text = score.ToString();
                }
            }
            index++;
        }

        yield return new WaitForSeconds(3f);
        OnScoreScreenComplete();
    }

    private void OnScoreScreenComplete()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleEndOfRoundTransition();
        }
    }

    public void ShowWinnerScreen(string winnerName, int winnerScore)
    {
        HideAllScreens();

        if (winScreensContainer != null) winScreensContainer.SetActive(true);
        if (winnerScreen != null) winnerScreen.SetActive(true);

        if (winnerNameText != null) winnerNameText.text = winnerName;
        if (winnerScoreText != null) winnerScoreText.text = winnerScore.ToString();

        // Convert the points to worms coins in GS and save to Playfab
        if(GS.Instance != null)
        {
            GS.Instance.wormCoins += winnerScore; // Or total score logic
        }

        SaveWormCoinsToPlayFab(winnerScore); 
    }

    public void SaveWormCoinsToPlayFab(int coinsToAdd)
    {
        Debug.Log("ScoreManager: SaveWormCoinsToPlayFab called with " + coinsToAdd + " coins.");
        /*
        // UNCOMMENT THIS ONCE PLAYFAB SDK IS IMPORTED
        var request = new PlayFab.ClientModels.AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = "WC", // "WC" is your Worm Coins currency code
            Amount = coinsToAdd
        };
        PlayFab.PlayFabClientAPI.AddUserVirtualCurrency(request, 
            result => Debug.Log("Successfully awarded " + coinsToAdd + " Worm Coins!"),
            error => Debug.LogError("Error awarding Worm Coins: " + error.GenerateErrorReport()));
        */
    }
}
