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
    }

    private void Start()
    {
        GS.Instance.SetMusicVolume();
        GS.Instance.BGMusic.Play();

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
                    break;
                }

            case "LocalPlay":
                {
                    localPlayerUI.SetActive(true);
                    break;
                }

            case "Settings":
                {
                    settingUI.SetActive(true);
                    break;
                }

            case "Credits":
                {
                    craditsUI.SetActive(true);
                    break;
                }

            case "Quit":
                {
                    quitUI.SetActive(true);
                    break;
                }  
                
            case "hints":
                {
                    hintsUI.SetActive(true);
                    break;
                }   
        }
    }


}
