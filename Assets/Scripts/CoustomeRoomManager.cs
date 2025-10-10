using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoustomeRoomManager : MonoBehaviourPunCallbacks
{
    [Header("Water Type Toggles")]
    public Toggle toggleAllVisible;
    public Toggle toggleDeepWaters;
    public Toggle toggleMurkyWaters;
    public Toggle toggleClearWaters;
    public ToggleGroup toggleGroup; // 🔹 Add this reference

    private string selectedWaterType = "All Visible";

    private bool listenersAdded = false;

    [Header("Room Settings")]
    internal int maxPlayers; // Max players per room

    [Header("References")]
    internal PhotonLauncher PhotonLauncher;

    public Text createRoomName, joinRoomName,playerLimmit;
    public Text status; 

    public Text playersListText; // Assign in Inspector
    public Text waitingText;


    public CreateJoinManager createJoinManager;

    public GameObject hostLobby,clientLobby;

    internal GameObject lobby;

    private void Start()
    {
        PhotonLauncher = PhotonLauncher.Instance;
        PhotonNetwork.AutomaticallySyncScene = true;

        string randomNick = "Player_"  + Random.Range(100, 999);

        // Assign it to the local player
        PhotonNetwork.NickName = randomNick;
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
                   // JoinCustomeRoom();
                    JoinRandomAvailableRoom();
                    break;
                }

            case "howToPlay":
                {
                    Instantiate(GS.instance.howToPlay, createJoinManager.transform);
                    break;
                }

            case "Start":
                {
                    customeStartGame();
                    break;
                }
        }
    }

    GameObject preloder;

    // ------------------ Create Custome Room ------------------
    internal void CreateCustomeRoom()
    {
        Debug.Log(createRoomName.text.ToString() + "-" + playerLimmit.text.ToString());
        maxPlayers = int.Parse(playerLimmit.text);

        if (createRoomName.text == "" || maxPlayers > 7 || maxPlayers < 2)
        {
            return;
        }
        lobby = hostLobby;
        Debug.Log("afsssssssssssssssssgu");

        preloder = Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);

        string roomName = createRoomName.text;
        Debug.Log("roomName = " + roomName);

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsOpen = true,
            IsVisible = true,
            Plugins = null // Must be null for PUN 2 Cloud
        };

        RoomStatus("RoomName = '" + roomName + "' Trying to create Room...", false);
        PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
    }

    public RoomTableManager roomManager; // assign in Inspector

    public void JoinRandomAvailableRoom()
    {

        List<RoomInfo> joinableRooms = roomManager.GetJoinableRooms();

        if (joinableRooms.Count == 0)
        {
            Debug.LogWarning("No available rooms to join!");
            return;
        }

        lobby = clientLobby;

        Debug.LogWarning("available rooms = " + joinableRooms.Count);

        RoomInfo selectedRoom = joinableRooms[Random.Range(0, joinableRooms.Count)];
        PhotonNetwork.JoinRoom(selectedRoom.Name);
        Debug.Log("Joining room: " + selectedRoom.Name);
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

        DashManager.instance.backButton.SetActive(false);


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

        DashManager.instance.backButton.SetActive(true);


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
        if (preloder != null)
        {
            Destroy(preloder);
        }
        
      

        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
           " | Player Name: " + PhotonNetwork.NickName +
           " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
           " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);


        RoomStatus("RoomName = '" + PhotonNetwork.CurrentRoom.Name + "' Joined successfully.",true);

        if (PhotonNetwork.InRoom)
        {
            int myId = PhotonNetwork.LocalPlayer.ActorNumber;
            Debug.Log("My Client ID = " + myId);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeWaterTypeToggles();
            UpdatePlayerListUI();
        }
        else
        {
            createJoinManager.JoinPanel.SetActive(false);
        }
        lobby.SetActive(true);
       

      

      /*  if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
        {

            Debug.Log("Room is now full. No more players can join.");

            photonView.RPC(nameof(LoadPlaySceneMasterClient), RpcTarget.MasterClient);
        }*/
    }
    void UpdatePlayerListUI()
    {
        if (playersListText == null || PhotonNetwork.CurrentRoom == null) return;

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        playersListText.text = "Players = " + currentPlayers + " / " + maxPlayers;
    }

    public void customeStartGame()
    {
        maxPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("LoadPlaySceneMasterClient", RpcTarget.All);
        }
    }

    [PunRPC]
    public void LoadPlaySceneMasterClient()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Play");
        }
    }


    private IEnumerator StartPlaySceneCountdown()
    {
        int countdown = 0;

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



    public void InitializeWaterTypeToggles()
    {
        // 🔹 Assign toggles to same ToggleGroup (radio behavior)
        toggleAllVisible.group = toggleGroup;
        toggleDeepWaters.group = toggleGroup;
        toggleMurkyWaters.group = toggleGroup;
        toggleClearWaters.group = toggleGroup;

        // 🔹 Set default ON toggle
        toggleDeepWaters.isOn = true;

        // 🔹 Add listeners
        toggleAllVisible.onValueChanged.AddListener((isOn) => { if (isOn) SetWaterType("All Visible"); });
        toggleDeepWaters.onValueChanged.AddListener((isOn) => { if (isOn) SetWaterType("Deep Waters"); });
        toggleMurkyWaters.onValueChanged.AddListener((isOn) => { if (isOn) SetWaterType("Murky Waters"); });
        toggleClearWaters.onValueChanged.AddListener((isOn) => { if (isOn) SetWaterType("Clear Waters"); });
    }

    void SetWaterType(string type)
    {
        selectedWaterType = type;
        Debug.Log("Selected Water Type: " + selectedWaterType);

        // (Optional) update room property for network sync
        if (PhotonNetwork.InRoom)
        {
            ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps["WaterType"] = selectedWaterType;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
    }

    public string GetSelectedWaterType()
    {
        return selectedWaterType;
    }
}
