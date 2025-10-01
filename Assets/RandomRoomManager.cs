using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RandomRoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room Settings")]
    internal int maxPlayers = 3; // Max players per room

    [Header("References")]
    internal PhotonLauncher PhotonLauncher;

    public GameObject coustomButtons, randomButtons;
    public Text createRoomName, joinRoomName;
    public Text status;


    public bool coustomCreate;

    private void Start()
    {
        PhotonLauncher = PhotonLauncher.Instance;
        PhotonNetwork.AutomaticallySyncScene = true;

        if (coustomCreate)
        {
            coustomButtons.SetActive(true);
            randomButtons.SetActive(false);
        }
        else
        {
            coustomButtons.SetActive(false);
            randomButtons.SetActive(true);
        }
    }

    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "CreateRandom":
                {
                    CreateRandomRoom();
                    break;
                }

            case "JoinRandom":
                {
                    JoinRandomRoom();
                    break;
                }
        }
    }

    // ------------------ Create Random Room ------------------

    internal void CreateRandomRoom()
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(false);

        string randomRoomName = "Room_" + Random.Range(1000, 9999);

        RoomOptions options = new RoomOptions
        {
            IsOpen = true,
            IsVisible = true,
            Plugins = null // Must be null for PUN 2 Cloud
        };

        PhotonNetwork.CreateRoom(randomRoomName, options, TypedLobby.Default);
        Debug.Log("Creating Room: " + randomRoomName);
    }

    // ------------------  Join Random  Room ------------------
    internal void JoinRandomRoom()
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(false);

        PhotonNetwork.JoinRandomRoom();
        Debug.Log("Trying to join a random room...");
    }
}
