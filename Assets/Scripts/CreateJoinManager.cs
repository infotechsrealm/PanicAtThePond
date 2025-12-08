using Mirror;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
public class CreateJoinManager : MonoBehaviourPunCallbacks
{
    public static CreateJoinManager Instance;

    public CoustomeRoomManager coustomeRoomManager;

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

    public Text requirePlayerText;
    private void Awake()
    {
        Instance = this;
    }


    private void Update()
    {
        /*if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Is Connected");
        }
        else
        {
            Debug.Log("Is Disconnected");
        }*/
    }

    private void Start()
    {

    }
    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "Join":
                {
                    GS.Instance.isLan = LAN.isOn;

                    Debug.Log("action = " + action);
                    isCreating = false;
                    isJoining = true;
                    LaunchGame();
                    if(GS.Instance.isLan)
                    {
                        GS.Instance.IsMirrorMasterClient = isCreating;
                    }
                    break;
                }

            case "Create":
                {
                    GS.Instance.isLan = LAN.isOn;

                    Debug.Log("action = " + action);
                    isCreating = true;
                    isJoining = false;
                    LaunchGame();
                    if (GS.Instance.isLan)
                    {
                        GS.Instance.IsMirrorMasterClient = isCreating;
                    }
                    break;
                }


            case "Continue":
                {
                    if (GS.Instance.isLan)
                    {
                        LANDiscoveryMenu.Instance.HostGame();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instance.CreateCustomeRoom();
                        }
                        else
                        {
                            GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "JoinRandom":
                {
                    isJoinRandom = true;
                    isJoinCustome = false;

                    if (GS.Instance.isLan)
                    {
                       // LANDiscoveryMenu.Instance.FindGames();
                    }
                    else
                    {
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instance.JoinRandomAvailableRoom();
                        }
                        else
                        {
                            GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "JoinCustome":
                {
                    isJoinCustome = true;
                    isJoinRandom = false;

                    if (GS.Instance.isLan)
                    {
                        LANDiscoveryMenu.Instance.FindGames();
                    }
                    else
                    {

                        Debug.Log("Joining Custome Room");
                        if (PhotonNetwork.IsConnected)
                        {
                            CoustomeRoomManager.Instance.JoinCustomeRoom();
                        }
                        else
                        {
                            GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

                            PhotonNetwork.ConnectUsingSettings();
                        }
                    }
                    break;
                }

            case "Back":
                {
                    BackManager.instance.UnregisterScreen();
                    createAndJoinButtons.SetActive(false);
                    break;
                }

            case "Start":
                {
                    if (GS.Instance.isLan)
                    {
                        if (NetworkServer.active)
                        {
                            NetworkManager.singleton.ServerChangeScene("Play");
                        }
                    }
                    else
                    {
                        CoustomeRoomManager.Instance.customeStartGame();
                    }
                    break;
                }
        }
    }

    public void LaunchGame()
    {
        if (!GS.Instance.isLan)
        {
            if (!PhotonNetwork.IsConnected)
            {
                GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

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
            GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

            createPanel.gameObject.SetActive(false);
            JoinPanel.gameObject.SetActive(true);

            if (GS.Instance.isLan)
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

       // createAndJoinButtons.SetActive(false);
        Debug.Log("Disconnected from Photon. Cause: " + cause);
    }

    // --------------------------------------- Photon Networking ---------------------------------------


    [PunRPC]
    public void CallRpcFromClientSide()
    {
        SetVissiblity_Photon_RPC();
    }

    public void SetVissiblity_Photon_RPC()
    {
        GS gsObj = GS.Instance;
        photonView.RPC(nameof(SetVisibilityPhoton), RpcTarget.Others, gsObj.ReflectiveWater, gsObj.DeepWaters, gsObj.MurkyWaters, gsObj.ClearWaters);
        PhotonNetwork.SendAllOutgoingCommands();
    }

    [PunRPC]
    public void SetVisibilityPhoton(bool reflectiveWater, bool deepWaters, bool murkyWaters, bool clearWaters)
    {
        GS gsObj = GS.Instance;

        gsObj.ClearWaters = clearWaters;
        gsObj.MurkyWaters = murkyWaters;
        gsObj.DeepWaters = deepWaters;
        gsObj.ReflectiveWater = reflectiveWater;

        Debug.Log($"[GS] Visibility updated: All={reflectiveWater}, Deep={deepWaters}, Murky={murkyWaters}, Clear={clearWaters}");

        GS.Instance.rerfeshDropDown();
    }

}