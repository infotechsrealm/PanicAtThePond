using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{


    public static PhotonLauncher Instance;

    public int maxPlayers = 3;

    public bool isCreating= false;
    public bool isJoining = false;
    public CreateJoinManager createJoinManager;
    public GameObject buttons;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }


    public void LaunchGame()
    {
        Debug.Log("LaunchGame!");
        if (Preloader.instance == null)
        {
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }
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
        if (Preloader.instance != null)
        {
            Destroy(Preloader.instance.gameObject);
        }

        Debug.Log("called");

        if (isCreating)
        {
            createJoinManager.createPanel.gameObject.SetActive(true);
            createJoinManager.JoinPanel.gameObject.SetActive(false);
        }
        else if(isJoining)
        {
            createJoinManager.JoinPanel.gameObject.SetActive(true);
            createJoinManager.createPanel.gameObject.SetActive(false);
            Debug.Log("EnablePanel Called");
        }
    }

    public override void OnConnectedToMaster()
    {
        if(Preloader.instance == null)
        {
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }
        Debug.Log("Connected to Photon!");
        PhotonNetwork.JoinLobby(); // Optional: auto join lobby
    }

    public override void OnJoinedLobby()
    {
        EnablePanel();
        Debug.Log("✅ Joined Photon Lobby successfully!");
    }


    

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected: " + cause);

        if (cause == DisconnectCause.ServerTimeout ||
            cause == DisconnectCause.ExceptionOnConnect)
        {
            PhotonNetwork.Reconnect();
        }
    }
}
