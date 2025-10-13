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

    [Header("Room Settings")]
    internal int maxPlayers; // Max players per room

    [Header("References")]
    internal PhotonLauncher PhotonLauncher;

    public Text createRoomName, joinRoomName,playerLimmit, roomPasswordInput;
    public Text status; 

    public Text playersListText; // Assign in Inspector
    public Text waitingText;


    public CreateJoinManager createJoinManager;

    public GameObject hostLobby,clientLobby;

    internal GameObject lobby;

    public static CoustomeRoomManager Instence;

    public bool destroyRoom = false;

    public Button startButton;

    private void Awake()
    {
        Instence = this;
    }
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

            case "JoinRandom":
                {
                    JoinRandomAvailableRoom();
                    break;
                }

            case "JoinCustome":
                {
                    JoinCustomeRoom();
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


    // ------------------ Create Custome Room ------------------
    /*internal void CreateCustomeRoom()
    {
        Debug.Log(createRoomName.text.ToString() + "-" + playerLimmit.text.ToString());
        maxPlayers = int.Parse(playerLimmit.text);

        if (createRoomName.text == "" || maxPlayers > 7 || maxPlayers < 2)
        {
            return;
        }
        lobby = hostLobby;
        if (Preloader.instance == null)
        {

            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }

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
    }*/

    internal void CreateCustomeRoom()
    {
        Debug.Log(createRoomName.text + "-" + playerLimmit.text);

        maxPlayers = int.Parse(playerLimmit.text);

        if (string.IsNullOrEmpty(createRoomName.text) || maxPlayers > 7 || maxPlayers < 2)
            return;

        lobby = hostLobby;

        if (Preloader.instance == null)
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);

        string roomName = createRoomName.text;
        string password = roomPasswordInput.text; // <-- Add a password InputField in your UI


            Debug.Log("Creating room: " + roomName + " with password: " + password);
        if (roomPasswordInput.text.ToString() != null)
        {
            Debug.Log("Password is Set");

            // Add password to custom properties
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties["pwd"] = password;

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsOpen = true,
                IsVisible = true,

                CustomRoomProperties = customProperties,
                CustomRoomPropertiesForLobby = new string[] { "pwd" } // Optional: show password existence in lobby
            };

            RoomStatus($"RoomName = '{roomName}' Trying to create Room...", false);
            PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        }
        else
        {

            Debug.Log("Password is not Set");
            RoomOptions options = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsOpen = true,
                IsVisible = true,

                CustomRoomPropertiesForLobby = new string[] { "pwd" } // Optional: show password existence in lobby
            };

            RoomStatus($"RoomName = '{roomName}' Trying to create Room...", false);
            PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        }
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

        if (Preloader.instance == null)
        {
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }

        lobby = clientLobby;

        Debug.LogWarning("available rooms = " + joinableRooms.Count);

        RoomInfo selectedRoom = joinableRooms[Random.Range(0, joinableRooms.Count)];

        if (selectedRoom.CustomProperties.TryGetValue("pwd", out object pwdObj))
        {
            string roomPassword = pwdObj as string;

            if (!string.IsNullOrEmpty(roomPassword))
            {
                // Room has password -> Show password UI
                Debug.Log("Room requires password.");
                ShowPasswordPopup(selectedRoom, roomPassword);
                return;
            }
        }

        PhotonNetwork.JoinRoom(selectedRoom.Name);
        Debug.Log("Joining room: " + selectedRoom.Name);
    }

   /* public void JoinRandomAvailableRoom2()
    {
        List<RoomInfo> joinableRooms = roomManager.GetJoinableRooms(); 
        if (joinableRooms.Count == 0) { Debug.LogWarning("No available rooms to join!"); return; }
        if (Preloader.instance == null) { Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform); }
        lobby = clientLobby; Debug.LogWarning("available rooms = " + joinableRooms.Count); RoomInfo selectedRoom = joinableRooms[Random.Range(0, joinableRooms.Count)]; PhotonNetwork.JoinRoom(selectedRoom.Name); Debug.Log("Joining room: " + selectedRoom.Name);
    }*/

    [SerializeField] private GameObject passwordPopupPrefab; // Assign in Inspector

    private void ShowPasswordPopup(RoomInfo room, string correctPassword)
    {
        GameObject popup = Instantiate(passwordPopupPrefab, DashManager.instance.prefabPanret.transform);
        popup.GetComponent<PasswordPopup>().Init(room, correctPassword);
    }


    // ------------------ Join Custome Room ------------------

    internal void JoinCustomeRoom()
{
    if (joinRoomName == null || string.IsNullOrEmpty(joinRoomName.text))
        return;

    string roomName = joinRoomName.text;
    Debug.Log("Trying to join room: " + roomName);

    // Find the room info from your current room list
    RoomInfo targetRoom = null;

    if (aliveRooms.ContainsKey(roomName))
    {
        targetRoom = aliveRooms[roomName];
    }

    if (targetRoom == null)
    {
        Debug.LogWarning("Room not found in the current room list!");
        RoomStatus("Room not found or not available!", true);
        return;
    }

    // Check for password property
    if (targetRoom.CustomProperties.TryGetValue("pwd", out object pwdObj))
    {
        string roomPassword = pwdObj as string;

        if (!string.IsNullOrEmpty(roomPassword))
        {
            // Show password popup for verification
            Debug.Log("Room requires a password.");
            ShowPasswordPopup(targetRoom, roomPassword);
            return;
        }
    }

    // If no password, join directly
    if (Preloader.instance == null)
        Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);

    lobby = clientLobby;

    RoomStatus("RoomName = '" + roomName + "' Trying to join...", false);

    joinRoomName = null;

    PhotonNetwork.JoinRoom(roomName);
}

  /*  internal void JoinCustomeRoom()
    {
        if (joinRoomName == null || joinRoomName.text == "")
        {
            return;
        }

        // DashManager.instance.backButton.SetActive(false);

        if (Preloader.instance == null)
        {
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }
        lobby = clientLobby;

        string roomName = joinRoomName.text;
        Debug.Log("roomName = " + roomName);
        RoomStatus("RoomName = '" + roomName + "' Trying to join...", false);

        joinRoomName = null;
        // Join only, do not create if room doesn't exist
        PhotonNetwork.JoinRoom(roomName);
    }*/

    // ------------------ Room Callbacks ------------------
   

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
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
        if (Preloader.instance != null)
        {
            Destroy(Preloader.instance.gameObject);
        }


        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
           " | Player Name: " + PhotonNetwork.NickName +
           " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
           " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);


        RoomStatus("RoomName = '" + PhotonNetwork.CurrentRoom.Name + "' Joined successfully.", true);

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

        lobby.SetActive(true);

        //When all Player
        /*  if(PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
          {
              photonView.RPC(nameof(customeStartGame), RpcTarget.MasterClient);
          }*/


        if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            photonView.RPC(nameof(EnableStartButton), RpcTarget.MasterClient);

        }
    }


    [PunRPC]
    public void EnableStartButton()
    {
        startButton.interactable = true;
    }

    void UpdatePlayerListUI()
    {
        if (playersListText == null || PhotonNetwork.CurrentRoom == null) return;

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        playersListText.text = "Players = " + currentPlayers + " / " + maxPlayers;
    }

    [PunRPC]
    public void customeStartGame()
    {
        //maxPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                    PhotonNetwork.LoadLevel("Play");
            }
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
        if (destroyRoom)
        {
            LeaveRoom();
        }
        Debug.Log("Player Count: " + PhotonNetwork.CurrentRoom.PlayerCount);
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
        if(Preloader.instance!=null)
        {
            Destroy(Preloader.instance.gameObject);
        }

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


    public void CallLeaveRoom()
    {
        destroyRoom = true;
        Debug.Log("Leaving room...");
        if (Preloader.instance == null)
        {
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }
        clientLobby.SetActive(false);
        hostLobby.SetActive(false);

        LeaveRoom();
    }



    void  LeaveRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            Debug.Log("LeaveRoom Called");
            destroyRoom = false;
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            photonView.RPC(nameof(LeaveRoomRPC), RpcTarget.Others);
        }
    }








    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Application quitting - Forcing AllLeave");
            AllLeave();
        }
    }

    private void OnApplicationQuit()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("OnApplicationQuit - Calling AllLeave directly");
            // Direct call without RPC for reliability
            ForceAllLeave();
        }
    }

    // Alternative method without RPC
    public void ForceAllLeave()
    {
        Debug.Log("ForceAllLeave called");

        // Local execution sab players ke liye
        if (Preloader.instance == null && GS.instance != null && GS.instance.preloder != null)
        {
            if (DashManager.instance != null && DashManager.instance.prefabPanret != null)
            {
                Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
            }
        }

        if (clientLobby != null) clientLobby.SetActive(false);
        if (hostLobby != null) hostLobby.SetActive(false);

        PhotonNetwork.LeaveRoom();
    }

    // Ya fir try karein with buffered RPC
    public void AllLeave()
    {
        Debug.Log("AllLeave called with buffered RPC");
        photonView.RPC(nameof(LeaveRoomRPC), RpcTarget.AllBuffered);
    }

    [PunRPC]
    public void LeaveRoomRPC()
    {
        Debug.Log("Leaving room...");
        if (Preloader.instance == null)
        {
            Instantiate(GS.instance.preloder, DashManager.instance.prefabPanret.transform);
        }

        clientLobby.SetActive(false);
        hostLobby.SetActive(false);

        PhotonNetwork.LeaveRoom();
    }


    //When Room list is Update

    public Dictionary<string, RoomInfo> aliveRooms = new Dictionary<string, RoomInfo>();


    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        // Update local dictionary of alive rooms
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                aliveRooms.Remove(room.Name); // Remove closed rooms
            }
            else
            {
                aliveRooms[room.Name] = room; // Add or update alive rooms
            }
        }


        if (RoomTableManager.instance != null)
        {
            RoomTableManager.instance.UpdateRoomTableUI();
        }
    }
}
