using UnityEngine;


public class DashManager : MonoBehaviour
{
    public GameObject prefabPanret,coustomButtons, randomButtons,backButton,selectButtons,randomeRoomManager,coustomeRoomManager;

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
}
