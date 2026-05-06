using ExitGames.Client.Photon;
using Mirror;
using Mirror.Discovery;
using Photon.Pun;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UnityThread
{
    public static SynchronizationContext MainThread;
}

public class GS : MonoBehaviour  
{

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

}
