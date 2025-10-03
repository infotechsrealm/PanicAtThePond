using UnityEngine;


public class DashManager : MonoBehaviour
{
    public GameObject coustomButtons, randomButtons,selectButtons,randomeRoomManager,coustomeRoomManager;
    

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
            PhotonLauncher.Instance.buttons = coustomButtons;
        }
        else
        {
            randomButtons.SetActive(true);
            coustomButtons.SetActive(false);
            coustomeRoomManager.SetActive(false);
            selectButtons.SetActive(false);
            PhotonLauncher.Instance.buttons = randomButtons;
        }
    }
}
