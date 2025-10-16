using UnityEngine;

public class DashManager : MonoBehaviour
{
    public GameObject settingUI,quitUI,craditsUI,localPlayerUI,prefabPanret;

    public static DashManager instance;

    public GameObject createAndJoinButtons;

    private void Awake()
    {
        instance = this;
    }
    public void OnClickAction(string action)
    {
        switch (action)
        {   
            case "Play":
                {
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
        }
    }
}
