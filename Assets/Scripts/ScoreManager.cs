using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Photon.Pun;
using TMPro;

public class ScoreManager : MonoBehaviourPunCallbacks
{
    public static ScoreManager Instance;

    [Header("UI References")]
    public GameObject winScreensContainer;
    public GameObject scoreScreen;
    public GameObject winnerScreen;

    [Header("Player Wrappers (ScoreScreen) — Assign Wrapper1 to Wrapper7")]
    public GameObject[] playerWrappers;

    [Header("Winner UI (WinnerScreen)")]
    public TextMeshProUGUI winnerNameText;
    public TextMeshProUGUI winnerScoreText;

    [Header("Buttons (WinnerScreen)")]
    public Button playAgainBtn;
    public Button lobbyButton;

    [Header("Dropdown References")]
    public Dropdown gameModeDropdown;

    [Header("Animation Settings")]
    [Tooltip("How long the bar-rise animation plays")]
    public float animationDuration = 2f;

    [Tooltip("Max Y rise (pixels) for the highest scorer — they will go ABOVE this (out of box)")]
    public float maxHeight = 200f;

    [Tooltip("Extra Y the WINNER overshoots past maxHeight (the 'out of box' effect)")]
    public float winnerOvershoot = 80f;

    [Tooltip("Ease curve for the rise animation (leave as default EaseOut if none assigned)")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // -----------------------------------------------------------------------
    // Internal state
    // -----------------------------------------------------------------------
    private bool hasSavedCoinsThisRound = false;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        HideAllScreens();
    }

