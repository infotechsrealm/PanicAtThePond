using ExitGames.Client.Photon;
using Mirror;
using Mirror.Discovery;
using Photon.Pun;
using Steamworks;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnityThread
{
    public static SynchronizationContext MainThread;
}

public class GS : MonoBehaviour  
{
    private static readonly string[] SteamAchievementIds =
    {
        "SOLO_ARTIST",
        "SURVIVOR",
        "EARTH_PRAISER",
        "WHAT_A_SNACK",
        "FISH_SLAYER",
        "WE_COME_IN_SWARMS",
        "GULPER"
    };

    public static GS Instance;

    public NetworkDiscovery networkDiscovery;
    public NetworkManager networkManager;

    public GameObject createAndJoinPanel,
                      howToPlay,
                      preloder;

    [Header("Tie Preloader")]
    public GameObject tiePreloder;
    public float tiePreloderReturnDelay = 7f;

    private bool isTiePreloderReturnRunning = false;
    private bool shouldShowTiePreloderOnDash = false;
    private bool isTiePreloderVisible = false;
    public bool ShouldShowTiePreloderOnDash => shouldShowTiePreloderOnDash;


    [SerializeField]
    public GameObject passwordPopupPrefab; // Assign in Inspector

    public bool ClearWaters;
    public bool MurkyWaters;
    public bool DeepWaters;
    public bool ReflectiveWater;

    public bool isFullscreen;

    public bool dropDownChangeAvalable = false;

    public string nickName = "";

    internal bool isMasterClient;

    internal int totlePlayers;

    public bool isLan = false,IsMirrorMasterClient = false;
    
    [Header("Game Mode / Score Tracking")]
    public int currentGameMode = 0; // 0 = Quick Survivalist, 1 = Quick Cast, 2 = Deep Sea Fishing
    public int currentRound = 1;
    public System.Collections.Generic.Dictionary<string, int> playerScores = new System.Collections.Generic.Dictionary<string, int>();
    public int wormCoins = 0;
    public ScoreSystemSettings scoreSystemSettings = new ScoreSystemSettings();

    [Header("Achievement Tracking")]
    public int currentRoundWormsUsed = 0;
    public System.Collections.Generic.Dictionary<string, int> hooksEscaped = new System.Collections.Generic.Dictionary<string, int>();
    public System.Collections.Generic.Dictionary<string, int> wormsEatenThisRound = new System.Collections.Generic.Dictionary<string, int>();

    private Callback<UserStatsStored_t> steamStatsStoredCallback;
    private Callback<UserStatsReceived_t> steamStatsReceivedCallback;
    private bool steamAchievementSyncRequested;


    public AudioSource BGMusic;
    private void Awake()
    {
        Instance = this;
        UnityThread.MainThread = SynchronizationContext.Current;
        if (scoreSystemSettings == null)
        {
            scoreSystemSettings = new ScoreSystemSettings();
        }

        scoreSystemSettings.FillBlankValuesWithDefaults();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Dash")
        {
            ResetGameState();
            if (!IsInActiveLobby())
            {
                ResetScoreSystemSettings();
            }

            if (shouldShowTiePreloderOnDash)
            {
                StartCoroutine(ShowPendingTiePreloderOnDash());
            }
        }
        else if (scene.name == "Play")
        {
            LoadScoreSystemSettingsFromPhotonRoomProperties();
        }
    }

    public void ResetGameState()
    {
        currentRound = 1;
        playerScores.Clear();
        currentRoundWormsUsed = 0;
        hooksEscaped.Clear();
        wormsEatenThisRound.Clear();
        Debug.Log("🔄 GS Game State Reset: Round is now 1, scores cleared.");
    }

    public void ResetScoreSystemSettings()
    {
        if (scoreSystemSettings == null)
        {
            scoreSystemSettings = new ScoreSystemSettings();
        }

        scoreSystemSettings.Reset();
        BroadcastScoreSystemSettingsIfHost();
    }

    private bool IsInActiveLobby()
    {
        if (isLan)
        {
            return NetworkServer.active || NetworkClient.isConnected;
        }

        return PhotonNetwork.InRoom;
    }

