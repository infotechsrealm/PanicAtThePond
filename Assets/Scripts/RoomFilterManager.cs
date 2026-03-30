using UnityEngine;
using UnityEngine.UI;
using Steamworks;

public class RoomFilterManager : MonoBehaviour
{
    public InputField searchField;
    public Transform roomListParent;
    public Dropdown regionDropdown; // Add this in Inspector
    public Toggle friendsOnlyToggle; // Add this in Inspector

    // private string currentRegionFilter = ""; // Empty = All Regions
    private bool friendsOnlyFilter = false; // False = Show all rooms

    void Start()
    {
        searchField.onValueChanged.AddListener(OnSearchChanged);
        
        // Setup region dropdown if assigned
        if (regionDropdown != null)
        {
            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(new System.Collections.Generic.List<string>()
            {
                "Best Region",
                "Europe",
                "North America",
                "Oceania"
            });
            regionDropdown.onValueChanged.AddListener(OnRegionDropdownChanged);
        }

        // Setup friends only toggle if assigned
        if (friendsOnlyToggle != null)
        {
            friendsOnlyToggle.onValueChanged.AddListener(OnFriendsOnlyToggleChanged);
        }

        // Show initial region
        Debug.Log("[RoomFilter] Initial Region: " + Photon.Pun.PhotonNetwork.CloudRegion);
    }

    void Update()
    {
        // Optional: Periodic check or debug key to seeing current region
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log($"[RoomFilter] Current Region: {Photon.Pun.PhotonNetwork.CloudRegion}, Server: {Photon.Pun.PhotonNetwork.ServerAddress}");
        }
    }

    void OnSearchChanged(string searchText)
    {
        FilterRooms();
    }



    void OnRegionDropdownChanged(int index)
    {
        string targetRegion = "";
        string regionLabel = "Best Region";

        // Map dropdown index to Photon region code
        switch (index)
        {
            case 0: targetRegion = ""; regionLabel = "Best Region"; break; // Auto/All
            case 1: targetRegion = "eu"; regionLabel = "Europe"; break;
            case 2: targetRegion = "us"; regionLabel = "North America"; break;
            case 3: targetRegion = "au"; regionLabel = "Oceania"; break;
        }

        Debug.Log($"[RegionSwitcher] Switching to {regionLabel} ({targetRegion})...");

        // Disconnect and reconnect to new region
        // Note: This operation is asynchronous. The UI will clear as we disconnect.
        Photon.Pun.PhotonNetwork.Disconnect();
        
        // We need to wait for disconnection to complete before connecting, 
        // but ConnectToRegion usually handles this if called after Disconnect? 
        // Safer to use a Coroutine or just set a flag, but for simplicity we'll assume 
        // CreateJoinManager or a coroutine here is best.
        // Actually, let's use a simple coroutine to wait for disconnect.
        StartCoroutine(SwitchRegionRoutine(targetRegion));
    }

    System.Collections.IEnumerator SwitchRegionRoutine(string regionCode)
    {
        while (Photon.Pun.PhotonNetwork.IsConnected)
        {
            yield return null;
        }

        if (string.IsNullOrEmpty(regionCode))
        {
            Debug.Log("[RegionSwitcher] Connecting to Best Region...");
            Photon.Pun.PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log($"[RegionSwitcher] Connecting to Region: {regionCode}");
            Photon.Pun.PhotonNetwork.ConnectToRegion(regionCode);
        }
    }

    void OnFriendsOnlyToggleChanged(bool isOn)
    {
        friendsOnlyFilter = isOn;
        Debug.Log($"[RoomFilter] Friends Only filter: {(isOn ? "ON" : "OFF")}");
        FilterRooms();
    }

    bool IsSteamFriend(string userId)
    {
        // Check if Steam is initialized
        if (!SteamManager.Initialized)
        {
            Debug.LogWarning("[RoomFilter] Steam not initialized, cannot check friends");
            return false;
        }

        // Try to parse the userId as a Steam ID
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        // Photon's UserId for Steam is the Steam ID as a string
        if (ulong.TryParse(userId, out ulong steamId))
        {
            CSteamID friendSteamId = new CSteamID(steamId);
            
            // Check if this Steam ID is in our friends list
            int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            
            for (int i = 0; i < friendCount; i++)
            {
                CSteamID friendId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
                if (friendId == friendSteamId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void FilterRooms()
    {
        string searchText = searchField.text.ToLower();

        for (int i = 0; i < roomListParent.childCount; i++)
        {
            GameObject row = roomListParent.GetChild(i).gameObject;

            RoomRowPrefab roomRowPrefab = row.GetComponentInChildren<RoomRowPrefab>();
            if (roomRowPrefab == null)
                continue;

            string roomName = roomRowPrefab.roomName.text.ToLower();
            
            // Get region name for filtering
            string regionName = "";
            
            // Check if it's a Photon room (has photonRoomInfo)
            if (roomRowPrefab.photonRoomInfo != null)
            {
                // Get region from Photon room custom properties
                if (roomRowPrefab.photonRoomInfo.CustomProperties.TryGetValue("region", out object regionObj))
                {
                    regionName = (regionObj as string)?.ToLower();
                }
            }
            // Otherwise check LAN room
            else if (!string.IsNullOrEmpty(roomRowPrefab.lanRoomInfo.regionName))
            {
                regionName = roomRowPrefab.lanRoomInfo.regionName.ToLower();
            }

            // ---------- FILTER LOGIC ----------
            // Note: Region filtering is now handled by switching servers. 
            // We no longer filter "regionName" inside the list because the list contains ONLY rooms from the connected region.
            
            bool matchesSearch = string.IsNullOrEmpty(searchText) || 
                                 roomName.Contains(searchText) || 
                                 regionName.Contains(searchText);
            
            // Replaced region filter with "connected region" logic (always true effectively for the loop)
            bool matchesRegion = true; 

            // Friends Only filter
            bool matchesFriends = true; // Default: show all rooms
            if (friendsOnlyFilter && roomRowPrefab.photonRoomInfo != null)
            {
                // Try to get the room creator's Steam ID from custom properties
                string creatorSteamId = "";
                
                if (roomRowPrefab.photonRoomInfo.CustomProperties.TryGetValue("creatorSteamId", out object creatorIdObj))
                {
                    creatorSteamId = creatorIdObj as string;
                }
                
                // Check if creator is a Steam friend
                if (!string.IsNullOrEmpty(creatorSteamId))
                {
                    matchesFriends = IsSteamFriend(creatorSteamId);
                    Debug.Log($"[RoomFilter] Room '{roomName}' creator Steam ID: {creatorSteamId}, isFriend: {matchesFriends}");
                }
                else
                {
                    // If no creator Steam ID stored, show the room (backward compatibility)
                    matchesFriends = true;
                    Debug.LogWarning($"[RoomFilter] Room '{roomName}' has no creator Steam ID stored");
                }
            }

            // Show room only if it matches ALL filters
            bool shouldShow = matchesSearch && matchesRegion && matchesFriends;
            row.SetActive(shouldShow);
        }
    }
}
