using UnityEngine;
using UnityEngine.UI;


public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Slider musicVolumeSlider;
    public Button backButton;

    private AudioSource musicSource;

    private const string VolumeKey = "MusicVolume";

    void Start()
    {
        // Load saved volume or use default 0.5
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        musicVolumeSlider.value = savedVolume;
        

        // Hook up listener
        musicVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // Back button event
        backButton.onClick.AddListener(OnBackPressed);
    }

    private void OnVolumeChanged(float value)
    {
       
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }

   

    private void OnBackPressed()
    {
        // Return to main menu scene
        /*   Time.timeScale = 1f; // ensure unpaused
           SceneManager.LoadScene("MainMenu");*/
        gameObject.SetActive(false);
    }
}
