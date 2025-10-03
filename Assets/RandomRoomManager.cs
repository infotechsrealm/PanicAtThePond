using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RandomRoomManager : MonoBehaviourPunCallbacks
{
    [Header("Room Settings")]
    internal int maxPlayers; // Max players per room

    [Header("References")]
    internal PhotonLauncher PhotonLauncher;

    public Text status;
    public Text playersListText; // Assign in Inspector
    public Text waitingText;
    public Text timerText;

    private void Start()
    {
        PhotonLauncher = PhotonLauncher.Instance;
        maxPlayers = PhotonLauncher.maxPlayers;
        PhotonNetwork.AutomaticallySyncScene = true;
        timerText.gameObject.SetActive(true);
    }

    public void OnClickAction(string action)
    {
        switch (action)
        {
            case "CreateRandom":
                CreateRandomRoom();
                break;

            case "JoinRandom":
                JoinRandomRoom();
                break;
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
            MaxPlayers = (byte)maxPlayers
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

    // ------------------ Room Callbacks ------------------

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(true);

        Debug.Log("Room Creation Failed: " + message);
        RoomStatus("Room creation failed: " + message, false);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name +
                  " | Players: " + PhotonNetwork.CurrentRoom.PlayerCount +
                  " | MaxPlayers: " + maxPlayers);

        RoomStatus("Room '" + PhotonNetwork.CurrentRoom.Name + "' joined successfully.", true);

        int myId = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log("My Client ID = " + myId);

        // Update player list UI
        UpdatePlayerListUI();

        // If room full, lock it and load PlayScene (only MasterClient)
        /* if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
         {
             Debug.Log("Room is now full. No more players can join.");
             photonView.RPC(nameof(LoadPlaySceneMasterClient), RpcTarget.MasterClient);
         }*/

        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(StartTimer());
        }
    }

    private IEnumerator StartTimer()
    {
        int countdown = 10;

        while (countdown >= 0)
        {
            photonView.RPC(nameof(UpdateTimerUI), RpcTarget.All, countdown);

            yield return new WaitForSeconds(1f);
            if (countdown <= 0)
                status.text = "";

            countdown--;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount <= 1)
        {
            RoomStatus("", false);
            playersListText.text = timerText.text = "";
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            PhotonLauncher pl = PhotonLauncher.Instance;
            pl.maxPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            maxPlayers = pl.maxPlayers;
            UpdatePlayerListUI();
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;
                Debug.Log("MasterClient loading Play Scene...");
                PhotonNetwork.LoadLevel("Play");
            }
            //photonView.RPC(nameof(LoadPlaySceneMasterClient), RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void UpdateTimerUI(int currentTime)
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = "Waiting for : " + string.Format("{0:00}:{1:00}", minutes, seconds);
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
        int countdown = 0;

        while (countdown >= 0)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayers)
            {
                photonView.RPC(nameof(UpdateCountdownAllClients), RpcTarget.All, countdown);

                yield return new WaitForSeconds(1f);
                if (countdown <= 0)
                    status.text = "";

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

    [PunRPC]
    void UpdateCountdownAllClients(int seconds)
    {
        RoomStatus("Loading Play Scene in... " + seconds, false);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room Created: " + PhotonNetwork.CurrentRoom.Name +
                  " | MaxPlayers: " + PhotonNetwork.CurrentRoom.MaxPlayers);
        RoomStatus("Room '" + PhotonNetwork.CurrentRoom.Name + "' created successfully.", true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        if (PhotonLauncher != null)
            PhotonLauncher.ShowButton(true);

        Debug.Log("Join Random Room Failed: " + returnCode);

        switch (returnCode)
        {
            case 32758: // No random match found
                RoomStatus("No random room found. Creating a new one...", false);
                CreateRandomRoom();
                break;

            default:
                CreateRandomRoom();
                break;
        }
    }

    public void RoomStatus(string message, bool isOn)
    {
        if (status != null)
            status.text = message;

        if (waitingText != null)
            waitingText.gameObject.SetActive(isOn);
    }

    // ------------------ Extra: Player Join/Leave ------------------
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player joined: " + newPlayer.NickName);
        UpdatePlayerListUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerListUI();
        Debug.Log("Player Left: " + otherPlayer.NickName);
        RoomStatus("RoomName = '" + PhotonNetwork.CurrentRoom.Name + "' Room created successfully.", true);
    }
}
