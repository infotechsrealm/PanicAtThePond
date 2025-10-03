using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CoustomeRoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room Settings")]
    internal int maxPlayers; // Max players per room

    [Header("References")]
    internal PhotonLauncher PhotonLauncher;

    public Text createRoomName, joinRoomName;
    public Text status; 

    public Text playersListText; // Assign in Inspector
    public Text waitingText;

    private void Start()
    {
        PhotonLauncher = PhotonLauncher.Instance;
        maxPlayers = PhotonLauncher.maxPlayers;
        PhotonNetwork.AutomaticallySyncScene = true;
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
        }
    }

    // ------------------ Create Custome Room ------------------
    internal void CreateCustomeRoom()
    {
        if(createRoomName.text == "")
        {
            return;
        }
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

        RoomStatus("RoomName = '" + roomName + "' Trying to create Room...", false);
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    // ------------------ Join Custome Room ------------------
    internal void JoinCustomeRoom()
    {
        if (joinRoomName.text == "")
        {
            return;
        }

        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(false);

        string roomName = joinRoomName.text;
        Debug.Log("roomName = " + roomName);
        RoomStatus("RoomName = '" + roomName + "' Trying to join...", false);

        // Join only, do not create if room doesn't exist
        PhotonNetwork.JoinRoom(roomName);
    }

    // ------------------ Room Callbacks ------------------
   

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(true);

        Debug.Log("Room Creation Failed: " + message);
        string displayMessage = "";

        switch (returnCode)
        {
            case 32760: // Room already exists
                displayMessage = "Room already exists. Please create another room.";
                break;

            case 32763: // GameId length invalid
                displayMessage = "Room name is invalid. Enter a valid name.";
                break;

            case 32765: // Server error / network problem
                displayMessage = "Network error. Please check your connection and try again.";
                break;

            default:
                displayMessage = "Room creation failed: " + message;
                break;
        }

        RoomStatus(displayMessage, false);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
                  " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
                  " | MaxPlayers: " + maxPlayers);

        RoomStatus("RoomName = '" + PhotonNetwork.CurrentRoom.Name + "' Joined successfully.",true);

        if (PhotonNetwork.InRoom)
        {
            int myId = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log("My Client ID = " + myId);
        }

        // Update player list UI
        UpdatePlayerListUI();

        // If room full, lock it and load PlayScene (only MasterClient)
        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
        {
           
            Debug.Log("Room is now full. No more players can join.");

            photonView.RPC(nameof(LoadPlaySceneMasterClient), RpcTarget.MasterClient);
        }
    }
    void UpdatePlayerListUI()
    {
        if (playersListText == null || PhotonNetwork.CurrentRoom == null) return;

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        playersListText.text = "Players = " + currentPlayers + " / " + maxPlayers;
    }

    [PunRPC]
    void LoadPlaySceneMasterClient()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("MasterClient starting countdown...");
            StartCoroutine(StartPlaySceneCountdown());
        }
    }


    private IEnumerator StartPlaySceneCountdown()
    {
        int countdown = 3;

        while (countdown >= 0)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
            {
                photonView.RPC(nameof(UpdateCountdownAllClients), RpcTarget.All, countdown);

                yield return new WaitForSeconds(1f);
                if (countdown <= 0)
                    status.gameObject.SetActive(false);

                countdown--;
            }
            else
            {
                break;
            }
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            Debug.Log("MasterClient loading Play Scene...");
            PhotonNetwork.LoadLevel("Play");
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("New Player Joined: " + newPlayer.NickName);
        UpdatePlayerListUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerListUI();
        Debug.Log("Player Left: " + otherPlayer.NickName);
        RoomStatus("RoomName = '" + PhotonNetwork.CurrentRoom.Name + "' Room created successfully.", true);
    }

    [PunRPC]
    void UpdateCountdownAllClients(int seconds)
    {
        RoomStatus("Loading Play Scene in... seconds = " + seconds,false);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created: " + PhotonNetwork.CurrentRoom.Name +
                  " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);
        RoomStatus("RoomName = '" + PhotonNetwork.CurrentRoom.Name + "' Room created successfully.", true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(true);

        Debug.Log("Join Room Failed: " + message);
        string displayMessage = "";

        switch (returnCode)
        {
            case 32758: // No random match found
                displayMessage = "No available room found. Enter valid room name or create a new one.";
                break;

            case 32765: // Server error / network problem
                displayMessage = "Network error. Please check your connection and try again.";
                break;

            default:
                displayMessage = "Failed to join room: " + message;
                break;
        }

        RoomStatus(displayMessage,false);
    }

    public void RoomStatus(string meassage,bool isOn)
    {
        status.text = meassage;
        waitingText.gameObject.SetActive(isOn);
    }
}
