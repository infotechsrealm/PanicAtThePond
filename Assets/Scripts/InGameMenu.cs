using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InGameMenu : MonoBehaviourPunCallbacks
{
    public GameObject menuPanel;       // Reference to the whole pause menu UI
    private bool isMenuOpen = false;

    private const string VolumeKey = "MusicVolume";

    public Slider musicVolumeSlider;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        musicVolumeSlider.value = savedVolume;

        // Hook up listener
        musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);
        GameManager.instance.myFish.CallGamePauseRPC(isMenuOpen);
    }

    public void QuitToMainMenu()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Dash");
    }

    private void OnVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }
    
    public void OnBackPressed()
    {
        ToggleMenu();
    }
}
