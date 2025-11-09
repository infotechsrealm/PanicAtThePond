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
    public RoomRowPrefab roomRowPrefab;
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
            GameObject row = Instantiate(roomRowPrefab.gameObject, roomTablePanel);

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

    public void UpdateLANRoomTableUI()
    {
       /* foreach (Transform child in roomTablePanel)
        {
            Destroy(child.gameObject);
        }*/

        LANDiscoveryMenu lANDiscoveryMenu = LANDiscoveryMenu.Instance;


        // 🔹 1️⃣ Discovered room names ka set bana lo
        HashSet<string> discoveredNames = new HashSet<string>();
        foreach (var server in lANDiscoveryMenu.discoveredServers)
        {
            if (!string.IsNullOrEmpty(server.roomName))
                discoveredNames.Add(server.roomName);
        }

        // 🔹 2️⃣ Existing UI rows collect karo
        List<RoomRowPrefab> existingRows = new List<RoomRowPrefab>();
        foreach (Transform child in roomTablePanel)
        {
            RoomRowPrefab existingRow = child.GetComponent<RoomRowPrefab>();
            if (existingRow != null)
                existingRows.Add(existingRow);
        }

        // 🔹 3️⃣ Remove those rows jinka name discovered list me nahi hai
        foreach (var row in existingRows)
        {
            string existingName = row.lanRoomInfo.roomName;
            if (!discoveredNames.Contains(existingName))
            {
                Debug.Log($"🗑 Removing stale room: {existingName}");
                Destroy(row.gameObject);
            }
        }

        // 🔹 4️⃣ UI me jo room missing hai unke liye new row create karo
        HashSet<string> currentUIRoomNames = new HashSet<string>();
        foreach (Transform child in roomTablePanel)
        {
            RoomRowPrefab existingRow = child.GetComponent<RoomRowPrefab>();
            if (existingRow != null && !string.IsNullOrEmpty(existingRow.lanRoomInfo.roomName))
            {
                currentUIRoomNames.Add(existingRow.lanRoomInfo.roomName);
            }
        }

        for (int i = 0; i < lANDiscoveryMenu.discoveredServers.Count; i++)
        {
            var server = lANDiscoveryMenu.discoveredServers[i];
              string roomName = server.roomName;

            // Skip agar already UI me hai
            if (currentUIRoomNames.Contains(roomName))
            {
                Debug.Log($"⏭ Skipping existing room: {roomName}");
                continue;
            }

            // ✅ Otherwise, add new one
            RoomRowPrefab roomRowPrefeb = Instantiate(roomRowPrefab, roomTablePanel);

            roomRowPrefeb.lanRoomInfo.roomName = server.roomName;
            roomRowPrefeb.lanRoomInfo.port = server.port;
            roomRowPrefeb.lanRoomInfo.baseBroadcastPort = server.baseBroadcastPort;
            roomRowPrefeb.lanRoomInfo.roomPassword = server.roomPassword;
            roomRowPrefeb.lanRoomInfo.connectedPlayers = server.playerCount;

            Text[] texts = roomRowPrefeb.GetComponentsInChildren<Text>();
            Button btn = roomRowPrefeb.GetComponentInChildren<Button>();
            allRommButtons.Add(btn);

            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString(); // Index
                texts[1].text = server.roomName;           // Room name
                texts[2].text = $"{server.playerCount}/{server.maxPlayers}";             // Joined / Max
            }

            Debug.Log($"➕ Added new room: {server.roomName}");
        }

        // 🔹 5️⃣ Optional: Remove Preloader if exists
        GS.Instance.DestroyPreloder();
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
