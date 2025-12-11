using Mirror;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class CoustomeRoomManager : MonoBehaviourPunCallbacks
{
    public static CoustomeRoomManager Instance;

    public CreateJoinManager createJoinManager;


    

    public RoomTableManager roomManager;
    public Button startButton;
    public bool destroyRoom = false;

    
    public Text
        createRoomName,
        createRoomNameError,
        playerLimitError,
        roomPasswordInput,
        roomPasswordInputError;

    public InputField playerLimitInput;

    internal Text joinRoomName;

    internal GameObject lobby;

    [Header("Room Settings")]
    public int maxPlayers;


    private string selectedWaterType = "All Visible";
    


    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerLimitInput.text = "7";
        PhotonNetwork.NickName = GS.Instance.nickName;

        PhotonNetwork.AutomaticallySyncScene = true;

        if (GS.Instance.isLan)
        {
            int playerCount = NetworkManager.singleton.numPlayers;
            Debug.Log("Players: " + playerCount);

            if (playerCount > 1)
            {
                if (GS.Instance.IsMirrorMasterClient)
                {
                    createJoinManager.hostLobby.gameObject.SetActive(true);
                    createJoinManager.createPanel.gameObject.SetActive(true);
                    Debug.Log("start Button Enable");
                    startButton.interactable = true;
                }
                else
                {
                    createJoinManager.clientLobby.gameObject.SetActive(true);
                    createJoinManager.JoinPanel.gameObject.SetActive(true);
                }
                createJoinManager.createAndJoinButtons.SetActive(true);
            }
        }
        else
        {

            if (PhotonNetwork.InRoom)
            {
                if (PhotonNetwork.CurrentRoom.PlayerCount > 1)
                {
                    if (PhotonNetwork.IsMasterClient)
                    {
                        createJoinManager.hostLobby.gameObject.SetActive(true);
                        createJoinManager.createPanel.gameObject.SetActive(true);
                        Debug.Log("start Button Enable");
                        startButton.interactable = true;
                    }
                    else
                    {
                        createJoinManager.clientLobby.gameObject.SetActive(true);
                        createJoinManager.JoinPanel.gameObject.SetActive(true);
                    }
                    createJoinManager.createAndJoinButtons.SetActive(true);
                }
            }
        }
    }

    // ------------------ Create Custome Room ------------------
    public void OnValueChanged(InputField playerLimit)
    {
        if (int.TryParse(playerLimit.text, out int value))
        {
            if (value > 7)
            {
                playerLimitInput.text = "7";
            }
            else if (value < 2)
            {
                playerLimitInput.text = "2";
            }
        }
    }

    internal void CreateCustomeRoom()
    {
        string value = playerLimitInput.text;
        if (value != "")
        {
            maxPlayers = int.Parse(value);
        }

        // ✅ Username validation
        string username = createRoomName.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            createRoomNameError.text = "Username is required";
            return;
        }
        else if (username.Length < 3 || username.Length > 10)
        {
            createRoomNameError.text = "Username must be between 3 and 10 characters long";
            return;
        }
        else if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            createRoomNameError.text = "Username can only contain letters, numbers, and underscores";
            return;
        }
        else
        {
            createRoomNameError.text = "";
        }

        // ✅ Player Limit validation
      /*  if (maxPlayers > 7)
        {
            playerLimitInput.text = "7";
            //playerLimitError.text = "Player Limit must be between 2 to 7 members";
        }*/
      /*  else
        {
            playerLimitError.text = "";
        }*/

        // ✅ Password validation
        if (string.IsNullOrEmpty(roomPasswordInput.text))
        {
           /* roomPasswordInputError.text = "Password is required";
            return;*/
        }
        else if (roomPasswordInput.text.Length < 6)
        {
            roomPasswordInputError.text = "A minimum 6-digit password is required";
            return;
        }
        else
        {
            roomPasswordInputError.text = "";
        }

        createRoomNameError.text = "";

        lobby = createJoinManager.hostLobby.gameObject;

        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);


        string roomName = createRoomName.text;
        string password = roomPasswordInput.text; // <-- Add a password InputField in your UI

        if (roomPasswordInput.text.ToString() != null)
        {
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

            PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        }
        else
        {
            RoomOptions options = new RoomOptions
            {
                MaxPlayers = maxPlayers,
                IsOpen = true,
                IsVisible = true,

                CustomRoomPropertiesForLobby = new string[] { "pwd" } // Optional: show password existence in lobby
            };

            PhotonNetwork.CreateRoom(roomName, options, TypedLobby.Default);
        }
    }

    // ------------------ Join Custome Room ------------------

    public void JoinRandomAvailableRoom()
    {
        List<RoomInfo> joinableRooms = roomManager.GetJoinableRooms();

        if (joinableRooms.Count == 0)
        {
            return;
        }

        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);


        RoomInfo selectedRoom = joinableRooms[Random.Range(0, joinableRooms.Count)];

        lobby = createJoinManager.clientLobby.gameObject;

        if (selectedRoom.CustomProperties.TryGetValue("pwd", out object pwdObj))
        {
            string roomPassword = pwdObj as string;

            if (!string.IsNullOrEmpty(roomPassword))
            {
                ShowPasswordPopup(selectedRoom, roomPassword);
                return;
            }
        }
        PhotonNetwork.JoinRoom(selectedRoom.Name);
    }

    private void ShowPasswordPopup(RoomInfo room, string correctPassword)
    {
        Debug.Log("Room requires a password.");
        GameObject popup = Instantiate(GS.Instance.passwordPopupPrefab, transform);
        popup.GetComponent<PasswordPopup>().Init(room, correctPassword);
    }

    internal void JoinCustomeRoom()
    {
        if (joinRoomName == null || string.IsNullOrEmpty(joinRoomName.text))
        {
            Debug.Log("Please enter a room name to join.");
            return;
        }

        string roomName = joinRoomName.text;

        RoomInfo targetRoom = null;

        if (aliveRooms.ContainsKey(roomName))
        {
            targetRoom = aliveRooms[roomName];
        }

        if (targetRoom == null)
        {
            Debug.Log("Room not found in the current room list!");
            return;
        }

        lobby = createJoinManager.clientLobby.gameObject;

        // Check for password property
        if (targetRoom.CustomProperties.TryGetValue("pwd", out object pwdObj))
        {
            string roomPassword = pwdObj as string;

            if (!string.IsNullOrEmpty(roomPassword))
            {
                // Show password popup for verification
                ShowPasswordPopup(targetRoom, roomPassword);
                return;
            }
        }

        // If no password, join directly
        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);


        joinRoomName = null;

        PhotonNetwork.JoinRoom(roomName);
    }

    // ------------------ Room Callbacks ------------------
   
    public override void OnCreateRoomFailed(short returnCode, string message)
    {

        GS.Instance.DestroyPreloder();

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
    }

    public override void OnJoinedRoom()
    {
        GS.Instance.DestroyPreloder();


        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
           " | Player Name: " + PhotonNetwork.NickName +
           " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
           " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);

        if (PhotonNetwork.InRoom)
        {
            int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        }

       

        lobby.SetActive(true);

        if(PasswordPopup.Instance!=null)
        {
            Destroy(PasswordPopup.Instance.gameObject);
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount >= 2)
        {
            photonView.RPC(nameof(EnableStartButton), RpcTarget.MasterClient);
        }
    }


    [PunRPC]
    public void EnableStartButton()
    {

        Debug.Log("start Button Enable");

        startButton.interactable = true;
    }


    [PunRPC]
    public void customeStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.MaxPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            if (PhotonNetwork.CurrentRoom.PlayerCount >= PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                photonView.RPC(nameof(GeneratePreloder), RpcTarget.All);
                PhotonNetwork.LoadLevel("Play");
                PhotonNetwork.SendAllOutgoingCommands(); // send it now
            }
        }
    }

    [PunRPC]
    public void GeneratePreloder()
    {
        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateTablesUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateTablesUI();

        if (destroyRoom)
        {
            LeaveRoom();
        }
    }

    public void UpdateTablesUI()
    {
        if (PlayerTableManager.Instance != null)
        {
            PlayerTableManager.Instance.UpdatePlayerTable();
        }
        if (RoomTableManager.instance != null)
        {
            RoomTableManager.instance.UpdateRoomTable();
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created: " + PhotonNetwork.CurrentRoom.Name +
                  " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        GS.Instance.DestroyPreloder();


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

        GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

        createJoinManager.clientLobby.gameObject.SetActive(false);
        createJoinManager.hostLobby.gameObject.SetActive(false);

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
            photonView.RPC(nameof(ForceAllLeave), RpcTarget.All);
            PhotonNetwork.SendAllOutgoingCommands(); // send it now
        }
    }

    // Alternative method without RPC
    [PunRPC]
    public void ForceAllLeave()
    {
        Debug.Log("ForceAllLeave called");

        // Local execution sab players ke liye
        if (Preloader.Instence == null && GS.Instance != null && GS.Instance.preloder != null)
        {
            if (DashManager.Instance != null && DashManager.Instance.prefabPanret != null)
            {
                GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

            }
        }

        if (createJoinManager.clientLobby != null) createJoinManager.clientLobby.gameObject.SetActive(false);
        if (createJoinManager.hostLobby != null) createJoinManager.hostLobby.gameObject.SetActive(false);

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

        if (Preloader.Instence == null)
        {
            GS.Instance.GeneratePreloder(DashManager.Instance.prefabPanret.transform);

        }

        createJoinManager.clientLobby.gameObject.SetActive(false);
        createJoinManager.hostLobby.gameObject.SetActive(false);

        PhotonNetwork.LeaveRoom();
    }

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
            RoomTableManager.instance.UpdateRoomTable();
        }

    }

    public void ValidateUsername()
    {
        string username = createRoomName.text.Trim();

        if (string.IsNullOrEmpty(username))
        {
            createRoomNameError.text = "Username is required";
        }
        else if (username.Length < 10)
        {
            createRoomNameError.text = "Username must be at least 10 characters long";
        }
        else if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$"))
        {
            createRoomNameError.text = "Username can only contain letters, numbers, and underscores";
        }
        else
        {
            createRoomNameError.text = "";
        }
        return;
    }
}
