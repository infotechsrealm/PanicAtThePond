using UnityEngine;
using UnityEngine.UI;

public class DashManager : MonoBehaviour
{
    public GameObject createAndJoinButtons,settingUI, quitUI,craditsUI,localPlayerUI,prefabPanret,hintsUI;

    public Button createAndJoinButtonsBackButton;

    public static DashManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GS.Instance.BGMusic.Play();
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
