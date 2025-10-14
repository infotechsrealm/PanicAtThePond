using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RoomTableManager : MonoBehaviourPunCallbacks
{
    public static RoomTableManager instance;

    public Transform roomTablePanel;
    public GameObject roomRowPrefab;
    public Button SelectedButton;

    internal List<Button> allRommButtons = new List<Button>();
    private void Awake()
    {
        instance = this;
    }

    public void UpdateRoomTableUI()
    {
        Debug.Log("UpdateRoomTableUI Called");

        // Clear old UI rows
        foreach (Transform child in roomTablePanel)
            Destroy(child.gameObject);

        int displayIndex = 1;

        foreach (var room in CoustomeRoomManager.Instence.aliveRooms.Values.OrderBy(r => r.Name))
        {
            GameObject row = Instantiate(roomRowPrefab, roomTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            Button btn = row.GetComponentInChildren<Button>();

            allRommButtons.Add(btn);

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
        var joinableRooms = CoustomeRoomManager.Instence.aliveRooms.Values.Where(r => r.PlayerCount < r.MaxPlayers).ToList();

        if (joinableRooms.Count == 0)
        {
            Debug.LogWarning("No available rooms to join!");
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
        return CoustomeRoomManager.Instence.aliveRooms.Values.Where(r => r.PlayerCount < r.MaxPlayers).ToList();
    }
}
