using Photon.Pun;
using UnityEngine;

public class CreateJoinManager : MonoBehaviour
{

    public GameObject createPanel, JoinPanel, createAndJoinButtons;

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

                    Debug.Log("action = " + action);

                    launcher.isCreating = false;
                    launcher.LaunchGame();
                    break;
                }

            case "Create":
                {
                    Debug.Log("action = " + action);


                    launcher.isCreating = true;
                    launcher.LaunchGame();
                    break;
                }

            case "Close":
                {
                    Close();
                    break;
                }
        }
    }

    public void Close()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log("in room");
            PhotonNetwork.LeaveRoom();
        }
        Destroy(gameObject);

    }



}
