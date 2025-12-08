using UnityEngine;
using UnityEngine.UI;


public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public Slider musicVolumeSlider;

    public Button backButton,controlsButton, achivementButton;

    public GameObject controlsUI, achivementsUI;

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
        controlsButton.onClick.AddListener(onControlPressed);
        achivementButton.onClick.AddListener(onAchivementsPressed);

    }

    private void OnEnable()
    {
        BackManager.instance.RegisterScreen(backButton);
    }

    private void OnVolumeChanged(float value)
    {
       
        PlayerPrefs.SetFloat(VolumeKey, value);
        PlayerPrefs.Save();
    }
   

    private void OnBackPressed()
    {
        BackManager.instance.UnregisterScreen();

        gameObject.SetActive(false);
        if (GS.Instance.isLan)
        {

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
}
