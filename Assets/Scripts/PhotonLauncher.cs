using Photon.Pun;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{

    public GameObject buttons;

    public static PhotonLauncher Instance;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {
        ShowButton(false);
        PhotonNetwork.ConnectUsingSettings(); // Auto connect to Photon Cloud
    }

    public override void OnConnectedToMaster()
    {
        UnityEngine.Debug.Log("Connected to Photon!");
        ShowButton(true);
        PhotonNetwork.JoinLobby(); // Optional: auto join lobby
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        UnityEngine.Debug.Log("Disconnected: " + cause);
    }

    public void ShowButton(bool isEnable)
    {
        buttons.SetActive(isEnable);
    }
}
