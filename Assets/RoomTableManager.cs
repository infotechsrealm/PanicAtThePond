using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;

public class RoomTableManager : MonoBehaviourPunCallbacks
{
    public Transform roomTablePanel;
    public GameObject roomRowPrefab;

    public Dictionary<string, RoomInfo> aliveRooms = new Dictionary<string, RoomInfo>();

   

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // Connect to Photon
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby(); // Join lobby to receive room list updates
    }

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

        // Rebuild UI
        UpdateRoomTableUI();
    }

    void UpdateRoomTableUI()
    {
        // Clear old UI rows
        foreach (Transform child in roomTablePanel)
            Destroy(child.gameObject);

        int displayIndex = 1;

        foreach (var room in aliveRooms.Values.OrderBy(r => r.Name))
        {
            GameObject row = Instantiate(roomRowPrefab, roomTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            if (texts.Length >= 3) // 3 Text components
            {
                texts[0].text = displayIndex.ToString();       // Sequential number
                texts[1].text = room.Name;                     // Room name
                texts[2].text = $"{room.PlayerCount}/{room.MaxPlayers}"; // Joined / Max
            }

            displayIndex++;
        }
    }
    public void JoinRandomAvailableRoom()
    {
        // Get all rooms that are not full
        var joinableRooms = aliveRooms.Values.Where(r => r.PlayerCount < r.MaxPlayers).ToList();

        if (joinableRooms.Count == 0)
        {
            Debug.LogWarning("No available rooms to join!");
            // Optional: create a new room if no room available
            // PhotonNetwork.CreateRoom("Room_" + Random.Range(1000, 9999));
            return;
        }

        // Pick a random room from joinable rooms
        RoomInfo selectedRoom = joinableRooms[Random.Range(0, joinableRooms.Count)];

        // Join the selected room
        PhotonNetwork.JoinRoom(selectedRoom.Name);
        Debug.Log("Joining room: " + selectedRoom.Name);
    }

    public List<RoomInfo> GetJoinableRooms()
    {
        return aliveRooms.Values.Where(r => r.PlayerCount < r.MaxPlayers).ToList();
    }
}
