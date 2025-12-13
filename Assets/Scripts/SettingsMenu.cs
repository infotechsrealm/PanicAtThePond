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
        if(GS.Instance.isLan)
        {
            GameManager.Instance.myFish.DestroyThisGameobject();
        }
        else
        {
            PhotonNetwork.LeaveRoom();
        }
        SceneManager.LoadScene("Dash");
    }

    public void Quit()
    {
        quitUI.SetActive(true);
    }
}
