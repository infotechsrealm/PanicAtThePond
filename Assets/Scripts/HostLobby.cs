using Mirror;
using Mirror.BouncyCastle.Asn1.Crmf;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class HostLobby : MonoBehaviourPunCallbacks 
{

    public PlayerTableManager playerTableManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public Button backButton, controlsButton,hintButton,pauseButton;

    public GameObject hintUI, controlUI,pauseUI;

    private void Start()
    {
        controlsButton.onClick.AddListener(onControlPressed);
        hintButton.onClick.AddListener(onHintPressed);
        pauseButton.onClick.AddListener(pause);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        BackManager.instance.RegisterScreen(pauseButton);
        playerTableManager.UpdatePlayerTable();
        //GS.Instance.rerfeshDropDown();
    }
    private void onControlPressed()
    {
        controlUI.SetActive(true);
    }
    private void onHintPressed()
    {
        hintUI.SetActive(true);
    }

    private void pause()
    {
        pauseUI.SetActive(true);
    }
    public void Close()
    {
        BackManager.instance.UnregisterScreen();

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
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
            // 1️⃣ पहले discovery बंद करो ताकि broadcast रुक जाए
            LANDiscoveryMenu.Instance.networkDiscovery.StopDiscovery();

            // 2️⃣ फिर host बंद करो (इससे server + client दोनों बंद हो जाते हैं)
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }

            gameObject.SetActive(false);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room successfully!");
    }
}