    public void BroadcastScoreSystemSettingsIfHost()
    {
        if (scoreSystemSettings == null)
        {
            return;
        }

        scoreSystemSettings.FillBlankValuesWithDefaults();

        if (isLan)
        {
            if (IsMirrorMasterClient && CustomNetworkManager.Instence != null)
            {
                CustomNetworkManager.Instence.BroadcastScoreSystemSettings(scoreSystemSettings);
            }

            return;
        }

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            Hashtable roomProperties = scoreSystemSettings.ToPhotonProperties();
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProperties);
        }
    }

    public void LoadScoreSystemSettingsFromPhotonRoomProperties()
    {
        if (isLan || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            return;
        }

        if (scoreSystemSettings == null)
        {
            scoreSystemSettings = new ScoreSystemSettings();
        }

        scoreSystemSettings.ApplyPhotonProperties(PhotonNetwork.CurrentRoom.CustomProperties);
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        nickName = "Player_" + UnityEngine.Random.Range(100, 999);
        RegisterSteamAchievementCallbacks();
        StartCoroutine(SyncUnlockedAchievementsToSteamAfterDelay());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            // Toggle Fullscreen
            ChangeScreenMode();
        }
    }

    public void ChangeScreenMode()
    {
        if (Screen.fullScreen)
        {
            // Go to Windowed Mode
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
            isFullscreen = false;
        }
        else
        {
            // Go to Exclusive Fullscreen to remove black lines
            Resolution currentRes = Screen.currentResolution;
            Screen.SetResolution(currentRes.width, currentRes.height, FullScreenMode.ExclusiveFullScreen);
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            Screen.fullScreen = true;
            isFullscreen = true;
        }
    }

    public void SetSFXVolume(AudioSource audioSource)
    {
        float Volume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (audioSource.volume != Volume)
        {
            audioSource.volume = Volume;
        }
    }

    public void SetMusicVolume()
    {
        float Volume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        if (BGMusic.volume != Volume)
        {
            BGMusic.volume = Volume;
        }
    }

    public void GeneratePreloder(Transform transform)
    {
        if (Preloader.Instence == null)
        {
            Debug.Log("Generating Preloader -------------");
            Instantiate(preloder, transform);
        }
    }

    public void DestroyPreloder(bool force = false)
    {
      //  Debug.Log("Trying to Destroy Preloader ............");
        if (isTiePreloderVisible && !force)
        {
            return;
        }

        if (Preloader.Instence != null)
        {
           // Debug.Log("Destroying Preloader ==============");
            Destroy(Preloader.Instence.gameObject);
        }
        else
        {
           // Debug.Log("No Preloader found to destroy.");
        }

        isTiePreloderVisible = false;
    }

    public void GenerateTiePreloder(Transform parent = null)
    {
        if (tiePreloder == null)
        {
            Debug.LogWarning("Tie preloader prefab is not assigned on GS.");
            return;
        }

        DestroyPreloder(true);

        if (parent != null)
        {
            Instantiate(tiePreloder, parent, false);
        }
        else
        {
            Instantiate(tiePreloder);
        }

        isTiePreloderVisible = true;
    }

    public void ShowTiePreloderAndReturnToLobby()
    {
        if (isTiePreloderReturnRunning)
        {
            return;
        }

        StartCoroutine(ShowTiePreloderAndReturnToLobbyCoroutine());
    }

    public void MarkTiePreloderForDash()
    {
        shouldShowTiePreloderOnDash = true;
    }

    public void ReturnToLobbyWithPendingTiePreloder()
    {
        MarkTiePreloderForDash();
        dropDownChangeAvalable = isLan ? IsMirrorMasterClient : isMasterClient;

        if (isLan)
        {
            if (NetworkServer.active && NetworkManager.singleton != null)
            {
                NetworkManager.singleton.ServerChangeScene("Dash");
            }
        }
        else if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Dash");
        }
    }

    private System.Collections.IEnumerator ShowTiePreloderAndReturnToLobbyCoroutine()
    {
        isTiePreloderReturnRunning = true;
        GenerateTiePreloder();

        yield return new WaitForSeconds(GetTiePreloderDelay());

        dropDownChangeAvalable = isLan ? IsMirrorMasterClient : isMasterClient;

        if (isLan)
        {
            if (NetworkServer.active && NetworkManager.singleton != null)
            {
                NetworkManager.singleton.ServerChangeScene("Dash");
            }
        }
        else if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Dash");
        }

        isTiePreloderReturnRunning = false;
    }

    private System.Collections.IEnumerator ShowPendingTiePreloderOnDash()
    {
        shouldShowTiePreloderOnDash = false;

        float elapsed = 0f;
        while ((DashManager.Instance == null || DashManager.Instance.prefabPanret == null) && elapsed < 3f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Transform parent = DashManager.Instance != null && DashManager.Instance.prefabPanret != null
            ? DashManager.Instance.prefabPanret.transform
            : null;

        GenerateTiePreloder(parent);

        yield return new WaitForSeconds(GetTiePreloderDelay());
        DestroyPreloder(true);
    }

    private float GetTiePreloderDelay()
    {
        return Mathf.Max(7f, tiePreloderReturnDelay);
    }

    public void rerfeshDropDown()
    {
      dropDownChangeAvalable = false;
        int index = 0;
        if (ClearWaters)
        {
            index = 0;
        }
        else if (MurkyWaters)
        {
            index = 1;
        }
        else if (DeepWaters)
        {
            index = 2;
        }
        else if (ReflectiveWater)
        {
            index = 3;
        }

        DropdownHandler dropdownHandler = DropdownHandler.Instance;
        dropdownHandler.OnDropdownChanged(index);
        dropdownHandler.waterDropdown.value = index;   // dropdown option index set karega
        dropdownHandler.waterDropdown.RefreshShownValue();   // UI ko update karega
    }

    public void SyncUnlockedAchievementsToSteamDataAdmin(string reason = "manual")
    {
        Debug.Log($"[GS][SteamAchievementSync] Start. Reason: {reason}");
        Debug.Log($"[GS][SteamAchievementSync] SteamManager.Initialized = {SteamManager.Initialized}");

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[GS][SteamAchievementSync] Steam is not initialized. Local unlocked achievements remain saved in PlayerPrefs only.");
            LogLocalAchievementState();
            return;
        }

        RegisterSteamAchievementCallbacks();

        Debug.Log($"[GS][SteamAchievementSync] Steam persona = {SteamFriends.GetPersonaName()}");
        Debug.Log($"[GS][SteamAchievementSync] SteamID = {SteamUser.GetSteamID()}");
        Debug.Log($"[GS][SteamAchievementSync] AppID = {SteamUtils.GetAppID()}");

        Debug.Log("[GS][SteamAchievementSync] RequestCurrentStats() is not exposed by this Steamworks.NET version; current-user stats are handled by the Steam client before game startup.");

        int localUnlockedCount = 0;
        int setAchievementSuccessCount = 0;

        for (int i = 0; i < SteamAchievementIds.Length; i++)
        {
            string achievementId = SteamAchievementIds[i];
            bool localUnlocked = PlayerPrefs.GetInt("Achievement_" + achievementId, 0) == 1;
            bool steamGetSuccess = SteamUserStats.GetAchievement(achievementId, out bool steamUnlocked);

            Debug.Log($"[GS][SteamAchievementSync] {achievementId}: local={localUnlocked}, steamGetSuccess={steamGetSuccess}, steamAlreadyUnlocked={steamUnlocked}");

            if (!localUnlocked)
            {
                continue;
            }

            localUnlockedCount++;

            if (steamGetSuccess && steamUnlocked)
            {
                Debug.Log($"[GS][SteamAchievementSync] {achievementId}: already unlocked in Steam backend.");
                continue;
            }

            bool setResult = SteamUserStats.SetAchievement(achievementId);
            Debug.Log($"[GS][SteamAchievementSync] SetAchievement({achievementId}) returned {setResult}");
            if (setResult)
            {
                setAchievementSuccessCount++;
            }
        }

        if (localUnlockedCount == 0)
        {
            Debug.Log("[GS][SteamAchievementSync] No locally unlocked achievements found to send.");
            return;
        }

        bool storeResult = SteamUserStats.StoreStats();
        steamAchievementSyncRequested = storeResult;
        Debug.Log($"[GS][SteamAchievementSync] StoreStats() returned {storeResult}. LocalUnlocked={localUnlockedCount}, NewlySetThisSync={setAchievementSuccessCount}");
        Debug.Log("[GS][SteamAchievementSync] If StoreStats returned true, wait for UserStatsStored_t callback below to confirm Steam accepted the write.");
    }

    public void UnlockAchievementAndSyncToSteam(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId))
        {
            Debug.LogWarning("[GS][SteamAchievementSync] UnlockAchievementAndSyncToSteam called with empty achievement id.");
            return;
        }

        bool alreadyLocalUnlocked = PlayerPrefs.GetInt("Achievement_" + achievementId, 0) == 1;
        PlayerPrefs.SetInt("Achievement_" + achievementId, 1);
        PlayerPrefs.Save();

        Debug.Log($"[GS][SteamAchievementSync] Local achievement marked unlocked: {achievementId}. AlreadyLocalUnlocked={alreadyLocalUnlocked}");

        if (!SteamManager.Initialized)
        {
            Debug.LogWarning($"[GS][SteamAchievementSync] Steam is not initialized. {achievementId} saved locally and will sync next time Steam is available.");
            return;
        }

        RegisterSteamAchievementCallbacks();

        bool setResult = SteamUserStats.SetAchievement(achievementId);
        bool storeResult = SteamUserStats.StoreStats();
        steamAchievementSyncRequested = storeResult;

        Debug.Log($"[GS][SteamAchievementSync] Immediate sync for {achievementId}: SetAchievement={setResult}, StoreStats={storeResult}");
    }

    private System.Collections.IEnumerator SyncUnlockedAchievementsToSteamAfterDelay()
    {
        yield return null;
        yield return new WaitForSeconds(1f);
        SyncUnlockedAchievementsToSteamDataAdmin("GS.Start");
    }

    private void RegisterSteamAchievementCallbacks()
    {
        if (!SteamManager.Initialized)
        {
            return;
        }

        if (steamStatsStoredCallback == null)
        {
            steamStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnSteamUserStatsStored);
            Debug.Log("[GS][SteamAchievementSync] Registered UserStatsStored_t callback.");
        }

        if (steamStatsReceivedCallback == null)
        {
            steamStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnSteamUserStatsReceived);
            Debug.Log("[GS][SteamAchievementSync] Registered UserStatsReceived_t callback.");
        }
    }

    private void OnSteamUserStatsStored(UserStatsStored_t callback)
    {
        Debug.Log($"[GS][SteamAchievementSync] UserStatsStored_t callback. gameID={callback.m_nGameID}, result={callback.m_eResult}, syncWasRequested={steamAchievementSyncRequested}");
        steamAchievementSyncRequested = false;

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[GS][SteamAchievementSync] Steam rejected StoreStats. Result={callback.m_eResult}. Check Steam Data Admin achievement API names and app configuration.");
        }
        else
        {
            Debug.Log("[GS][SteamAchievementSync] Steam accepted StoreStats. Unlocked achievements were sent to Steam backend.");
        }
    }

    private void OnSteamUserStatsReceived(UserStatsReceived_t callback)
    {
        Debug.Log($"[GS][SteamAchievementSync] UserStatsReceived_t callback. gameID={callback.m_nGameID}, result={callback.m_eResult}");
    }

    private void LogLocalAchievementState()
    {
        for (int i = 0; i < SteamAchievementIds.Length; i++)
        {
            string achievementId = SteamAchievementIds[i];
            bool localUnlocked = PlayerPrefs.GetInt("Achievement_" + achievementId, 0) == 1;
            Debug.Log($"[GS][SteamAchievementSync] Local PlayerPrefs Achievement_{achievementId} = {localUnlocked}");
        }
    }

}
