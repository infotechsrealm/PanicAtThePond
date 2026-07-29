using Mirror;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

using PanicAtThePond.Managers;
using PanicAtThePond.Controllers;
using PanicAtThePond.Gameplay;
using PanicAtThePond.Shop;
using PanicAtThePond.Data;
using PanicAtThePond.Utilities;

namespace PanicAtThePond.UI
{
public class ClientLobby : MonoBehaviourPunCallbacks
{
    [SerializeField] private PlayerTableManager playerTableManager;

    public Button backButton;

    public Button controlsButton, hintButton, pauseButton;

    public GameObject hintUI, controlUI, pauseUI;

    private void Start()
    {
        controlsButton.onClick.AddListener(onControlPressed);
        hintButton.onClick.AddListener(onHintPressed);
        pauseButton.onClick.AddListener(pause);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        BackManager.EnsureInstance().RegisterScreen(pauseButton);
        playerTableManager.UpdatePlayerTable();
        if (GS.Instance.isLan)
        {
            if(GS.Instance.IsMirrorMasterClient)
            {

            }
        }
        else
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(CreateJoinManager.Instance.CallRpcFromClientSide), RpcTarget.MasterClient);
            }
        }
    }
    private void pause()
    {
        if (DashManager.Instance != null)
        {
            if (DashManager.Instance.settingUI != null && DashManager.Instance.settingUI.activeSelf)
            {
                SettingsMenu settings = DashManager.Instance.settingUI.GetComponent<SettingsMenu>();
                if (settings != null && settings.backButton != null)
                {
                    settings.backButton.onClick.Invoke();
                }
                else
                {
                    DashManager.Instance.settingUI.SetActive(false);
                }
            }
            else
            {
                DashManager.Instance.OpenSettings();
            }
        }
        else if (pauseUI != null)
        {
            pauseUI.SetActive(!pauseUI.activeSelf);
        }
    }
    private void onControlPressed()
    {
        controlUI.SetActive(true);
    }
    private void onHintPressed()
    {
        hintUI.SetActive(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        playerTableManager.ResetTable();
    }

    public void Close()
    {
        BackManager.EnsureInstance().UnregisterScreen();
        ResetScoreSystemAfterLeavingLobby();

        if (PhotonNetwork.InRoom)
        {
            if(PhotonNetwork.IsMasterClient)
            {
                CoustomeRoomManager.Instance.CallLeaveRoom();
            }
            else
            {
                Debug.Log("Leaving room...");
                GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);
                gameObject.SetActive(false);
                PhotonNetwork.LeaveRoom();
            }
        }
        else
        {
           

            LANDiscoveryMenu lanDiscoveryMenu = LANDiscoveryMenu.Instance;

            lanDiscoveryMenu.networkDiscovery.StopDiscovery();

            if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }

            /*   var transport = (TelepathyTransport)NetworkManager.singleton.transport;
            transport.Shutdown();*/

            lanDiscoveryMenu.DiscoveredServerInfo.port = 0;
            lanDiscoveryMenu.DiscoveredServerInfo.baseBroadcastPort = 0;

            lanDiscoveryMenu.isConnected = false;

            lanDiscoveryMenu.CallDiscoverAllLANHosts_Unlimited();

            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        ResetScoreSystemAfterLeavingLobby();
        Debug.Log("Left room successfully!");
    }

    private static void ResetScoreSystemAfterLeavingLobby()
    {
        if (GS.Instance != null)
        {
            GS.Instance.ResetScoreSystemSettings();
        }
    }
}

}