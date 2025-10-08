using UnityEngine;


public class DashManager : MonoBehaviour
{
    public GameObject prefabPanret,coustomButtons, randomButtons,backButton,selectButtons,randomeRoomManager,coustomeRoomManager;

    public static DashManager instance;


    private void Awake()
    {
        instance = this;
    }
    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "Coustome":
                {
                    EnableButtons(true);
                    break;
                }

            case "Random":
                {
                    EnableButtons(false);
                    break;
                }

            case "Back":
                {
                    Back();
                    break;
                }


            case "Play":
                {
                    Instantiate(GS.instance.createAndJoinPanel, prefabPanret.transform);
                    break;
                }

            case "LocalPlay":
                {
                    break;
                }

            case "Settings":
                {
                    break;
                }

            case "Credits":
                {
                    break;
                }

            case "Quit":
                {
                    break;
                }

        }
    }

    void EnableButtons(bool res)
    {
        if (res)
        {
            coustomButtons.SetActive(true);
            randomButtons.SetActive(false);
            randomeRoomManager.SetActive(false);
            selectButtons.SetActive(false);
            backButton.SetActive(true);
            PhotonLauncher.Instance.buttons = coustomButtons;
        }
        else
        {
            randomButtons.SetActive(true);
            coustomButtons.SetActive(false);
            coustomeRoomManager.SetActive(false);
            selectButtons.SetActive(false);
            backButton.SetActive(true);

            PhotonLauncher.Instance.buttons = randomButtons;
        }
    }

    void Back()
    {
        coustomButtons.SetActive(false);
        randomButtons.SetActive(false);

        coustomeRoomManager.SetActive(true);
        randomeRoomManager.SetActive(true);
        selectButtons.SetActive(true);
        backButton.SetActive(false);

        PhotonLauncher.Instance.buttons = null;
    }
}
