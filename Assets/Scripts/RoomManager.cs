using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room Settings")]
    internal int maxPlayers = 3; // Max players per room

    [Header("References")]
    internal PhotonLauncher PhotonLauncher;

    public GameObject coustomButtons, randomButtons;
    public Text createRoomName, joinRoomName;

    public bool coustomCreate;

    private void Start()
    {
        PhotonLauncher = PhotonLauncher.Instance;
        PhotonNetwork.AutomaticallySyncScene = true;

        if(coustomCreate)
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
            case "Create":
                {
                    CreateCustomeRoom();
                    break;
                }

            case "Join":
                {
                    JoinCustomeRoom();
                    break;
                }
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

    // ------------------ Create Custome Room ------------------

    internal void CreateCustomeRoom()
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(false);
        string roomName = createRoomName.text;
        Debug.Log("roomName = " + roomName);
        RoomOptions options = new RoomOptions
        {
            IsOpen = true,
            IsVisible = true,
            Plugins = null // Must be null for PUN 2 Cloud
        };
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    // ------------------ Join Custome Room  ------------------


    internal void JoinCustomeRoom()
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(false);
        string roomName = joinRoomName.text;

        RoomOptions options = new RoomOptions
        {
            IsOpen = true,
            IsVisible = true,
            Plugins = null // Must be null for PUN 2 Cloud
        };
        Debug.Log("roomName = " + roomName);

        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);

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

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("No available room, creating new one...");
    }

    // ------------------ Room Callbacks ------------------
    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created: " + PhotonNetwork.CurrentRoom.Name +
                  " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(true);

        Debug.LogError("Room Creation Failed: " + message);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
                  " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
                  " | MaxPlayers: " + maxPlayers);


        if (PhotonNetwork.InRoom)
        {
            int myId = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log("My Client ID = " + myId);
        }
        else
        {
            Debug.Log("❌ Abhi tak room me nahi ho!");
        }

        // If room full, lock it and load PlayScene (only MasterClient)
        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            Debug.Log("Room is now full. No more players can join.");

            //photonView.RPC("LoadPlaySceneMasterClient", RpcTarget.MasterClient);
            photonView.RPC(nameof(LoadPlaySceneMasterClient), RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void LoadPlaySceneMasterClient()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient loading Play Scene...");
            PhotonNetwork.LoadLevel("Play"); // Replace with your Play Scene name
        }
    }

}
