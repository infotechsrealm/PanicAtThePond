using UnityEngine;

public class CreateJoinManager : MonoBehaviour
{

    public GameObject  createPanel, JoinPanel, createAndJoinButtons;

    public PhotonLauncher launcher;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "Join":
                {
                    launcher.isCreating = false;
                    launcher.LaunchGame();
                    break;
                }

            case "Create":
                {
                    launcher.isCreating = true;
                    launcher.LaunchGame();
                    break;
                }

            case "Close":
                {
                    Destroy(gameObject);
                    break;
                }
        }
    }
}
