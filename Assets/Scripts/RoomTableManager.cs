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
            RoomRowPrefab roomRow = row.GetComponent<RoomRowPrefab>();

            // allRommButtons.Add(btn);

            // Get region string early for display
            string regionString = "";

            if (room.CustomProperties.TryGetValue("region", out object regionObj))
            {
                regionString = regionObj as string;
            }
            if (string.IsNullOrEmpty(regionString))
            {
                regionString = PhotonNetwork.CloudRegion;
            }

            if (texts.Length >= 3) // 3 Text components
            {
                // Display Region Name instead of Index
                if (RegionManager.Instance != null)
                {
                    texts[0].text = RegionManager.Instance.GetRegionDisplayName(regionString).ToUpper();
                }
                else
                {
                    texts[0].text = regionString; // Fallback
                }
                
                texts[1].text = room.Name;                     // Room name
                texts[2].text = $"{room.PlayerCount}/{room.MaxPlayers}"; // Joined / Max
            }

            // Set region icon
            if (roomRow != null && RegionManager.Instance != null)
            {
                // Store Photon room reference for filtering
                roomRow.photonRoomInfo = room;
                
                // Get and set the region icon
                Sprite regionIcon = RegionManager.Instance.GetRegionIcon(regionString);
                if (regionIcon != null)
                {
                    roomRow.SetRegionIcon(regionIcon);
                }
            }
            else
            {
                if (roomRow == null) Debug.LogWarning("[RegionIcon] RoomRowPrefab component is null!");
                if (RegionManager.Instance == null) Debug.LogWarning("[RegionIcon] RegionManager.Instance is null! Make sure RegionManager GameObject exists in the scene.");
            }

            displayIndex++;
        }

        GS.Instance.DestroyPreloder();

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

                // Determine region string
                string regionString = roomRowPrefeb.lanRoomInfo.regionName;
                if (string.IsNullOrEmpty(regionString) && RegionManager.Instance != null)
                {
                    regionString = RegionManager.Instance.GetLocalRegion();
                    roomRowPrefeb.lanRoomInfo.regionName = regionString;
                }

                // Text components update करो
                Text[] texts = roomRowPrefeb.GetComponentsInChildren<Text>();
                if (texts.Length >= 3)
                {
                    // Display Region Name instead of Index
                    if (RegionManager.Instance != null)
                    {
                        texts[0].text = RegionManager.Instance.GetRegionDisplayName(regionString).ToUpper();
                    }
                    else
                    {
                        texts[0].text = (i + 1).ToString(); // Fallback to index if no RegionManager
                    }

                    texts[1].text = server.roomName;
                    texts[2].text = $"{server.playerCount}/{server.maxPlayers}";
                }

                // Set region icon for LAN room
                if (RegionManager.Instance != null)
                {
                    Sprite regionIcon = RegionManager.Instance.GetRegionIcon(regionString);
                    
                    if (regionIcon != null)
                    {
                        roomRowPrefeb.SetRegionIcon(regionIcon);
                    }
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

                // Determine region string (New Room)
                string regionString = server.roomName; // Fallback, though we should probably check lanRoomInfo logic
                // Actually lanRoomInfo is in the prefab, we just instantiated it.
                // We should get local region since it's a discovered server, it might be local? 
                // Wait, discovered server means we found it on LAN. We might not know its region unless it's in the packet, 
                // but LAN usually implies same region (local).
                if (RegionManager.Instance != null)
                {
                    regionString = RegionManager.Instance.GetLocalRegion();
                    roomRowPrefeb.lanRoomInfo.regionName = regionString;
                }

                Text[] texts = roomRowPrefeb.GetComponentsInChildren<Text>();
                if (texts.Length >= 3)
                {
                     // Display Region Name instead of Index
                    if (RegionManager.Instance != null)
                    {
                        texts[0].text = RegionManager.Instance.GetRegionDisplayName(regionString).ToUpper();
                    }
                    else
                    {
                        texts[0].text = (i + 1).ToString();
                    }
                    
                    texts[1].text = server.roomName;
                    texts[2].text = $"{server.playerCount}/{server.maxPlayers}";
                }

                // Set region icon for LAN room
                if (RegionManager.Instance != null)
                {
                    Sprite regionIcon = RegionManager.Instance.GetRegionIcon(regionString);
                    
                    if (regionIcon != null)
                    {
                        roomRowPrefeb.SetRegionIcon(regionIcon);
                    }
                }

               // Debug.Log($"➕ Added new room: {server.roomName}");
            }
        }

        foreach (var kvp in existingRows)
        {
            Destroy(kvp.Value.gameObject);
            Debug.Log($"❌ Removed room (no longer active): {kvp.Key}");
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

        foreach (Transform child in roomTablePanel)
        {
            toDestroy.Add(child.gameObject);
        }

        foreach (GameObject go in toDestroy)
        {
            DestroyImmediate(go);
        }
    }
}
