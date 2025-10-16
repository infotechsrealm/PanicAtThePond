using UnityEngine;

public class CreateJoinManager : MonoBehaviour
{
    public GameObject createAndJoinButtons;

    public CreatePanel createPanel;
    public JoinPanel JoinPanel;

    public PhotonLauncher launcher;

    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "Join":
                {
                    Debug.Log("action = " + action);
                    launcher.isCreating = false;
                    launcher.isJoining = true;
                    launcher.LaunchGame();
                    break;
                }

            case "Create":
                {
                    Debug.Log("action = " + action);
                    launcher.isCreating = true;
                    launcher.isJoining = false;
                    launcher.LaunchGame();
                    break;
                }

            case "Back":
                {
                    createAndJoinButtons.SetActive(false);
                    break;
                }
        }
    }

}