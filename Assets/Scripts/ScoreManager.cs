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
    public float animationDuration = 6f;

    [Tooltip("Max Y rise (pixels) for the highest scorer — they will go ABOVE this (out of box)")]
    public float maxHeight = 200f;

    [Tooltip("Extra Y the WINNER overshoots past maxHeight (the 'out of box' effect)")]
    public float winnerOvershoot = 80f;

    [Tooltip("Horizontal padding from the left/right edges of the score area")]
    public float horizontalPadding = 110f;

    [Tooltip("Ease curve for the rise animation (leave as default EaseOut if none assigned)")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // -----------------------------------------------------------------------
    // Internal state
    // -----------------------------------------------------------------------
    private bool hasSavedCoinsThisRound = false;
    private Vector2[] wrapperStartPositions;

    // -----------------------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        animationDuration = Mathf.Max(animationDuration, 9f);
        CacheWrapperStartPositions();
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
        EnsureWrapperStartPositions();
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

        Vector2[] layoutStartPositions = CalculateWrapperStartPositions(slotCount);

        // ---- 3. Initialise each wrapper (reset position, set name, hide unused) ----
        for (int i = 0; i < playerWrappers.Length; i++)
        {
            GameObject wrapper = playerWrappers[i];
            if (wrapper == null) continue;

            if (i < slotCount)
            {
                RectTransform wrapperRT = GetWrapperRect(wrapper);
                if (wrapperRT != null)
                    wrapperRT.anchoredPosition = layoutStartPositions[i];

                ResetWrapperChildOffset(wrapper);

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
            float tSmooth = tRaw * tRaw * tRaw * (tRaw * (tRaw * 6f - 15f) + 10f);
            float tEased = riseCurve != null ? riseCurve.Evaluate(tSmooth) : tSmooth;

            for (int i = 0; i < slotCount; i++)
            {
                GameObject wrapper = playerWrappers[i];
                if (wrapper == null) continue;

                RectTransform wrapperRT = GetWrapperRect(wrapper);
                if (wrapperRT != null)
                {
                    Vector2 startPosition = GetWrapperStartPosition(i, wrapperRT);
                    if (i < layoutStartPositions.Length)
                    {
                        startPosition = layoutStartPositions[i];
                    }

                    float currentY = Mathf.Lerp(startPosition.y, startPosition.y + targetY[i], tEased);
                    wrapperRT.anchoredPosition = new Vector2(startPosition.x, currentY);
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

            RectTransform wrapperRT = GetWrapperRect(wrapper);
            if (wrapperRT != null)
            {
                Vector2 startPosition = GetWrapperStartPosition(i, wrapperRT);
                if (i < layoutStartPositions.Length)
                {
                    startPosition = layoutStartPositions[i];
                }

                wrapperRT.anchoredPosition = new Vector2(startPosition.x, startPosition.y + targetY[i]);
            }

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
    /// Returns the RectTransform of the Wrapper object that physically rises.
    /// </summary>
    private RectTransform GetWrapperRect(GameObject wrapper)
    {
        return wrapper != null ? wrapper.GetComponent<RectTransform>() : null;
    }

    private void CacheWrapperStartPositions()
    {
        if (playerWrappers == null)
        {
            wrapperStartPositions = null;
            return;
        }

        wrapperStartPositions = new Vector2[playerWrappers.Length];
        for (int i = 0; i < playerWrappers.Length; i++)
        {
            RectTransform wrapperRT = GetWrapperRect(playerWrappers[i]);
            wrapperStartPositions[i] = wrapperRT != null ? wrapperRT.anchoredPosition : Vector2.zero;
        }
    }

    private void EnsureWrapperStartPositions()
    {
        if (wrapperStartPositions == null || wrapperStartPositions.Length != playerWrappers.Length)
        {
            CacheWrapperStartPositions();
        }
    }

    private Vector2 GetWrapperStartPosition(int index, RectTransform wrapperRT)
    {
        if (wrapperStartPositions != null && index >= 0 && index < wrapperStartPositions.Length)
        {
            return wrapperStartPositions[index];
        }

        return wrapperRT != null ? wrapperRT.anchoredPosition : Vector2.zero;
    }

    private Vector2[] CalculateWrapperStartPositions(int slotCount)
    {
        Vector2[] positions = new Vector2[slotCount];
        if (slotCount <= 0)
        {
            return positions;
        }

        RectTransform firstWrapperRT = slotCount > 0 ? GetWrapperRect(playerWrappers[0]) : null;
        RectTransform layoutRect = firstWrapperRT != null ? firstWrapperRT.parent as RectTransform : null;
        float width = layoutRect != null && layoutRect.rect.width > 0f ? layoutRect.rect.width : 900f;
        bool usesCenteredAnchors = firstWrapperRT != null && Mathf.Approximately(firstWrapperRT.anchorMin.x, 0.5f) && Mathf.Approximately(firstWrapperRT.anchorMax.x, 0.5f);
        float leftX = usesCenteredAnchors ? horizontalPadding - (width * 0.5f) : horizontalPadding;
        float rightX = usesCenteredAnchors ? (width * 0.5f) - horizontalPadding : Mathf.Max(leftX, width - horizontalPadding);

        for (int i = 0; i < slotCount; i++)
        {
            RectTransform wrapperRT = GetWrapperRect(playerWrappers[i]);
            Vector2 basePosition = GetWrapperStartPosition(i, wrapperRT);
            float x = slotCount == 1
                ? (usesCenteredAnchors ? 0f : width * 0.5f)
                : Mathf.Lerp(leftX, rightX, i / (float)(slotCount - 1));

            positions[i] = new Vector2(x, basePosition.y);
        }

        return positions;
    }

    private void ResetWrapperChildOffset(GameObject wrapper)
    {
        if (wrapper == null || wrapper.transform.childCount == 0)
        {
            return;
        }

        RectTransform childRT = wrapper.transform.GetChild(0).GetComponent<RectTransform>();
        if (childRT != null)
        {
            childRT.anchoredPosition = new Vector2(childRT.anchoredPosition.x, 0f);
        }
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
