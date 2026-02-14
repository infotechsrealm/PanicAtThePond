using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Slider masterVolumeSlider, musicVolumeSlider, sfxVolumeSlider;

    public Button backButton, controlsButton, achivementButton, modeButton, lobbyButton,closeButton;

    public GameObject controlsUI, achivementsUI, quitUI;

    public Text modeButtonText;

    void Start()
    {
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicVolumeSlider.value = savedMusicVolume;
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        float savedSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
        sfxVolumeSlider.value = savedMusicVolume;
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        float savedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        masterVolumeSlider.value = savedMusicVolume;
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        // Back button event
        backButton.onClick.AddListener(OnBackPressed);
        controlsButton.onClick.AddListener(onControlPressed);
        achivementButton.onClick.AddListener(onAchivementsPressed);
        modeButton.onClick.AddListener(ChangeMode);
        lobbyButton.onClick.AddListener(GotoLobby);
        closeButton.onClick.AddListener(Quit);
        modeButtonText.text = GS.Instance.isFullscreen ? "Windowed Mode" : "Fullscreen Mode";

        // Set lobby button interactability based on whether player is Host
        UpdateLobbyButtonState();
    }

    private void UpdateLobbyButtonState()
    {
        bool isHost = false;

        if (GS.Instance.isLan)
        {
            isHost = GS.Instance.IsMirrorMasterClient;
        }
        else
        {
            isHost = GS.Instance.isMasterClient;
        }

        // Only Host can use the lobby button to return all players to lobby
        lobbyButton.interactable = isHost;
    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }

    private void OnMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
        GS.Instance.SetMusicVolume();
    }

    private void OnSFXVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    private void OnMasterVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();

        OnSFXVolumeChanged(value);
        sfxVolumeSlider.value = value;

        OnMusicVolumeChanged(value);
        musicVolumeSlider.value = value;
    }


    private void OnBackPressed()
    {
        BackManager.instance.UnregisterScreen();

        gameObject.SetActive(false);
        if (GS.Instance.isLan)
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.myFish != null)
                {
                    GameManager.Instance.myFish.fishController_Mirror.CallGamePause(false);
                }
            }
        }
        else
        {
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.myFish != null)
                {
                    GameManager.Instance.myFish.CallGamePauseRPC(false);
                }
            }
        }
    }

    private void onControlPressed()
    {
        controlsUI.SetActive(true);
    }
    private void onAchivementsPressed()
    {
        achivementsUI.SetActive(true);
    }

    public void ChangeMode()
    {
        GS.Instance.ChangeScreenMode();
        modeButtonText.text = GS.Instance.isFullscreen ? "Windowed Mode" : "Fullscreen Mode";
    }

    public void GotoLobby()
    {
        Debug.Log("=== LOBBY BUTTON CLICKED (Settings Menu) ===");
        Debug.Log($"IsMasterClient: {PhotonNetwork.IsMasterClient}");
        Debug.Log($"isMasterClient (original): {GS.Instance.isMasterClient}");
        Debug.Log($"InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"IsConnected: {PhotonNetwork.IsConnected}");

        if (!GS.Instance.isLan && !GS.Instance.isMasterClient)
        {
            Debug.LogWarning("⚠️ Lobby button clicked but not original host - This should not happen!");
            return;
        }

        if (GS.Instance.isLan)
        {
            Debug.Log("LAN Mode - Using ServerChangeScene");

            if (Mirror.NetworkServer.active)
            {
                Debug.Log("Server is active - Loading Dash scene");
                Mirror.NetworkManager.singleton.ServerChangeScene("Dash");
            }
            else
            {
                Debug.LogWarning("Server is not active - Cannot load Dash scene");
            }
        }
        else
        {
            Debug.Log("Photon Mode - Using LoadLevel");

            bool hasRoomInfo = PhotonNetwork.CurrentRoom != null;
            bool inRoom = PhotonNetwork.InRoom;
            bool isConnected = PhotonNetwork.IsConnected;

            Debug.Log($"Room state check - InRoom: {inRoom}, HasRoomInfo: {hasRoomInfo}, IsConnected: {isConnected}");

            if (GS.Instance.isMasterClient && PhotonNetwork.IsMasterClient)
            {
                if (hasRoomInfo && isConnected)
                {
                    Debug.Log("✅ Loading Dash scene - returning to lobby with all players in room");
                    Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
                    PhotonNetwork.LoadLevel("Dash");
                }
                else if (inRoom)
                {
                    Debug.Log("✅ Loading Dash scene - returning to lobby with all players in room");
                    Debug.Log($"Room Name: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
                    PhotonNetwork.LoadLevel("Dash");
                }
                else if (isConnected)
                {
                    Debug.LogWarning("⚠️ Connected but no room info - Attempting to load Dash anyway");
                    PhotonNetwork.LoadLevel("Dash");
                }
                else
                {
                    Debug.LogError("❌ Cannot load lobby - not connected!");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ Lobby button clicked but not master client or original host");
            }
        }
    }

    public void Quit()
    {
        quitUI.SetActive(true);
    }
}
