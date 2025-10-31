using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    public static PhotonLauncher Instance;

    public int maxPlayers = 3;

    internal bool isCreating= false;
    internal bool isJoining = false;

    public CreateJoinManager createJoinManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void LaunchGame()
    {
        if (Preloader.instance == null)
        {
            Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
        }

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void EnablePanel()
    {
        if (Preloader.instance != null)
        {
            Destroy(Preloader.instance.gameObject);
        }

        if (isCreating)
        {
            createJoinManager.createPanel.gameObject.SetActive(true);
            createJoinManager.JoinPanel.gameObject.SetActive(false);
        }
        else if(isJoining)
        {
            createJoinManager.JoinPanel.gameObject.SetActive(true);
            createJoinManager.createPanel.gameObject.SetActive(false);
        }
    }

    public override void OnConnectedToMaster()
    {
        if(Preloader.instance == null)
        {
            Instantiate(GS.Instance.preloder, DashManager.instance.prefabPanret.transform);
        }
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