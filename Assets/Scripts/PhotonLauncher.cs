using Photon.Pun;
using UnityEngine;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{

    public GameObject buttons;

    public static PhotonLauncher Instance;

    public int maxPlayers = 3;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }
    void Start()
    {
        LaunchGame();
    }

    public void LaunchGame()
    {
        ShowButton(false);

        if (!PhotonNetwork.IsConnected)
        {
            // Not connected → Connect to Photon
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            // Already connected
            ShowButton(true);

            // Agar lobby me nahi ho to join karo
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
            }
            else
            {
                ShowButton(true);
                Debug.Log("Already in Lobby!");
            }
        }
    }


    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon!");


        ShowButton(true);


        PhotonNetwork.JoinLobby(); // Optional: auto join lobby
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.Log("Disconnected: " + cause);
    }

    public void ShowButton(bool isEnable)
    {
        if (buttons != null)
            buttons.SetActive(isEnable);
        else
            Debug.LogWarning("Buttons reference missing!");
    }
}
