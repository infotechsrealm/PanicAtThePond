using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
public class CreateJoinManager : MonoBehaviourPunCallbacks
{
    public static CreateJoinManager Instence;

    public GameObject createAndJoinButtons;

    public CreatePanel createPanel;
    public JoinPanel JoinPanel;

    public ClientLobby  clientLobby;
    public HostLobby hostLobby;


    internal bool isCreating = false;
    internal bool isJoining = false;
    internal bool isJoinRandom = false;
    internal bool isJoinCustome = false;

    public Toggle LAN;

    public Button joinRandomBtn;


    private void Awake()
    {
        Instence = this;


        if (LAN.isOn)
        {
            joinRandomBtn.interactable = false;
        }
        else
        {
            joinRandomBtn.interactable = true;
        }
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
                        LANDiscoveryMenu.Instance.HostGame();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instence.CreateCustomeRoom();
                        }
                        else
                        {
                            GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);

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
                       // LANDiscoveryMenu.Instance.FindGames();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instence.JoinRandomAvailableRoom();
                        }
                        else
                        {
                            GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);

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
                        LANDiscoveryMenu.Instance.FindGames();
                    }
                    else
                    {

                        Debug.Log("Joining Custome Room");
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instence.JoinCustomeRoom();
                        }
                        else
                        {
                            GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);

                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "howToPlay":
                {
                    Instantiate(GS.Instance.howToPlay, transform);
                    break;
                }

            case "Back":
                {
                    createAndJoinButtons.SetActive(false);
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
                GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);

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
            GS.Instance.GeneratePreloder(DashManager.instance.prefabPanret.transform);

            JoinPanel.gameObject.SetActive(true);
            createPanel.gameObject.SetActive(false);

            if (LAN.isOn)
            {
                LANDiscoveryMenu.Instance.CallDiscoverAllLANHosts_Unlimited();
            }

        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(); 
    }

    public override void OnJoinedLobby()
    {
        GS.Instance.DestroyPreloder();

        Debug.Log("✅ Joined Photon Lobby successfully!");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        GS.Instance.DestroyPreloder();

        createAndJoinButtons.SetActive(false);
        Debug.Log("Disconnected from Photon. Cause: " + cause);
    }

}