    private void Start()
    {
        if (playAgainBtn != null)
        {
            playAgainBtn.onClick.RemoveAllListeners();
            playAgainBtn.onClick.AddListener(OnPlayAgainClicked);
        }
        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(OnLobbyClicked);
        }
    }

    // -----------------------------------------------------------------------
    // Screen helpers
    // -----------------------------------------------------------------------

    public void HideAllScreens()
    {
        if (winScreensContainer != null) winScreensContainer.SetActive(false);
        if (scoreScreen != null)        scoreScreen.SetActive(false);
        if (winnerScreen != null)       winnerScreen.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Score Screen
    // -----------------------------------------------------------------------

    public void ShowScoreScreen(Dictionary<string, int> currentScores)
    {
        HideAllScreens();
        if (winScreensContainer != null) winScreensContainer.SetActive(true);
        if (scoreScreen != null)         scoreScreen.SetActive(true);

        StartCoroutine(AnimateChests(currentScores));
    }

    private IEnumerator AnimateChests(Dictionary<string, int> scores)
    {
        // ---- 1. Find the highest score so we can scale everything ----
        int highestScore = 1;
        string winnerName = "";
        foreach (var kvp in scores)
        {
            if (kvp.Value > highestScore)
            {
                highestScore = kvp.Value;
                winnerName   = kvp.Key;
            }
        }

        // ---- 2. Pre-calculate target Y for every player ----
        //         The highest scorer gets maxHeight + winnerOvershoot (pops out of box).
        //         Everyone else is scaled relative to maxHeight.
        int   slotCount     = 0;
        float[] targetY     = new float[playerWrappers.Length];
        int[]   targetScore = new int[playerWrappers.Length];
        string[] slotNames  = new string[playerWrappers.Length];

        foreach (var kvp in scores)
        {
            if (slotCount >= playerWrappers.Length) break;

            slotNames[slotCount]  = kvp.Key;
            targetScore[slotCount] = kvp.Value;

            bool isWinner = (kvp.Key == winnerName);
            float ratio   = kvp.Value / (float)highestScore; // 0..1

            if (isWinner)
                targetY[slotCount] = maxHeight + winnerOvershoot;   // out-of-box
            else
                targetY[slotCount] = ratio * maxHeight;              // proportional

            slotCount++;
        }

        // ---- 3. Initialise each wrapper (reset position, set name, hide unused) ----
        for (int i = 0; i < playerWrappers.Length; i++)
        {
            GameObject wrapper = playerWrappers[i];
            if (wrapper == null) continue;

            if (i < slotCount)
            {
                // Reset the Player child to Y = 0
                RectTransform playerRT = GetPlayerRect(wrapper);
                if (playerRT != null)
                    playerRT.anchoredPosition = new Vector2(playerRT.anchoredPosition.x, 0f);

                // Set name / score labels
                foreach (TextMeshProUGUI t in wrapper.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (t.gameObject.name.Contains("Name"))  t.text = slotNames[i];
                    if (t.gameObject.name.Contains("Score")) t.text = "0";
                }

                wrapper.SetActive(true);
            }
            else
            {
                wrapper.SetActive(false);
            }
        }

        // ---- 4. Animate the rise ----
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float tRaw  = Mathf.Clamp01(elapsed / animationDuration);
            float tEased = riseCurve.Evaluate(tRaw); // smooth easing

            for (int i = 0; i < slotCount; i++)
            {
                GameObject wrapper = playerWrappers[i];
                if (wrapper == null) continue;

                // Move Player child upward
                RectTransform playerRT = GetPlayerRect(wrapper);
                if (playerRT != null)
                {
                    float currentY = Mathf.Lerp(0f, targetY[i], tEased);
                    playerRT.anchoredPosition = new Vector2(playerRT.anchoredPosition.x, currentY);
                }

                // Animate score counter
                int shownScore = Mathf.RoundToInt(Mathf.Lerp(0, targetScore[i], tEased));
                foreach (TextMeshProUGUI tx in wrapper.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (tx.gameObject.name.Contains("Score")) tx.text = shownScore.ToString();
                }
            }

            yield return null;
        }

        // ---- 5. Snap to final values ----
        for (int i = 0; i < slotCount; i++)
        {
            GameObject wrapper = playerWrappers[i];
            if (wrapper == null) continue;

            RectTransform playerRT = GetPlayerRect(wrapper);
            if (playerRT != null)
                playerRT.anchoredPosition = new Vector2(playerRT.anchoredPosition.x, targetY[i]);

            foreach (TextMeshProUGUI tx in wrapper.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tx.gameObject.name.Contains("Score")) tx.text = targetScore[i].ToString();
            }
        }

        // ---- 6. Brief pause, then transition ----
        yield return new WaitForSeconds(3f);
        OnScoreScreenComplete();
    }

    /// <summary>
    /// Returns the RectTransform of the FIRST child inside a Wrapper
    /// (i.e., the "Player" GameObject that physically rises).
    /// </summary>
    private RectTransform GetPlayerRect(GameObject wrapper)
    {
        if (wrapper.transform.childCount == 0) return null;
        return wrapper.transform.GetChild(0).GetComponent<RectTransform>();
    }

    private void OnScoreScreenComplete()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.HandleEndOfRoundTransition();
    }

    // -----------------------------------------------------------------------
    // Winner Screen
    // -----------------------------------------------------------------------

    public void ShowWinnerScreen(string winnerName, int winnerScore)
    {
        HideAllScreens();

        if (winScreensContainer != null) winScreensContainer.SetActive(true);
        if (winnerScreen != null)        winnerScreen.SetActive(true);

        if (winnerNameText  != null) winnerNameText.text  = winnerName;
        if (winnerScoreText != null) winnerScoreText.text = winnerScore.ToString();

        // Show host-only buttons
        bool isHost = false;
        if (GS.Instance != null)
            isHost = GS.Instance.isLan ? GS.Instance.IsMirrorMasterClient : GS.Instance.isMasterClient;

        if (playAgainBtn != null) playAgainBtn.gameObject.SetActive(isHost);
        if (lobbyButton  != null) lobbyButton.gameObject.SetActive(isHost);

        // Award coins
        if (GS.Instance != null)
            GS.Instance.wormCoins += winnerScore;

        SaveWormCoinsToPlayFab(winnerScore);
    }

    // -----------------------------------------------------------------------
    // Buttons
    // -----------------------------------------------------------------------

    public void OnPlayAgainClicked()
    {
        Debug.Log("ScoreManager: OnPlayAgainClicked");
        if (GS.Instance != null && GS.Instance.wormCoins > 0)
            SaveWormCoinsToPlayFab(GS.Instance.wormCoins);

        if (GameManager.Instance != null)
            GameManager.Instance.ProcessRestart();
    }

    public void OnLobbyClicked()
    {
        Debug.Log("ScoreManager: OnLobbyClicked");
        if (GS.Instance != null && GS.Instance.wormCoins > 0)
            SaveWormCoinsToPlayFab(GS.Instance.wormCoins);

        if (GS.Instance != null)
        {
            bool isHost = GS.Instance.isLan ? GS.Instance.IsMirrorMasterClient : GS.Instance.isMasterClient;
            GS.Instance.dropDownChangeAvalable = isHost;
            
            if (!isHost && gameModeDropdown != null)
            {
                gameModeDropdown.interactable = false;
            }
        }

        if (GS.Instance.isLan)
        {
            if (Mirror.NetworkServer.active)
                Mirror.NetworkManager.singleton.ServerChangeScene("Dash");
        }
        else
        {
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LoadLevel("Dash");
        }
    }

    // -----------------------------------------------------------------------
    // PlayFab
    // -----------------------------------------------------------------------

    public void SaveWormCoinsToPlayFab(int coinsToAdd)
    {
        if (hasSavedCoinsThisRound) return;
        hasSavedCoinsThisRound = true;

        Debug.Log($"ScoreManager: SaveWormCoinsToPlayFab — adding {coinsToAdd} coins.");
        if (PlayFabManager.Instance != null && coinsToAdd > 0)
            PlayFabManager.Instance.AddCurrency(coinsToAdd);
    }

    public void ResetCoinSaveFlag()
    {
        hasSavedCoinsThisRound = false;
    }
}