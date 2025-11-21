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



  //  internal List<RoomRowPrefab> allRoomPrefabs = new List<RoomRowPrefab>();

    private void Awake()
    {
        instance = this;
    }

    public void UpdateRoomTable()
    {
        if (GS.Instance.isLan)
        {
            UpdateLANRoomTableUI();
        }
        else
        {
            UpdateRoomTableUI();
        }
    }

    public void UpdateRoomTableUI()
    {
        Debug.Log("UpdateRoomTableUI Called");

        // Clear old UI rows
        foreach (Transform child in roomTablePanel)
            Destroy(child.gameObject);

        int displayIndex = 1;

        foreach (var room in CoustomeRoomManager.Instance.aliveRooms.Values.OrderBy(r => r.Name))
        {
            GameObject row = Instantiate(roomRowPrefab.gameObject, roomTablePanel);

            Text[] texts = row.GetComponentsInChildren<Text>();
            Button btn = row.GetComponentInChildren<Button>();

            // allRommButtons.Add(btn);

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
        LANDiscoveryMenu lANDiscoveryMenu = LANDiscoveryMenu.Instance;

        // पहले से बने हुए rows को track करने के लिए dictionary रखो (key = roomName)
        Dictionary<string, RoomRowPrefab> existingRows = new Dictionary<string, RoomRowPrefab>();

        // पहले से बने हुए child rows को dictionary में डालो
        foreach (Transform child in roomTablePanel)
        {
            RoomRowPrefab row = child.GetComponent<RoomRowPrefab>();
            if (row != null && !string.IsNullOrEmpty(row.lanRoomInfo.roomName))
            {
                existingRows[row.lanRoomInfo.roomName] = row;
            }
        }

        // अब discoveredServers के हिसाब से UI sync करो
        for (int i = 0; i < lANDiscoveryMenu.discoveredServers.Count; i++)
        {
            var server = lANDiscoveryMenu.discoveredServers[i];
            RoomRowPrefab roomRowPrefeb;

            // 🔹 अगर यह room पहले से exist करता है → सिर्फ update करो
            if (existingRows.TryGetValue(server.roomName, out roomRowPrefeb))
            {
                roomRowPrefeb.lanRoomInfo.roomName = server.roomName;
                roomRowPrefeb.lanRoomInfo.port = server.port;
                roomRowPrefeb.lanRoomInfo.baseBroadcastPort = server.baseBroadcastPort;
                roomRowPrefeb.lanRoomInfo.roomPassword = server.roomPassword;
                roomRowPrefeb.lanRoomInfo.connectedPlayers = server.playerCount;
                roomRowPrefeb.lanRoomInfo.maxPlayers = server.maxPlayers;

                // Text components update करो
                Text[] texts = roomRowPrefeb.GetComponentsInChildren<Text>();
                if (texts.Length >= 3)
                {
                    texts[0].text = (i + 1).ToString(); // Index
                    texts[1].text = server.roomName;
                    texts[2].text = $"{server.playerCount}/{server.maxPlayers}";
                }

                existingRows.Remove(server.roomName); // यह update हो गया
            }
            else
            {
                // 🔹 नया room → prefab instantiate करो
                roomRowPrefeb = Instantiate(roomRowPrefab, roomTablePanel);

                roomRowPrefeb.lanRoomInfo.roomName = server.roomName;
                roomRowPrefeb.lanRoomInfo.port = server.port;
                roomRowPrefeb.lanRoomInfo.baseBroadcastPort = server.baseBroadcastPort;
                roomRowPrefeb.lanRoomInfo.roomPassword = server.roomPassword;
                roomRowPrefeb.lanRoomInfo.connectedPlayers = server.playerCount;
                roomRowPrefeb.lanRoomInfo.maxPlayers = server.maxPlayers;

                Text[] texts = roomRowPrefeb.GetComponentsInChildren<Text>();
                if (texts.Length >= 3)
                {
                    texts[0].text = (i + 1).ToString();
                    texts[1].text = server.roomName;
                    texts[2].text = $"{server.playerCount}/{server.maxPlayers}";
                }

                Debug.Log($"➕ Added new room: {server.roomName}");
            }
        }

        // 🔹 जो पुराने rooms अब discovery list में नहीं हैं → उन्हें हटाओ
        foreach (var kvp in existingRows)
        {
            Destroy(kvp.Value.gameObject);
            Debug.Log($"❌ Removed room (no longer active): {kvp.Key}");
        }

        // 🔹 Index numbers को ensure करो कि सही हों (1,2,3,...)
        int index = 1;
        foreach (Transform child in roomTablePanel)
        {
            Text[] texts = child.GetComponentsInChildren<Text>();
            if (texts.Length > 0)
            {
                texts[0].text = index.ToString();
                index++;
            }
        }

        GS.Instance.DestroyPreloder();
    }


    public List<RoomInfo> GetJoinableRooms()
    {
        return CoustomeRoomManager.Instance.aliveRooms.Values.Where(r => r.PlayerCount < r.MaxPlayers).ToList();
    }

    public void ResetTable()
    {
        List<GameObject> toDestroy = new List<GameObject>();

        // पहले सभी children collect करो
        foreach (Transform child in roomTablePanel)
        {
            toDestroy.Add(child.gameObject);
        }

        // अब safely destroy करो
        foreach (GameObject go in toDestroy)
        {
            DestroyImmediate(go);
        }
    }
}
