using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DashManager : MonoBehaviour
{
    public GameObject createAndJoinButtons,settingUI, quitUI,craditsUI,localPlayerUI,prefabPanret,hintsUI;

    public Button createAndJoinButtonsBackButton;

    public TextMeshProUGUI CoinText;

    public static DashManager Instance;

    private void Awake()
    {
        Instance = this;
        LegacyTextSharpener.EnsureSceneTextIsSharp();
    }

    private void Start()
    {
        LegacyTextSharpener.EnsureSceneTextIsSharp();
        GS.Instance.SetMusicVolume();
        GS.Instance.BGMusic.Play();
        if (settingUI != null)
        {
            settingUI.SetActive(false);
        }

        StartCoroutine(FetchCoinsWhenReady());
    }

    private IEnumerator FetchCoinsWhenReady()
    {
        if (PlayFabManager.Instance != null && CoinText != null)
        {
            // Wait until PlayFab is fully logged in
            while (!PlayFabManager.Instance.IsLoggedIn)
            {
                yield return null; // wait to next frame
            }

            PlayFabManager.Instance.GetCurrency(amount =>
            {
                CoinText.text = amount.ToString();
            });
        }
    }
    public void OnClickAction(string action)
    {
        switch (action)
        {   
            case "Play":
                {
                    BackManager.instance.RegisterScreen(createAndJoinButtonsBackButton);
                    createAndJoinButtons.SetActive(true);
                    LegacyTextSharpener.EnsureSceneTextIsSharp();
                    break;
                }

            case "LocalPlay":
                {
                    localPlayerUI.SetActive(true);
                    LegacyTextSharpener.EnsureSceneTextIsSharp();
                    break;
                }

            case "Settings":
                {
                    OpenSettings();
                    break;
                }

            case "Credits":
                {
                    craditsUI.SetActive(true);
                    LegacyTextSharpener.EnsureSceneTextIsSharp();
                    break;
                }

            case "Quit":
                {
                    quitUI.SetActive(true);
                    LegacyTextSharpener.EnsureSceneTextIsSharp();
                    break;
                }  
                
            case "hints":
                {
                    hintsUI.SetActive(true);
                    LegacyTextSharpener.EnsureSceneTextIsSharp();
                    break;
                }   
        }
    }

    public void Click_Fish(){
        craditsUI.SetActive(true);
    }
    public void Back_Credit()
    {
        craditsUI.SetActive(false);
    }
    public void LocalPLayBack()
    {
        localPlayerUI.SetActive(false);
    }

    public void OpenSettings()
    {
        if (settingUI == null)
        {
            SettingsMenu settingsMenu = FindFirstObjectByType<SettingsMenu>(FindObjectsInactive.Include);
            if (settingsMenu != null)
            {
                settingUI = settingsMenu.gameObject;
            }
        }

        if (settingUI == null)
        {
            Debug.LogWarning("DashManager.OpenSettings called, but settingUI is not assigned.");
            return;
        }

        RectTransform rectTransform = settingUI.transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0f);
            rectTransform.SetAsLastSibling();
        }

        settingUI.SetActive(true);
        LegacyTextSharpener.EnsureSceneTextIsSharp();
    }
}
