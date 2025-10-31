using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class CreateJoinManager : MonoBehaviourPunCallbacks
{
    public GameObject createAndJoinButtons;

    public CreatePanel createPanel;
    public JoinPanel JoinPanel;

    internal bool isCreating = false;
    internal bool isJoining = false;
    internal bool isJoinRandom = false;
    internal bool isJoinCustome = false;

    public Toggle LAN;

    public static CreateJoinManager Instence;

    private void Awake()
    {
        Instence = this;
    }

    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "Join":
                {
                    Debug.Log("action = " + action);
                    isCreating = false;
                    isJoining = true;
                    LaunchGame();
                    break;
                }

            case "Create":
                {
                    Debug.Log("action = " + action);
                    isCreating = true;
                    isJoining = false;
                    LaunchGame();
                    break;
                }


            case "Continue":
                {
                    if (LAN.isOn)
                    {
                        LANConnector.Instence.StartHost();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instence.CreateCustomeRoom();
                        }
                        else
                        {
                            if (Preloader.instance == null)
                            {
                                Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
                            }
                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "JoinRandom":
                {
                    isJoinRandom = true;
                    isJoinCustome = false;

                    if (LAN.isOn)
                    {
                        LANConnector.Instence.StartClient();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instence.JoinRandomAvailableRoom();
                        }
                        else
                        {
                            if (Preloader.instance == null)
                            {
                                Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
                            }
                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "JoinCustome":
                {
                    isJoinCustome = true;
                    isJoinRandom = false;

                    if (LAN.isOn)
                    {
                        LANConnector.Instence.StartHost();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instence.JoinCustomeRoom();
                        }
                        else
                        {
                            if (Preloader.instance == null)
                            {
                                Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
                            }
                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "Back":
                {
                    if(PhotonNetwork.IsConnected)
                    {
                        PhotonNetwork.Disconnect();
                    }
                    else
                    {
                        createAndJoinButtons.SetActive(false);
                    }
                    break;
                }
        }
    }

    public void LaunchGame()
    {
        if (!LAN.isOn)
        {
            if (!PhotonNetwork.IsConnected)
            {
                if (Preloader.instance == null)
                {
                    Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
                }
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        if (isCreating)
        {
            createPanel.gameObject.SetActive(true);
            JoinPanel.gameObject.SetActive(false);
        }
        else if (isJoining)
        {
            JoinPanel.gameObject.SetActive(true);
            createPanel.gameObject.SetActive(false);
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(); 
    }

    public override void OnJoinedLobby()
    {
        if (Preloader.instance !=null)
        {
            Destroy(Preloader.instance.gameObject);
        }
        Debug.Log("✅ Joined Photon Lobby successfully!");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (Preloader.instance != null)
        {
            Destroy(Preloader.instance.gameObject);
        }
        createAndJoinButtons.SetActive(false);
        Debug.Log("Disconnected from Photon. Cause: " + cause);
    }

}