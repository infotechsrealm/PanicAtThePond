using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{


    public static PhotonLauncher Instance;

    public int maxPlayers = 3;

    public bool isCreating= false;
    public CreateJoinManager createJoinManager;
    public GameObject buttons;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    GameObject preloder;
    public void LaunchGame()
    {
        Debug.Log("LaunchGame!");

        preloder = Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("ConnectUsingSettings");
            // Not connected → Connect to Photon
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            // Agar lobby me nahi ho to join karo
            if (!PhotonNetwork.InLobby)
            {
                Debug.Log("JoinLobby");

                PhotonNetwork.JoinLobby();
            }
            else
            {
                EnablePanel();
                Debug.Log("Already in Lobby!");
            }
        }
    }

    public void EnablePanel()
    {
        if(preloder != null)
        {
            Destroy(preloder);
        }

        createJoinManager.createAndJoinButtons.SetActive(false);
        Debug.Log("called");
        if (isCreating)
        {
            createJoinManager.createPanel.SetActive(true);
            createJoinManager.JoinPanel.SetActive(false);
        }
        else
        {
            createJoinManager.JoinPanel.SetActive(true);
            createJoinManager.createPanel.SetActive(false);
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon!");
        PhotonNetwork.JoinLobby(); // Optional: auto join lobby
    }

    public override void OnJoinedLobby()
    {
        EnablePanel();
        Debug.Log("✅ Joined Photon Lobby successfully!");
    }


    public void ShowButton(bool isEnable)
    {
        if (buttons != null)
            buttons.SetActive(isEnable);
        else
            Debug.LogWarning("Buttons reference missing!");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected: " + cause);

        DashManager dashManager = DashManager.instance;
        dashManager.coustomButtons.SetActive(false);
        dashManager.randomButtons.SetActive(false);
        dashManager.selectButtons.SetActive(false);

        if (cause == DisconnectCause.ServerTimeout ||
            cause == DisconnectCause.ExceptionOnConnect)
        {
            PhotonNetwork.Reconnect();
        }
    }
}
